#import "ToolTip.h"
#import "ToolTipTextField.h"

static ToolTip *sharedToolTip = nil;

@interface ToolTip (Private)

-(void) setString: (NSString *) string forEvent: (NSEvent *) theEvent;

@end

@implementation ToolTip

+(void) setString: (NSString *) string forEvent: (NSEvent *) theEvent
{
	if (sharedToolTip == nil) sharedToolTip = [[ToolTip alloc] init];
	[sharedToolTip setString: string forEvent: theEvent];
}

+(void) release
{
	[sharedToolTip release];
	sharedToolTip = nil;
}

-(id) init
{
	id	retVal = [super init];
    
	if (retVal != nil) 
	{
        
// These size are not really import, just the relation between the two...

        NSRect contentRect = NSMakeRect(0, 0, 100, 20);
        
		window = [[NSWindow alloc] initWithContentRect: contentRect
											 styleMask: NSBorderlessWindowMask
											   backing: NSBackingStoreBuffered
												 defer: YES];
        
        [window setOpaque: YES];
        [window setAlphaValue: 1.00];
        [window setBackgroundColor: [NSColor colorWithDeviceRed: 1.0 green: 0.96 blue: 0.76 alpha: 1.0]];
        [window setHasShadow: YES];
        [window setLevel: NSStatusWindowLevel];
        [window setReleasedWhenClosed: YES];
        [window orderFront: nil];
        
        textField = [[ToolTipTextField alloc] initWithFrame: contentRect];
        [textField setEditable: NO];
        [textField setSelectable: NO];
        [textField setBezeled: NO];
        [textField setBordered: NO];
        [textField setDrawsBackground: NO];
        [textField setAlignment: NSLeftTextAlignment];
        [textField setAutoresizingMask: NSViewWidthSizable | NSViewHeightSizable];
        [textField setFont: [NSFont toolTipsFontOfSize: [NSFont smallSystemFontSize]]];
        [[window contentView] addSubview: textField];
        
        [textField setStringValue: @" "];
        textAttributes = [[[textField attributedStringValue] attributesAtIndex:0 effectiveRange:nil] retain];
    }
    
    return retVal;
}

-(void) dealloc
{
	[window release];
	[textAttributes release];
	[super dealloc];
}

-(void) setString: (NSString *) string forEvent: (NSEvent *) theEvent
{
	NSSize size = [string sizeWithAttributes: textAttributes];
	NSPoint eventLocation = [theEvent locationInWindow];
	NSRect windowFrame = [[theEvent window] frame];
	NSRect screenFrame = [[NSScreen mainScreen] frame];

	NSPoint cursorScreenPosition = [[theEvent window] convertBaseToScreen: eventLocation];

	float cornerX;
    if ((windowFrame.size.width - eventLocation.x < size.width + 20) ||
		(screenFrame.size.width - cursorScreenPosition.x < size.width + 20))
	{
		cornerX = cursorScreenPosition.x - size.width - 10;
	}
	else cornerX = cursorScreenPosition.x + 10;
	
	[textField setStringValue: string];
	[window setFrameOrigin: NSMakePoint(cornerX, cursorScreenPosition.y + 10)];
    
	[window setContentSize: NSMakeSize(size.width + 10, size.height + 1)];
}

@end
