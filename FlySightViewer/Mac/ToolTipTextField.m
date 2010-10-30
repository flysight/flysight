#import "ToolTipTextField.h"

@implementation ToolTipTextField

-(void) drawRect: (NSRect) aRect
{
	[super drawRect: aRect];
    
    [[NSColor colorWithCalibratedWhite: 0.925 alpha: 1.0] set];
    NSFrameRect(aRect);
}

@end
