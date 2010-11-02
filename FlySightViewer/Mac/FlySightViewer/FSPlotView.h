#import <Cocoa/Cocoa.h>
#import <CorePlot/CorePlot.h>

@class PlotController;

@interface FSPlotView : CPLayerHostingView
{
	__weak PlotController *plotController;
	NSCursor *graphCursor;
}

@property (nonatomic, readwrite, assign) __weak PlotController *plotController;
@property (nonatomic, readwrite, retain) NSCursor *graphCursor;

@end
