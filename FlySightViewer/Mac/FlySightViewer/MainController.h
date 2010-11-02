#import <Cocoa/Cocoa.h>

@class FSWindowController;
@class FSDataManager;

@interface MainController : NSObject
{
	NSMutableArray *windowControllers;
	NSString *licenseText;
}

-(IBAction) open: (id) sender;
-(IBAction) runPageLayout: (id) sender;

@end
