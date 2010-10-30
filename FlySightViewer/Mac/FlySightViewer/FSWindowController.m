#import "FSWindowController.h"
#import "PlotAxisParameters.h"
#import "ToolTip.h"
#import "PlotRange.h"
#import "FSPlotView.h"

double selectionRootIndex;
double scrollLastXValue;

void setToolbarPopUpImage(NSToolbarItem *item, NSString *imageName)
{
	[[(NSPopUpButton *)[item view] itemAtIndex: 0] setImage: [NSImage imageNamed: imageName]];
}

@implementation FSWindowController

@synthesize toolType;
@synthesize layoutType;
@synthesize unitType;

+(void) initialize
{	
	NSMutableDictionary *appDefaults = [[NSMutableDictionary alloc] init];
	[appDefaults setObject: [NSNumber numberWithInt: FSToolTypeCrop] forKey: @"toolType"];
	[appDefaults setObject: [NSNumber numberWithInt: FSLayoutTypeSingle] forKey: @"layoutType"];
	[appDefaults setObject: [NSNumber numberWithInt: FSUnitTypeMetric] forKey: @"unitType"];
	[appDefaults setObject: [NSNumber numberWithInt: FSDisplayElementTime] forKey: @"xDisplayElement"];
	[appDefaults setObject: [NSNumber numberWithInt: FSDisplayElementVDown] forKey: @"lDisplayElement"];
	[appDefaults setObject: [NSNumber numberWithInt: FSDisplayElementVHorizontal] forKey: @"rDisplayElement"];
	
	[[NSUserDefaults standardUserDefaults] registerDefaults: appDefaults];
	[appDefaults release];
}

-(id) initWithDataManager: (FSDataManager *) initDataManager
{
	self = [super initWithWindowNibName: @"GraphView"];
	if (self != nil)
	{
		dataManager = [initDataManager retain];
		xParameters = nil;

		singlePlotController = nil;
		upperPlotController = nil;
		lowerPlotController = nil;
	
		cropList = [[NSMutableArray alloc] init];

		unitType = FSUnitTypeMetric;
		[dataManager setDisplayUnitType: unitType];
	}
	
	return self;
}

-(void) awakeFromNib
{
	[self.window makeKeyAndOrderFront: self];
	
// Set up plot controllers
	
	singlePlotController = [[PlotController alloc] initWithArrayController: dataManager.arrayController
																	  view: singleView
																	bounds: singleView.bounds];
	singlePlotController.delegate = self;
	singleView.plotController = singlePlotController;
	
	upperPlotController = [[PlotController alloc] initWithArrayController: dataManager.arrayController 
																	 view: upperView
																   bounds: upperView.bounds];
	upperPlotController.delegate = self;
	upperView.plotController = upperPlotController;
	
	lowerPlotController = [[PlotController alloc] initWithArrayController: dataManager.arrayController 
																	 view: lowerView
																   bounds: lowerView.bounds];
	lowerPlotController.delegate = self;
	lowerView.plotController = lowerPlotController;
	
// Populate axis selectors
	
	[self addDisplayElement: FSDisplayElementTime withName: @"Time" 
			 toSelectorType: FSSelectorTypeDomain];
	[self addDisplayElement: FSDisplayElementDistance withName: @"Horizontal distance" 
			 toSelectorType: FSSelectorTypeDomain];
	
	[self addDisplayElement: FSDisplayElementElevation withName: @"Elevation" 
			 toSelectorType: FSSelectorTypeLeft];
	[self addDisplayElement: FSDisplayElementVHorizontal withName: @"Horizontal speed" 
			 toSelectorType: FSSelectorTypeLeft];
	[self addDisplayElement: FSDisplayElementVDown withName: @"Vertical speed" 
			 toSelectorType: FSSelectorTypeLeft];
	[self addDisplayElement: FSDisplayElementVTotal withName: @"Total speed" 
			 toSelectorType: FSSelectorTypeLeft];
	[self addDisplayElement: FSDisplayElementGlideRatio withName: @"Glide ratio" 
			 toSelectorType: FSSelectorTypeLeft];
	
	[self addDisplayElement: FSDisplayElementNone withName: @"None" 
			 toSelectorType: FSAxisTypeRight];
	[self addDisplayElement: FSDisplayElementElevation withName: @"Elevation" 
			 toSelectorType: FSSelectorTypeRight];
	[self addDisplayElement: FSDisplayElementVHorizontal withName: @"Horizontal speed" 
			 toSelectorType: FSSelectorTypeRight];
	[self addDisplayElement: FSDisplayElementVDown withName: @"Vertical speed" 
			 toSelectorType: FSSelectorTypeRight];
	[self addDisplayElement: FSDisplayElementVTotal withName: @"Total speed" 
			 toSelectorType: FSSelectorTypeRight];
	[self addDisplayElement: FSDisplayElementGlideRatio withName: @"Glide ratio" 
			 toSelectorType: FSSelectorTypeRight];
	
// Initialize cropping

	PlotRange *initRange = [PlotRange plotRangeWithLocation: 0.0 length: [dataManager count] - 1];
	[cropList addObject: initRange];
	
	[cropSelector setEnabled: NO forSegment: 0];
	[cropSelector setEnabled: NO forSegment: 1];
	
// Set up selectors

	[self setXDisplayElement: [[NSUserDefaults standardUserDefaults] integerForKey: @"xDisplayElement"]];
	[self setDisplayElement: [[NSUserDefaults standardUserDefaults] integerForKey: @"lDisplayElement"]
					forAxis: FSAxisTypeLeft];
	[self setDisplayElement: [[NSUserDefaults standardUserDefaults] integerForKey: @"rDisplayElement"]
					forAxis: FSAxisTypeRight];

	self.toolType = [[NSUserDefaults standardUserDefaults] integerForKey: @"toolType"];	
	self.unitType = [[NSUserDefaults standardUserDefaults] integerForKey: @"unitType"];
	self.layoutType = [[NSUserDefaults standardUserDefaults] integerForKey: @"layoutType"];
}
	
-(void) dealloc
{
	[cropList release];
	
	[dataManager release];
	[xParameters release];
	
	[singlePlotController release];
	[upperPlotController release];
	[lowerPlotController release];
	
	[super dealloc];
}

-(void) setToolType: (FSToolType) newType
{
	toolType = newType;
	[[NSUserDefaults standardUserDefaults] setInteger: toolType forKey: @"toolType"];
	
	switch (toolType)
	{
		case FSToolTypeMeasure:
			toolSelector.selectedSegment = 0;
			[self setCursors: [NSCursor crosshairCursor]];
			break;
		case FSToolTypeScroll:
			toolSelector.selectedSegment = 1;
			[self setCursors: [NSCursor openHandCursor]];
			break;
		case FSToolTypeCrop:
			toolSelector.selectedSegment = 2;
			[self setCursors: [NSCursor arrowCursor]];
			break;
	}
}

-(void) setLayoutType: (FSLayoutType) newType
{
	layoutType = newType;
	[[NSUserDefaults standardUserDefaults] setInteger: layoutType forKey: @"layoutType"];
	
	if (layoutType == FSLayoutTypeSingle)
	{
		[layoutSelector setSelectedSegment: 0];
		[tabView selectTabViewItemAtIndex: 0];
		[singlePlotController relabelAxes];		
		
		setToolbarPopUpImage(leftAxisSelector, @"single_left");
		[leftAxisSelector setLabel: @"Left"];
		setToolbarPopUpImage(rightAxisSelector, @"single_right");
		[rightAxisSelector setLabel: @"Right"];
		setToolbarPopUpImage(domainAxisSelector, @"single_lower");
		[domainAxisSelector setLabel: @"Domain"];		
	}
	else
	{
		[layoutSelector setSelectedSegment: 1];
		[tabView selectTabViewItemAtIndex: 1];
		[upperPlotController relabelAxes];
		[lowerPlotController relabelAxes];
		
		setToolbarPopUpImage(leftAxisSelector, @"stacked_upper");
		[leftAxisSelector setLabel: @"Upper"];
		setToolbarPopUpImage(rightAxisSelector, @"stacked_lower");
		[rightAxisSelector setLabel: @"Lower"];
		setToolbarPopUpImage(domainAxisSelector, @"stacked_domain");
		[domainAxisSelector setLabel: @"Domain"];		
	}	
}

-(void) setUnitType: (FSUnitType) newType
{
	unitType = newType;
	[[NSUserDefaults standardUserDefaults] setInteger: unitType forKey: @"unitType"];

// Update popup button
	
	[unitSelector selectItemAtIndex: unitType];
	
// Update data values
	
	[dataManager setDisplayUnitType: unitType];
	
// Update axes

	NSMenuItem *domainItem = [(NSPopUpButton *)[domainAxisSelector view] selectedItem];
	FSDisplayElement domainDisplayElement = [[domainItem representedObject] intValue];
	[self setXDisplayElement: domainDisplayElement];
	
	NSMenuItem *leftItem = [(NSPopUpButton *)[leftAxisSelector view] selectedItem];
	FSDisplayElement leftDisplayElement = [[leftItem representedObject] intValue];
	[self setDisplayElement: leftDisplayElement forAxis: FSAxisTypeLeft];
	
	NSMenuItem *rightItem = [(NSPopUpButton *)[rightAxisSelector view] selectedItem];
	FSDisplayElement rightDisplayElement = [[rightItem representedObject] intValue];
	[self setDisplayElement: rightDisplayElement forAxis: FSAxisTypeRight];
}

-(void) addDisplayElement: (FSDisplayElement) displayElement withName: (NSString *) name
	   toSelectorType: (FSSelectorType) selectorType
{
	NSToolbarItem *selector;
	switch(selectorType)
	{
		case FSSelectorTypeLeft: 
			selector = leftAxisSelector;
			break;
		case FSSelectorTypeRight: 
			selector = rightAxisSelector;
			break;
		case FSSelectorTypeDomain: 
			selector = domainAxisSelector;
			break;
	}
	
	NSMenu *menu = [(NSPopUpButton *)[selector view] menu];
	NSMenuItem *menuItem = [[[NSMenuItem alloc] initWithTitle: name
													   action: NULL 
												keyEquivalent: @""] autorelease];
	[menuItem setRepresentedObject: [NSNumber numberWithInt: displayElement]];
	[menu addItem: menuItem];
}

-(void) setXDisplayElement: (FSDisplayElement) newElement
{
	NSPopUpButton *domainButton  = (NSPopUpButton *)[domainAxisSelector view];
	for (NSMenuItem *item in [domainButton itemArray]) 
	{
		if ([[item representedObject] intValue] == newElement) 
		{
			[domainButton selectItem: item];
			[item setState: NSOnState];
		}
		else [item setState: NSOffState];
	}
	
	[[NSUserDefaults standardUserDefaults] setInteger: newElement forKey: @"xDisplayElement"];

	PlotAxisParameters *newParameters = [FSDataManager parametersForDisplayElement: newElement];

	[newParameters retain];
	[xParameters release];
	xParameters = newParameters;
	
	NSString *xLabel = newParameters.axisLabel;
	NSString *xDataName = [newParameters.dataNames lastObject];
	
	[singlePlotController setXName: xDataName label: xLabel];
	[upperPlotController setXName: xDataName label: xLabel];
	[lowerPlotController setXName: xDataName label: xLabel];
}

-(void) setDisplayElement: (FSDisplayElement) newElement forAxis: (FSAxisType) axisType
{
	PlotAxisParameters *newParameters = [FSDataManager parametersForDisplayElement: newElement];
	
	if (axisType == FSAxisTypeLeft)
	{
		NSPopUpButton *leftButton  = (NSPopUpButton *)[leftAxisSelector view];
		for (NSMenuItem *item in [leftButton itemArray]) 
		{
			if ([[item representedObject] intValue] == newElement) 
			{
				[leftButton selectItem: item];
				[item setState: NSOnState];
			}
			else [item setState: NSOffState];
		}

		[[NSUserDefaults standardUserDefaults] setInteger: newElement forKey: @"lDisplayElement"];
		
		newParameters.colorArray = [PlotColorArray blueColorArray];
		[singlePlotController setParameters: newParameters forAxis: FSAxisTypeLeft];
		[upperPlotController setParameters: newParameters forAxis: FSAxisTypeLeft];
	}
	else
	{
		NSPopUpButton *rightButton  = (NSPopUpButton *)[rightAxisSelector view];
		for (NSMenuItem *item in [rightButton itemArray]) 
		{
			if ([[item representedObject] intValue] == newElement) 
			{
				[rightButton selectItem: item];
				[item setState: NSOnState];
			}
			else [item setState: NSOffState];
		}
		
		[[NSUserDefaults standardUserDefaults] setInteger: newElement forKey: @"rDisplayElement"];

		if (newParameters == nil)
		{
			[self setLayoutType: FSLayoutTypeSingle];
			[layoutSelector setEnabled: NO forSegment: 1];
		}
		else [layoutSelector setEnabled: YES forSegment: 1];
		
		newParameters.colorArray = [PlotColorArray redColorArray];
		[singlePlotController setParameters: newParameters forAxis: FSAxisTypeRight];
		[lowerPlotController setParameters: newParameters forAxis: FSAxisTypeLeft];		
	}
}

-(void) setCursors: (NSCursor *) cursor
{
	upperView.graphCursor = cursor;
	[self.window invalidateCursorRectsForView: upperView];
	lowerView.graphCursor = cursor;
	[self.window invalidateCursorRectsForView: lowerView];
	singleView.graphCursor = cursor;
	[self.window invalidateCursorRectsForView: singleView];
}

-(IBAction) toolSelectorClicked: (id) sender
{
	int clickedSegment = [sender selectedSegment];
	switch (clickedSegment)
	{
		case 0:
			self.toolType = FSToolTypeMeasure;
			break;
		case 1:
			self.toolType = FSToolTypeScroll;
			break;
		case 2:
			self.toolType = FSToolTypeCrop;
			break;
	}
}

-(IBAction) layoutSelectorClicked: (id) sender
{
	int clickedSegment = [sender selectedSegment];
	[self setLayoutType: clickedSegment];
}

-(IBAction) cropSelectorClicked: (id) sender
{
	int clickedSegment = [sender selectedSegment];
	if (clickedSegment == 0) cropListIndex--;
	else cropListIndex++;

	[cropSelector setEnabled: (cropListIndex > 0) forSegment: 0];
	[cropSelector setEnabled: (cropListIndex < [cropList count] - 1) forSegment: 1];
	
	PlotRange *currentCrop = [cropList objectAtIndex: cropListIndex];
	singlePlotController.visibleRange = currentCrop;
	upperPlotController.visibleRange = currentCrop;
	lowerPlotController.visibleRange = currentCrop;
}

-(IBAction) leftAxisTypeChanged: (id) sender
{
	FSDisplayElement displayElement = [[[sender selectedItem] representedObject] intValue];
	[self setDisplayElement: displayElement forAxis: FSAxisTypeLeft];
}

-(IBAction) rightAxisTypeChanged: (id) sender
{
	for (NSMenuItem *item in [sender itemArray]) [item setState: NSOffState];
	[[sender selectedItem] setState: NSOnState];
	FSDisplayElement displayElement = [[[sender selectedItem] representedObject] intValue];
	[self setDisplayElement: displayElement forAxis: FSAxisTypeRight];
}

-(IBAction) domainAxisTypeChanged: (id) sender
{
	for (NSMenuItem *item in [sender itemArray]) [item setState: NSOffState];
	[[sender selectedItem] setState: NSOnState];
	FSDisplayElement displayElement = [[[sender selectedItem] representedObject] intValue];
	[self setXDisplayElement: displayElement];
}

-(IBAction) unitsChanged: (id) sender
{
	for (NSMenuItem *item in [sender itemArray]) [item setState: NSOffState];
	[[sender selectedItem] setState: NSOnState];
	
	NSString *name = [[sender selectedItem] title];
	FSUnitType newUnitType;
	if ([name compare: @"Metric"] == NSOrderedSame) newUnitType = FSUnitTypeMetric;
	else newUnitType = FSUnitTypeImperial;

	self.unitType = newUnitType;
}

-(IBAction) print: (id) sender
{
	
// Open a print dialog to get print info
	
	NSPrintInfo *printInfo = [NSPrintInfo sharedPrintInfo];
	NSPrintPanel *printPanel = [NSPrintPanel printPanel];
	if ([printPanel runModalWithPrintInfo: printInfo] == NSOKButton)
	{

// Get print manager details from print info
		
		PMPrintSession printSession = (PMPrintSession)[printInfo PMPrintSession];
		PMPageFormat pageFormat = (PMPageFormat)[printInfo PMPageFormat];
		PMPrintSettings printSettings = (PMPrintSettings)[printInfo PMPrintSettings];

// Begin document and page (since we're only printing one page)
	
		PMSessionBeginCGDocumentNoDialog(printSession, printSettings, pageFormat);
		PMSessionBeginPageNoDialog(printSession, pageFormat, NULL);

// Open up a print context to draw into
		
		CGContextRef printingContext;
		PMSessionGetCGGraphicsContext(printSession, &printingContext);

// Get printable area
		
		PMPaper paper;
		PMPaperMargins margins;
		PMOrientation orientation = kPMPortrait;
		double paperWidth, paperHeight;
		
		PMGetPageFormatPaper(pageFormat, &paper);
		PMGetOrientation(pageFormat, &orientation);
		PMPaperGetWidth(paper, &paperWidth);
		PMPaperGetHeight(paper, &paperHeight);
		PMPaperGetMargins(paper, &margins);
		
		if ((orientation == kPMLandscape) || (orientation == kPMReverseLandscape))
		{

// Landscape paper sizes have the width and height reversed.

			double temp = paperWidth;
			paperWidth = paperHeight;
			paperHeight = temp;
		}

		CGRect printBounds = CGRectMake(margins.left, margins.bottom,
										(paperWidth - margins.left - margins.right),
										(paperHeight - margins.top - margins.bottom));
		
// Draw graphs

		if (layoutType == FSLayoutTypeSingle)
		{
			[singlePlotController renderToContext: printingContext inBounds: printBounds];
		}
		else
		{
			double upperHeight = upperView.bounds.size.height;
			double lowerHeight = lowerView.bounds.size.height;
			double upperFraction = upperHeight / (upperHeight + lowerHeight);
			double lowerFraction = lowerHeight / (upperHeight + lowerHeight);
			
			CGRect upperRect = CGRectMake(printBounds.origin.x, 
										  printBounds.origin.y + printBounds.size.height * lowerFraction,
										  printBounds.size.width, printBounds.size.height * upperFraction);
			[upperPlotController renderToContext: printingContext inBounds: upperRect];

			CGRect lowerRect = CGRectMake(printBounds.origin.x, printBounds.origin.y,
										  printBounds.size.width, printBounds.size.height * lowerFraction);
			[lowerPlotController renderToContext: printingContext inBounds: lowerRect];
		}

				
// Close page and document
		
		PMSessionEndPageNoDialog(printSession);
		PMSessionEndDocumentNoDialog(printSession);
	}
}

-(IBAction) exportToCSV: (id) sender
{
	NSSavePanel *exportDialog = [NSSavePanel savePanel];
	[exportDialog setAccessoryView: exportView];
	[exportDialog setRequiredFileType:@"csv"];
	
	if ([exportDialog runModal] == NSOKButton)
	{
		NSURL *url = [exportDialog URL];
		if ([url isFileURL])
		{
			NSRange exportRange;
			if ([exportCroppedButton state] == 0) exportRange = NSMakeRange(0, [dataManager count]);
			else 
			{
				PlotRange *currentCrop = [cropList objectAtIndex: cropListIndex];
				int minIndex = floor(currentCrop.minValue);
				int maxIndex = ceil(currentCrop.maxValue);
				exportRange = NSMakeRange(minIndex, maxIndex - minIndex);
			}
			
			NSMutableString *string = [[[NSMutableString alloc] init] autorelease];
			[dataManager exportToCSV: string range: exportRange];
			[string writeToURL: url atomically: NO encoding: NSASCIIStringEncoding error: NULL];
		}
	}
}

-(IBAction) exportToNMEA: (id) sender
{
	NSSavePanel *exportDialog = [NSSavePanel savePanel];
	[exportDialog setAccessoryView: exportView];

	if ([exportDialog runModal] == NSOKButton)
	{
		NSURL *url = [exportDialog URL];
		if ([url isFileURL])
		{
			NSRange exportRange;
			if ([exportCroppedButton state] == 0) exportRange = NSMakeRange(0, [dataManager count]);
			else 
			{
				PlotRange *currentCrop = [cropList objectAtIndex: cropListIndex];
				int minIndex = floor(currentCrop.minValue);
				int maxIndex = ceil(currentCrop.maxValue);
				exportRange = NSMakeRange(minIndex, maxIndex - minIndex);
			}
			
			NSMutableString *string = [[[NSMutableString alloc] init] autorelease];
			[dataManager exportToNMEA: string range: exportRange];
			[string writeToURL: url atomically: NO encoding: NSASCIIStringEncoding error: NULL];
		}
	}
}

-(IBAction) exportToKML: (id) sender
{
	NSSavePanel *exportDialog = [NSSavePanel savePanel];
	[exportDialog setAccessoryView: exportView];
	[exportDialog setRequiredFileType:@"kml"];
	
	if ([exportDialog runModal] == NSOKButton)
	{
		NSURL *url = [exportDialog URL];
		if ([url isFileURL])
		{
			NSRange exportRange;
			if ([exportCroppedButton state] == 0) exportRange = NSMakeRange(0, [dataManager count]);
			else 
			{
				PlotRange *currentCrop = [cropList objectAtIndex: cropListIndex];
				int minIndex = floor(currentCrop.minValue);
				int maxIndex = ceil(currentCrop.maxValue);
				exportRange = NSMakeRange(minIndex, maxIndex - minIndex);
			}
			
			NSMutableString *string = [[[NSMutableString alloc] init] autorelease];
			[dataManager exportToKML: string range: exportRange];
			[string writeToURL: url atomically: NO encoding: NSASCIIStringEncoding error: NULL];
		}
	}
}

-(BOOL) plotController: (PlotController *) controller
	shouldHandlePointingDeviceDownEvent: (NSEvent *) event atPoint: (CGPoint) point
{
	CGPoint viewPoint = [controller graphCoordinatesForEvent: event];
	if ([controller pointIsInPlot: viewPoint])
	{
		if (toolType == FSToolTypeMeasure)
		{
			[self updateToolTipForEvent: event controller: controller isDistance: NO];
			[controller openMeasurementWithViewpoint: viewPoint];
		}
		else if (toolType == FSToolTypeScroll)
		{
			[self setCursors: [NSCursor closedHandCursor]];
			scrollLastXValue = [controller xValueForPoint: viewPoint];
		}
		else
		{
			double indexValue;
			[controller getIndexValue: &indexValue forXValue: [controller xValueForPoint: viewPoint]];
			selectionRootIndex = indexValue;

			[singlePlotController openSelectionWithIndexValue: indexValue];
			[upperPlotController openSelectionWithIndexValue: indexValue];
			[lowerPlotController openSelectionWithIndexValue: indexValue];
		}
		return NO;
	}
	
	return YES;
}

-(BOOL) plotController: (PlotController *) controller 
	shouldHandlePointingDeviceDraggedEvent: (NSEvent *) event atPoint: (CGPoint) point
{	
	CGPoint viewPoint = [controller graphCoordinatesForEvent: event];
	if ([controller pointIsInPlot: viewPoint])
	{
		if (toolType == FSToolTypeMeasure)
		{
			[self updateToolTipForEvent: event controller: controller isDistance: YES];
			[controller updateMeasurementWithViewpoint: viewPoint];
		}
		else if (toolType == FSToolTypeScroll)
		{
			double xValue = [controller xValueForPoint: viewPoint];
			double deltaX = scrollLastXValue - xValue;
			
			PlotRange *currentCrop = [cropList objectAtIndex: cropListIndex];
			double newMinX, newMaxX;
			[controller getXValue: &newMinX forIndexValue: currentCrop.minValue];
			[controller getXValue: &newMaxX forIndexValue: currentCrop.maxValue];
			double cropXSize = newMaxX - newMinX;

			newMinX += deltaX;
			newMaxX += deltaX;
			
			double newMinIndex, newMaxIndex;
			if (![controller getIndexValue: &newMinIndex forXValue: newMinX])
			{
				[controller getXValue: &newMinX forIndexValue: 0.0];
				newMaxX = newMinX + cropXSize;
			}
			if (![controller getIndexValue: &newMaxIndex forXValue: newMaxX])
			{
				[controller getXValue: &newMaxX forIndexValue: [dataManager count] - 1];
				newMinX = newMaxX - cropXSize;
			}
			
			[controller getIndexValue: &newMinIndex forXValue: newMinX];
			[controller getIndexValue: &newMaxIndex forXValue: newMaxX];
			currentCrop.location = newMinIndex;
			currentCrop.length = newMaxIndex - newMinIndex;
			
			singlePlotController.visibleRange = currentCrop;
			upperPlotController.visibleRange = currentCrop;
			lowerPlotController.visibleRange = currentCrop;
			
			scrollLastXValue = [controller xValueForPoint: viewPoint];
		}
		else
		{
			double indexValue;
			[controller getIndexValue: &indexValue forXValue: [controller xValueForPoint: viewPoint]];
			
			[singlePlotController updateSelectionWithIndexValue: indexValue];
			[upperPlotController updateSelectionWithIndexValue: indexValue];
			[lowerPlotController updateSelectionWithIndexValue: indexValue];
		}
		return NO;
	}
	
	return YES;
}

-(BOOL) plotController: (PlotController *) controller
	shouldHandlePointingDeviceUpEvent: (NSEvent *) event atPoint: (CGPoint) point
{
	if (toolType == FSToolTypeMeasure)
	{
		[ToolTip release];
		[controller closeMeasurement];
		
		return NO;
	}
	else if (toolType == FSToolTypeScroll)
	{
		[self setCursors: [NSCursor openHandCursor]];
		return NO;
	}
	else
	{		
		CGPoint viewPoint = [controller graphCoordinatesForEvent: event];
		double indexValue;
		[controller getIndexValue: &indexValue forXValue: [controller xValueForPoint: viewPoint]];

		double newLocation = (indexValue < selectionRootIndex) ? indexValue : selectionRootIndex;
		double newLength = fabs(selectionRootIndex - indexValue);

		if (newLength > 0.0)
		{
			
// Clear list items beyond the current one
			
			while ([cropList count] > cropListIndex + 1) [cropList removeLastObject];
			
// Add a new crop list item

			PlotRange *newItem = [PlotRange plotRangeWithLocation: newLocation length: newLength];

			[cropList addObject: newItem];
			cropListIndex++;
			
// Update selector enabling
			
			[cropSelector setEnabled: YES forSegment: 0];
			[cropSelector setEnabled: NO forSegment: 1];
		}
		
		[singlePlotController closeSelection];
		[upperPlotController closeSelection];
		[lowerPlotController closeSelection];
		
		return NO;
	}
	
	return YES;
}

-(void) updateToolTipForEvent: (NSEvent *) event controller: (PlotController *) controller
	isDistance: (BOOL) isDistance
{
	static double startXValue;
	static double startLeftValue;
	static double startRightValue;
	
	CGPoint viewPoint = [controller graphCoordinatesForEvent: event];
	double xValue = [controller xValueForPoint: viewPoint];
	if (!isDistance) startXValue = xValue;
	else xValue -= startXValue;
	
	NSString *format = isDistance ? @"%@ difference: %.2lf %@" : @"%@: %.2lf %@";

	NSMutableString *toolTipString = [NSMutableString stringWithFormat: format,
									  xParameters.axisName, xValue, xParameters.unitName];

	PlotAxisParameters *leftPlotSet = [controller parametersForAxis: FSAxisTypeLeft];
	if (leftPlotSet != nil)
	{
		double value = [controller yValueForPoint: viewPoint onAxis: FSAxisTypeLeft];
		if (!isDistance) startLeftValue = value;
		else value -= startLeftValue;
		[toolTipString appendString: @"\n"];
		[toolTipString appendFormat: format, [leftPlotSet axisName], value, [leftPlotSet unitName]];
	}

	PlotAxisParameters *rightPlotSet = [controller parametersForAxis: FSAxisTypeRight];
	if (rightPlotSet != nil)
	{
		double value = [controller yValueForPoint: viewPoint onAxis: FSAxisTypeRight];
		if (!isDistance) startRightValue = value;
		else value -= startRightValue;
		[toolTipString appendString: @"\n"];
		[toolTipString appendFormat: format, [rightPlotSet axisName], value, [rightPlotSet unitName]];
	}
	
	[ToolTip setString: toolTipString forEvent: event];
}

@end
