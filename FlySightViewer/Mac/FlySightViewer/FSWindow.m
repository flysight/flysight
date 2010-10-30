#import "FSWindow.h"
#import "FSWindowController.h"

@implementation FSWindow

-(IBAction) print: (id) sender
{
	if ([self.windowController isKindOfClass: [FSWindowController class]])
	{
		[self.windowController print: sender];
	}
	else 
	{
		[super print: sender];
	}
}

@end
