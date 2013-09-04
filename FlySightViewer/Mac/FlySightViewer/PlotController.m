/*
 * Changes made to Core Plot:
 * 
 * Line 27 in CPAxisTitle.m should read:
 * 			anchor = (coordinate == CPCoordinateX ? CGPointMake(0.5, 1.0) : CGPointMake(0.5, 0.0));
 *
 * Line 453 in CPAxis.m commented out, and the following added after it:
 *			pointLocation = [pointLocation decimalNumberBySubtracting:interval];
 *			numPoints = numPoints + 2;
 * Seems to give good scales in a wider variety of views by allowing major and minor ticks at the ends
 *
 * Inserted at line 1120 of CPAxis.m:
 *			if (!plotArea)
 *			{
 *				for ( CPAxisLabel *label in axisLabels ) {
 *					CPLayer *contentLayer = label.contentLayer;
 *					if ( contentLayer ) [contentLayer removeFromSuperlayer];
 *				}
 *				if ( self.axisTitle.contentLayer ) {
 *					[self.axisTitle.contentLayer removeFromSuperlayer];
 *				}
 *			}
 * and at line 73 of CPAxisSet.m:
 *			axis.plotArea = nil;
 * without these, axis labels and titles never seem to be *removed* from the superlayer when axisSet is changed such that
 * some axis is removed.
 *
 * Inserted at line 207 of CPLayer.m:
 * 		CGContextClipToRect(context, self.bounds);
 * without this, nothing seems to be done to the context before rendering recursively, and no clipping is done.
 */

#import "PlotController.h"
#import "PlotAxisParameters.h"
#import "PlotRange.h"

@implementation PlotController

@synthesize delegate;
@synthesize graph;

-(id) initWithArrayController: (NSArrayController *) initArrayController view: (NSView *) initView bounds: (NSRect) initBounds
{
	self = [super init];
	if (self != nil)
	{
		view = (CPLayerHostingView *)initView;
		
		xName = nil;
		annotationPlotSpace = nil;
		selectionPoints = nil;
		xAxis = nil;
		
		for (int i = 0; i < FSAxisTypeCount; ++i)
		{
			axisParameters[i] = nil;
			plots[i] = nil;
			plotSpaces[i] = nil;
			yAxes[i] = nil;
		}
		
		dataPoints = [initArrayController retain];
		visibleRange = [[PlotRange plotRangeWithLocation: 0.0 length: [[dataPoints arrangedObjects] count] - 1] retain];

// Set up graph
		
		graph = [[CPXYGraph alloc] initWithFrame: *((CGRect*)&initBounds)];
		
		CPTheme *theme = [CPTheme themeNamed: kCPPlainWhiteTheme];
		[graph applyTheme:theme];
		if (view != nil) view.hostedLayer = graph;

		graph.plotAreaFrame.paddingLeft = 70.0;
		graph.plotAreaFrame.paddingTop = 20.0;
		graph.plotAreaFrame.paddingRight = 70.0;
		graph.plotAreaFrame.paddingBottom = 50.0;
		
		graph.plotAreaFrame.borderLineStyle = nil;

// Set up plot space for annotations
		
		annotationPlotSpace = (CPXYPlotSpace *)(graph.defaultPlotSpace);		
		annotationPlotSpace.allowsUserInteraction = NO;
		annotationPlotSpace.delegate = self;

		annotationPlotSpace.yRange = [CPPlotRange plotRangeWithLocation: CPDecimalFromFloat(0.0)
																length: CPDecimalFromFloat(1.0)];	

// Create a plot for selection
		
		NSDecimalNumber *zero = (NSDecimalNumber *)[NSDecimalNumber numberWithDouble: 0.0];
		NSDecimalNumber *one = (NSDecimalNumber *)[NSDecimalNumber numberWithDouble: 1.0];

		NSMutableArray *selectionArray = [NSMutableArray arrayWithCapacity: 2];
		[selectionArray addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys: zero, @"x", one, @"y", nil]];
		[selectionArray addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys: zero, @"x", one, @"y", nil]];
		selectionPoints = [[NSArrayController alloc] init];
		[selectionPoints addObjects: selectionArray];
		
		selectionPlot = [[CPScatterPlot alloc] init];
		selectionPlot.identifier = @"Selection plot";
		selectionPlot.dataLineStyle.lineWidth = 0.0f;
		selectionPlot.areaFill = [CPFill fillWithColor: [CPColor colorWithGenericGray: 0.9]];
		selectionPlot.areaBaseValue = CPDecimalFromString(@"0.0");
		[selectionPlot bind: CPScatterPlotBindingXValues toObject: selectionPoints withKeyPath: @"arrangedObjects.x" options: nil];
		[selectionPlot bind: CPScatterPlotBindingYValues toObject: selectionPoints withKeyPath: @"arrangedObjects.y" options: nil];
		
		[graph addPlot: selectionPlot toPlotSpace: annotationPlotSpace];

// Create a plot for measuring
		
		NSMutableArray *measuringArray = [NSMutableArray arrayWithCapacity: 2];
		[measuringArray addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys: zero, @"x", zero, @"y", nil]];
		[measuringArray addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys: zero, @"x", zero, @"y", nil]];
		measuringPoints = [[NSArrayController alloc] init];
		[measuringPoints addObjects: measuringArray];
		
		measuringPlot = [[CPScatterPlot alloc] init];
		measuringPlot.identifier = @"Measurement plot";
		measuringPlot.dataLineStyle.lineWidth = 1.0f;
		measuringPlot.dataLineStyle.lineColor = [CPColor blackColor];
		[measuringPlot bind: CPScatterPlotBindingXValues toObject: measuringPoints withKeyPath: @"arrangedObjects.x" options: nil];
		[measuringPlot bind: CPScatterPlotBindingYValues toObject: measuringPoints withKeyPath: @"arrangedObjects.y" options: nil];

		CPPlotSymbol *measuringSymbol = [CPPlotSymbol ellipsePlotSymbol];
		measuringSymbol.fill = [CPFill fillWithColor:[CPColor blackColor]];
		measuringSymbol.size = CGSizeMake(10.0, 10.0);
		measuringPlot.plotSymbol = measuringSymbol;

// Set up plot spaces for data
		
		for (int i = 0; i < FSAxisTypeCount; ++i)
		{
			plotSpaces[i] = (CPXYPlotSpace *)[graph newPlotSpace];
			plotSpaces[i].allowsUserInteraction = NO;
			[graph addPlotSpace: plotSpaces[i]];		
		}

// Build a line style for axes and ticks
		
		CPLineStyle *lineStyle = [CPLineStyle lineStyle];
		lineStyle.lineColor = [CPColor blackColor];
		lineStyle.lineWidth = 1.0f;	

// Set up y axes
		
		for (int i = 0; i < FSAxisTypeCount; ++i)
		{
			yAxes[i] = [(CPXYAxis *)[CPXYAxis alloc] initWithFrame: graph.bounds]; 
			yAxes[i].plotSpace = plotSpaces[i];
			yAxes[i].coordinate = CPCoordinateY;
			
			yAxes[i].majorTickLineStyle = lineStyle;
			yAxes[i].minorTickLineStyle = lineStyle;
			yAxes[i].axisLineStyle = lineStyle;
			yAxes[i].minorTickLength = 5.0f;
			yAxes[i].majorTickLength = 7.0f;
			yAxes[i].labelingPolicy = CPAxisLabelingPolicyAutomatic;
			yAxes[i].preferredNumberOfMajorTicks = 5;
			yAxes[i].labelOffset = 3.0f;
			yAxes[i].titleOffset = 50.0f;
		}
		
		yAxes[FSAxisTypeLeft].tickDirection = CPSignNegative;		
		yAxes[FSAxisTypeRight].tickDirection = CPSignPositive;
		
// Set up x axis
		
		xAxis = [(CPXYAxis *)[CPXYAxis alloc] initWithFrame: graph.bounds]; 
		xAxis.coordinate = CPCoordinateX;
		
		xAxis.majorTickLineStyle = lineStyle;
		xAxis.minorTickLineStyle = lineStyle;
		xAxis.axisLineStyle = lineStyle;
		xAxis.minorTickLength = 5.0f;
		xAxis.majorTickLength = 7.0f;
		xAxis.labelingPolicy = CPAxisLabelingPolicyAutomatic;
		xAxis.preferredNumberOfMajorTicks = 5;
		xAxis.labelOffset = 3.0f;
		xAxis.tickDirection = CPSignNegative;

		xAxis.plotSpace = annotationPlotSpace;
		xAxis.orthogonalCoordinateDecimal = CPDecimalFromFloat(0.0);

		CPXYAxisSet *axisSet = (CPXYAxisSet *)graph.axisSet;
		axisSet.axes = [NSArray arrayWithObject: xAxis];
	}
	
	return self;
}

-(void) dealloc
{
	[visibleRange release];
	
	[xName release];
	[xAxis release];
	
	[selectionPoints release];
	[selectionPlot release];
	[measuringPoints release];
	[measuringPlot release];

	for (int i = 0; i < FSAxisTypeCount; ++i)
	{
		[axisParameters[i] release];
		[plots[i] release];
		[plotSpaces[i] release];
		[yAxes[i] release];
	}
	
	[graph release];
	[dataPoints release];
	
	[super dealloc];
}

-(void) setXName: (NSString *) newXName label: (NSString *) newXLabel
{
	[xName release];
	xName = [newXName copy];
	xAxis.title = [newXLabel copy];

	for (int i = 0; i < FSAxisTypeCount; ++i) 
	{
		for (CPScatterPlot *thisPlot in plots[i])
		{
			NSString *xKeyPath = [NSString stringWithFormat: @"arrangedObjects.%@", xName];
			[thisPlot bind: CPScatterPlotBindingXValues toObject: dataPoints withKeyPath: xKeyPath options: nil];
		}

		[self rescalePlotSpace: i];
	}
}

-(void) setParameters: (PlotAxisParameters *) newAxisParameters forAxis: (FSAxisType) axisType
{	
	[newAxisParameters retain];
	[axisParameters[axisType] release];
	axisParameters[axisType] = newAxisParameters;
	
// Remove old plots
	
	for (int i = 0; i < [plots[axisType] count]; ++i) 
		[graph removePlot: [plots[axisType] objectAtIndex: i]];	
	[plots[axisType] release];
	plots[axisType] = nil;
	
	if (newAxisParameters != nil) 
	{
		
// Create new plots
		
		NSMutableArray *newPlots = [[NSMutableArray alloc] init];
		for (int i = 0; i < [newAxisParameters count]; ++i)
		{
			NSString *thisName = [newAxisParameters.dataNames objectAtIndex: i];
			CPColor *plotColor = (CPColor *)[newAxisParameters.colorArray objectAtIndex: i];
			
			CPScatterPlot *thisPlot = [[[CPScatterPlot alloc] init] autorelease];
			[newPlots addObject: thisPlot];
			
			thisPlot.identifier = [NSString stringWithFormat: @"%@ vs %@ plot", thisName, xName];
			thisPlot.dataLineStyle.lineWidth = 3.0f;
			thisPlot.dataLineStyle.lineColor = plotColor;
			NSString *xKeyPath = [NSString stringWithFormat: @"arrangedObjects.%@", xName];
			[thisPlot bind: CPScatterPlotBindingXValues toObject: dataPoints withKeyPath: xKeyPath options: nil];
			NSString *plotKeyPath = [NSString stringWithFormat: @"arrangedObjects.%@", thisName];
			[thisPlot bind: CPScatterPlotBindingYValues toObject: dataPoints withKeyPath: plotKeyPath options: nil];
		}
		
		plots[axisType] = newPlots;
		for (int i = 0; i < [plots[axisType] count]; ++i)
			[graph addPlot: [plots[axisType] objectAtIndex: i] toPlotSpace: plotSpaces[axisType]];
						
// Set up axis title
		
		yAxes[axisType].title = newAxisParameters.axisLabel;
		CPTextLayer *titleLayer = (CPTextLayer *)(yAxes[axisType].axisTitle.contentLayer);
		titleLayer.textStyle.color = (CPColor *)[newAxisParameters.colorArray objectAtIndex: 0];

// Rescale plot space
	
		[self rescalePlotSpace: axisType];
	}

	NSMutableArray *newAxes = [NSMutableArray arrayWithObject: xAxis];
	for (int i = 0; i < FSAxisTypeCount; ++i) 
	{
		if (axisParameters[i] != nil) [newAxes addObject: yAxes[i]];
	}
	CPXYAxisSet *axisSet = (CPXYAxisSet *)graph.axisSet;
	axisSet.axes = newAxes;
}

-(PlotAxisParameters *) parametersForAxis: (FSAxisType) axisType
{
	return axisParameters[axisType];
}

-(void) relabelAxes
{
	[xAxis relabel];
	for (int i = 0; i < FSAxisTypeCount; ++i) 
		if (axisParameters[i] != nil) [yAxes[i] relabel];
}

-(void) setVisibleRange: (PlotRange *) newVisibleRange
{
	[newVisibleRange retain];
	[visibleRange release];
	visibleRange = newVisibleRange;
	
	for (int i = 0; i < FSAxisTypeCount; ++i) [self rescalePlotSpace: i];
}

-(PlotRange *) visibleRange
{
	return visibleRange;
}

-(void) rescalePlotSpace: (FSAxisType) axisType
{

// Find graph range
	
	BOOL isFirst = YES;
	double yMin, yMax;
	
	NSUInteger minIndex = ceil(visibleRange.minValue);
	NSUInteger maxIndex = floor(visibleRange.maxValue);
		
	for (int j = 0; j < [axisParameters[axisType] count]; ++j)
	{

// Check interior values
		
		NSString *thisName = [axisParameters[axisType].dataNames objectAtIndex: j];
		for (NSUInteger i = minIndex; i <= maxIndex; ++i)
		{
			NSDictionary *thisDataPoint = [[dataPoints arrangedObjects] objectAtIndex: i];
			double thisValue = [[thisDataPoint valueForKey: thisName] doubleValue];

			if (isFirst)
			{
				yMin = thisValue;
				yMax = thisValue;
				isFirst = NO;
			}
			else
			{
				if (thisValue < yMin) yMin = thisValue;
				if (thisValue > yMax) yMax = thisValue;
			}
		}

// Check boundaries
		
		double leftIndex = visibleRange.minValue;
		if (minIndex != leftIndex)
		{
			NSDictionary *lowDataPoint = [[dataPoints arrangedObjects] objectAtIndex: minIndex - 1];
			double yLow = [[lowDataPoint valueForKey: thisName] doubleValue];
			NSDictionary *highDataPoint = [[dataPoints arrangedObjects] objectAtIndex: minIndex];
			double yHigh = [[highDataPoint valueForKey: thisName] doubleValue];
			double thisValue = (yLow - yHigh) * (minIndex - leftIndex) + yHigh;

			if (isFirst)
			{
				yMin = thisValue;
				yMax = thisValue;
				isFirst = NO;
			}
			else
			{
				if (thisValue < yMin) yMin = thisValue;
				if (thisValue > yMax) yMax = thisValue;
			}
		}
		
		double rightIndex = visibleRange.maxValue;
		if (maxIndex != rightIndex)
		{
			NSDictionary *lowDataPoint = [[dataPoints arrangedObjects] objectAtIndex: maxIndex];
			double yLow = [[lowDataPoint valueForKey: thisName] doubleValue];
			NSDictionary *highDataPoint = [[dataPoints arrangedObjects] objectAtIndex: maxIndex + 1];
			double yHigh = [[highDataPoint valueForKey: thisName] doubleValue];
			double thisValue = (yHigh - yLow) * (rightIndex - maxIndex) + yLow;
			
			if (isFirst)
			{
				yMin = thisValue;
				yMax = thisValue;
				isFirst = NO;
			}
			else
			{
				if (thisValue < yMin) yMin = thisValue;
				if (thisValue > yMax) yMax = thisValue;
			}
		}		
	}
	
	double border = 0.2 * (yMax - yMin);
	
	if (axisParameters[axisType].includeZero && (yMin >= 0.0)) yMin = 0.0;
	else yMin -= border;
	
	if (axisParameters[axisType].includeZero && (yMax <= 0.0)) yMax = 0.0;
	else yMax += border;
	
	
// Set graph scales

	double xMin, xMax;
	[self getXValue: &xMin forIndexValue: visibleRange.minValue];
	[self getXValue: &xMax forIndexValue: visibleRange.maxValue];

	annotationPlotSpace.xRange = [CPPlotRange plotRangeWithLocation: CPDecimalFromFloat(xMin)
															 length: CPDecimalFromFloat(xMax - xMin)];
	
	xAxis.visibleRange = [CPPlotRange plotRangeWithLocation: CPDecimalFromFloat(xMin)
													 length: CPDecimalFromFloat(xMax - xMin)];
	
	plotSpaces[axisType].xRange = [CPPlotRange plotRangeWithLocation: CPDecimalFromFloat(xMin)
															  length: CPDecimalFromFloat(xMax - xMin)];
	plotSpaces[axisType].yRange = [CPPlotRange plotRangeWithLocation: CPDecimalFromFloat(yMin)
															  length: CPDecimalFromFloat(yMax - yMin)];	
	
	yAxes[axisType].visibleRange = [CPPlotRange plotRangeWithLocation: CPDecimalFromFloat(yMin)
															   length: CPDecimalFromFloat(yMax - yMin)];

	if (axisType == FSAxisTypeLeft) yAxes[axisType].orthogonalCoordinateDecimal = CPDecimalFromFloat(xMin);
	else yAxes[axisType].orthogonalCoordinateDecimal = CPDecimalFromFloat(xMax);
}

-(void) renderToContext: (CGContextRef) renderContext inBounds: (CGRect) renderBounds
{
	NSRect simpleBounds = NSMakeRect(0.0, 0.0, renderBounds.size.width, renderBounds.size.height);
	PlotController *printController = [[PlotController alloc] initWithArrayController: dataPoints 
																				 view: nil
																			   bounds: simpleBounds];
	
	[printController setXName: xName label: xAxis.title];
	[printController setVisibleRange: [self visibleRange]];
	for (int i = 0; i < FSAxisTypeCount; ++i)
		[printController setParameters: [self parametersForAxis: i] forAxis: i];
	[printController relabelAxes];

	CPPushCGContext(renderContext);
	CGContextSaveGState(renderContext);
	CGContextTranslateCTM(renderContext, renderBounds.origin.x, renderBounds.origin.y); 
	[printController.graph layoutAndRenderInContext: renderContext];
	CGContextRestoreGState(renderContext);
	CPPopCGContext();
	
	[printController autorelease];
}

-(void) openSelectionWithIndexValue: (double) indexValue
{
	double xValue;
	[self getXValue: &xValue forIndexValue: indexValue];
	NSDecimalNumber *newPoint = (NSDecimalNumber *)[NSDecimalNumber numberWithDouble: xValue];
	[[[selectionPoints arrangedObjects] objectAtIndex: 0] setObject: newPoint forKey: @"x"];
	[[[selectionPoints arrangedObjects] objectAtIndex: 1] setObject: newPoint forKey: @"x"];
}

-(void) updateSelectionWithIndexValue: (double) indexValue
{
	double xValue;
	[self getXValue: &xValue forIndexValue: indexValue];
	NSDecimalNumber *newPoint = (NSDecimalNumber *)[NSDecimalNumber numberWithDouble: xValue];
	[[[selectionPoints arrangedObjects] objectAtIndex: 1] setObject: newPoint forKey: @"x"];
}

-(void) closeSelection
{	
	double xStart = [[[[selectionPoints arrangedObjects] objectAtIndex: 0] objectForKey: @"x"] doubleValue];
	double xEnd = [[[[selectionPoints arrangedObjects] objectAtIndex: 1] objectForKey: @"x"] doubleValue];
	
	double minIndex, maxIndex;
	[self getIndexValue: &minIndex forXValue: (xStart < xEnd) ? xStart : xEnd];
	[self getIndexValue: &maxIndex forXValue: (xStart < xEnd) ? xEnd : xStart];
	
	double location = minIndex;
	double length = maxIndex - minIndex;

	if (length > 0.0) self.visibleRange = [PlotRange plotRangeWithLocation: location length: length];

	NSDecimalNumber *zeroPoint = (NSDecimalNumber *)[NSDecimalNumber numberWithDouble: 0.0];
	[[[selectionPoints arrangedObjects] objectAtIndex: 0] setObject: zeroPoint forKey: @"x"];
	[[[selectionPoints arrangedObjects] objectAtIndex: 1] setObject: zeroPoint forKey: @"x"];
}

-(void) openMeasurementWithViewpoint: (CGPoint) viewPoint
{	
	double xValue = [self xValueForPoint: viewPoint];
	NSDecimalNumber *x = (NSDecimalNumber *)[NSDecimalNumber numberWithDouble: xValue];
	double yValue = [self selectionValueForPoint: viewPoint];
	NSDecimalNumber *y = (NSDecimalNumber *)[NSDecimalNumber numberWithDouble: yValue];

	[[[measuringPoints arrangedObjects] objectAtIndex: 0] setObject: x forKey: @"x"];
	[[[measuringPoints arrangedObjects] objectAtIndex: 0] setObject: y forKey: @"y"];

	[[[measuringPoints arrangedObjects] objectAtIndex: 1] setObject: x forKey: @"x"];
	[[[measuringPoints arrangedObjects] objectAtIndex: 1] setObject: y forKey: @"y"];

	[graph addPlot: measuringPlot toPlotSpace: annotationPlotSpace];
}

-(void) updateMeasurementWithViewpoint: (CGPoint) viewPoint
{
	double xValue = [self xValueForPoint: viewPoint];
	NSDecimalNumber *x = (NSDecimalNumber *)[NSDecimalNumber numberWithDouble: xValue];
	double yValue = [self selectionValueForPoint: viewPoint];
	NSDecimalNumber *y = (NSDecimalNumber *)[NSDecimalNumber numberWithDouble: yValue];
	
	[[[measuringPoints arrangedObjects] objectAtIndex: 1] setObject: x forKey: @"x"];
	[[[measuringPoints arrangedObjects] objectAtIndex: 1] setObject: y forKey: @"y"];
}

-(void) closeMeasurement
{
	[graph removePlot: measuringPlot];
}

-(NSRect) graphAreaBounds
{
	CALayer *plotLayer = (CALayer *)(graph.plotAreaFrame.plotArea);
	return NSMakeRect(plotLayer.frame.origin.x + graph.paddingLeft, plotLayer.frame.origin.y + graph.paddingBottom,
					  plotLayer.frame.size.width, plotLayer.frame.size.height);
}

-(CGPoint) graphCoordinatesForEvent: (NSEvent *) event
{
	NSPoint windowLocation = [event locationInWindow];
	NSView *windowContentView = [[view window] contentView];
	NSPoint viewLocation = [view convertPoint: windowLocation fromView: windowContentView];
	
	CGPoint viewPoint;
	CALayer *plotLayer = (CALayer *)(graph.plotAreaFrame.plotArea);
	CGPoint plotOrigin = plotLayer.frame.origin;
	viewPoint.x = viewLocation.x - plotOrigin.x - graph.paddingLeft;
	viewPoint.y = viewLocation.y - plotOrigin.y - graph.paddingBottom;
	
	return viewPoint;
}

-(BOOL) getXValue: (double *) xValue forIndexValue: (double) indexValue
{
	if ((indexValue < 0.0) || (indexValue > [[dataPoints arrangedObjects] count] - 1)) return NO;

	NSUInteger minIndex = (int)indexValue;
	double minValue = [[[[dataPoints arrangedObjects] objectAtIndex: minIndex] valueForKey: xName] doubleValue];
	if (indexValue == minIndex) *xValue = minValue;
	else
	{
		NSUInteger maxIndex = minIndex + 1;
		double maxValue = [[[[dataPoints arrangedObjects] objectAtIndex: maxIndex] valueForKey: xName] doubleValue];
		*xValue = (indexValue - minIndex) * (maxValue - minValue) + minValue;
	}
	return YES;
}

-(BOOL) getIndexValue: (double *) indexValue forXValue: (double) xValue
{
	NSUInteger minIndex = 0;
	NSUInteger maxIndex = [[dataPoints arrangedObjects] count] - 1;
	while (maxIndex - minIndex > 1)
	{
		NSUInteger i = (maxIndex + minIndex) / 2;
		double thisValue = [[[[dataPoints arrangedObjects] objectAtIndex: i] valueForKey: xName] doubleValue];
		if (thisValue > xValue) maxIndex = i;
		else minIndex = i;
	}
	
	double minValue = [[[[dataPoints arrangedObjects] objectAtIndex: minIndex] valueForKey: xName] doubleValue];
	double maxValue = [[[[dataPoints arrangedObjects] objectAtIndex: maxIndex] valueForKey: xName] doubleValue];
	if ((xValue < minValue) || (xValue > maxValue)) return NO;
	
	*indexValue = minIndex + (xValue - minValue) / (maxValue - minValue);
	return YES;
}

-(BOOL) pointIsInPlot: (CGPoint) viewPoint
{
	CALayer *plotLayer = (CALayer *)(graph.plotAreaFrame.plotArea);
	return ((viewPoint.x >= 0.0) && (viewPoint.x <= plotLayer.frame.size.width) && 
			(viewPoint.y >= 0.0) && (viewPoint.y <= plotLayer.frame.size.height));
}

-(double) xValueForPoint: (CGPoint) viewPoint
{
	NSDecimal plotPoint[2];
	[annotationPlotSpace plotPoint: plotPoint forPlotAreaViewPoint: viewPoint];
	return CPDecimalFloatValue(plotPoint[0]);
}

-(double) yValueForPoint: (CGPoint) viewPoint onAxis: (FSAxisType) axisType
{
	NSDecimal plotPoint[2];
	[plotSpaces[axisType] plotPoint: plotPoint forPlotAreaViewPoint: viewPoint];
	return CPDecimalFloatValue(plotPoint[1]);
}

-(double) selectionValueForPoint: (CGPoint) viewPoint
{
	NSDecimal plotPoint[2];
	[annotationPlotSpace plotPoint: plotPoint forPlotAreaViewPoint: viewPoint];
	return CPDecimalFloatValue(plotPoint[1]);
}

-(BOOL)plotSpace:(CPPlotSpace *)space shouldHandlePointingDeviceDownEvent:(id)event atPoint:(CGPoint)point;
{
	NSEvent *nativeEvent = event;
	return [delegate plotController: self shouldHandlePointingDeviceDownEvent: nativeEvent atPoint: point];	
}

-(BOOL)plotSpace:(CPPlotSpace *)space shouldHandlePointingDeviceDraggedEvent:(id)event atPoint:(CGPoint)point;
{
	NSEvent *nativeEvent = event;
	return [delegate plotController: self shouldHandlePointingDeviceDraggedEvent: nativeEvent atPoint: point];	
}

-(BOOL)plotSpace:(CPPlotSpace *)space shouldHandlePointingDeviceUpEvent:(id)event atPoint:(CGPoint)point
{
	NSEvent *nativeEvent = event;
	return [delegate plotController: self shouldHandlePointingDeviceUpEvent: nativeEvent atPoint: point];	
}

@end
