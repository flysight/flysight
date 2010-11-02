#import <Cocoa/Cocoa.h>
#import <CorePlot/CorePlot.h>

@class PlotRange;
@class PlotAxisParameters;
@class PlotController;

@protocol FSPlotControllerDelegate <NSObject>

@optional

-(BOOL) plotController: (PlotController *) controller
	shouldHandlePointingDeviceDownEvent: (NSEvent *) event atPoint: (CGPoint) point;
-(BOOL) plotController: (PlotController *) controller 
	shouldHandlePointingDeviceDraggedEvent: (NSEvent *) event atPoint: (CGPoint) point;
-(BOOL) plotController: (PlotController *) controller
	shouldHandlePointingDeviceUpEvent: (NSEvent *) event atPoint: (CGPoint) point;

@end

typedef enum {
	FSAxisTypeLeft = 0,
	FSAxisTypeRight,
	FSAxisTypeCount
} FSAxisType;

@interface PlotController : NSObject <CPPlotSpaceDelegate>
{
	__weak id <FSPlotControllerDelegate> delegate;

	NSString *xName;
	CPLayerHostingView *view;
	CPXYGraph *graph;
	
	PlotRange *visibleRange;

	CPXYPlotSpace *annotationPlotSpace;
	CPXYAxis *xAxis;
	NSArrayController *selectionPoints;	
	CPScatterPlot *selectionPlot;
	NSArrayController *measuringPoints;
	CPScatterPlot *measuringPlot;
	
	PlotAxisParameters *axisParameters[FSAxisTypeCount];
	NSArray *plots[FSAxisTypeCount];
	CPXYPlotSpace *plotSpaces[FSAxisTypeCount];
	CPXYAxis *yAxes[FSAxisTypeCount];
	
	NSArrayController *dataPoints;
}

-(id) initWithArrayController: (NSArrayController *) initArrayController view: (NSView *) initView bounds: (NSRect) initBounds;

-(void) setXName: (NSString *) newXName label: (NSString *) newXLabel;
-(void) setParameters: (PlotAxisParameters *) newAxisParameters forAxis: (FSAxisType) axisType;
-(PlotAxisParameters *) parametersForAxis: (FSAxisType) axisType;

-(void) relabelAxes;

-(void) setVisibleRange: (PlotRange *) newVisibleRange;
-(PlotRange *) visibleRange;

-(void) rescalePlotSpace: (FSAxisType) axisType;

-(void) renderToContext: (CGContextRef) renderContext inBounds: (CGRect) renderBounds;

-(void) openSelectionWithIndexValue: (double) indexValue;
-(void) updateSelectionWithIndexValue: (double) indexValue;
-(void) closeSelection;

-(void) openMeasurementWithViewpoint: (CGPoint) viewPoint;
-(void) updateMeasurementWithViewpoint: (CGPoint) viewPoint;
-(void) closeMeasurement;

-(NSRect) graphAreaBounds;

-(CGPoint) graphCoordinatesForEvent: (NSEvent *) event;
-(BOOL) getXValue: (double *) xValue forIndexValue: (double) indexValue;
-(BOOL) getIndexValue: (double *) indexValue forXValue: (double) xValue;
-(BOOL) pointIsInPlot: (CGPoint) viewPoint;
-(double) xValueForPoint: (CGPoint) viewPoint;
-(double) yValueForPoint: (CGPoint) viewPoint onAxis: (FSAxisType) axisType;
-(double) selectionValueForPoint: (CGPoint) viewPoint;

@property (nonatomic, readwrite, assign) __weak id <FSPlotControllerDelegate> delegate;
@property (nonatomic, readonly) CPXYGraph *graph;

@end
