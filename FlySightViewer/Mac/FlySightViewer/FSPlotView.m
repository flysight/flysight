#import "FSPlotView.h"
#import "PlotController.h"

@implementation FSPlotView

@synthesize plotController;
@synthesize graphCursor;

-(void) resetCursorRects
{
    [self addCursorRect: [plotController graphAreaBounds] cursor: graphCursor];
}

@end
