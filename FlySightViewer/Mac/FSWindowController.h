#import <Cocoa/Cocoa.h>
#import <CorePlot/CorePlot.h>
#import "FSDataManager.h"
#import "PlotController.h"

@class PlotAxisParameters;
@class FSDataManager;
@class FSPlotView;

typedef enum
{
	FSToolTypeMeasure = 0,
	FSToolTypeScroll,
	FSToolTypeCrop
} FSToolType;

typedef enum
{
	FSLayoutTypeSingle = 0,
	FSLayoutTypeStacked
} FSLayoutType;

typedef enum
{
	FSSelectorTypeLeft = 0,
	FSSelectorTypeRight,
	FSSelectorTypeDomain
} FSSelectorType;

@interface FSWindowController : NSWindowController <FSPlotControllerDelegate>
{
	IBOutlet NSTabView *tabView;
	IBOutlet FSPlotView *upperView;
	IBOutlet FSPlotView *lowerView;
	IBOutlet FSPlotView *singleView;
	
	IBOutlet NSSegmentedControl *cropSelector;
	IBOutlet NSSegmentedControl *toolSelector;
	IBOutlet NSSegmentedControl *layoutSelector;
	IBOutlet NSPopUpButton *unitSelector;

	IBOutlet NSToolbarItem *leftAxisSelector;
	IBOutlet NSToolbarItem *rightAxisSelector;
	IBOutlet NSToolbarItem *domainAxisSelector;
	
	IBOutlet NSToolbarItem *printItem;
	
	IBOutlet NSView *exportView;
	IBOutlet NSButton *exportCroppedButton;
	
	FSDataManager *dataManager;
	PlotAxisParameters *xParameters;

	PlotController *singlePlotController;
	PlotController *upperPlotController;
	PlotController *lowerPlotController;
	
	NSMutableArray *cropList;
	int cropListIndex;
	
	FSToolType toolType;
	FSLayoutType layoutType;
	FSUnitType unitType;
}

-(id) initWithDataManager: (FSDataManager *) initDataManager;

-(void) addDisplayElement: (FSDisplayElement) displayElement withName: (NSString *) name
		   toSelectorType: (FSSelectorType) selectorType;

-(void) setXDisplayElement: (FSDisplayElement) newElement;
-(void) setDisplayElement: (FSDisplayElement) newElement forAxis: (FSAxisType) axisType;

-(void) setLayoutType: (FSLayoutType) newLayoutType;

-(void) setCursors: (NSCursor *) cursor;

-(IBAction) toolSelectorClicked: (id) sender;
-(IBAction) layoutSelectorClicked: (id) sender;
-(IBAction) cropSelectorClicked: (id) sender;

-(IBAction) leftAxisTypeChanged: (id) sender;
-(IBAction) rightAxisTypeChanged: (id) sender;
-(IBAction) domainAxisTypeChanged: (id) sender;

-(IBAction) unitsChanged: (id) sender;

-(IBAction) print: (id) sender;
-(IBAction) exportToCSV: (id) sender;
-(IBAction) exportToNMEA: (id) sender;
-(IBAction) exportToKML: (id) sender;

-(BOOL) plotController: (PlotController *) controller
	shouldHandlePointingDeviceDownEvent: (NSEvent *) event atPoint: (CGPoint) point;
-(BOOL) plotController: (PlotController *) controller 
	shouldHandlePointingDeviceDraggedEvent: (NSEvent *) event atPoint: (CGPoint) point;
-(BOOL) plotController: (PlotController *) controller
	shouldHandlePointingDeviceUpEvent: (NSEvent *) event atPoint: (CGPoint) point;

-(void) updateToolTipForEvent: (NSEvent *) event controller: (PlotController *) controller
	isDistance: (BOOL) isDistance;

@property(nonatomic, readwrite, assign) FSToolType toolType;
@property(nonatomic, readwrite, assign) FSLayoutType layoutType;
@property(nonatomic, readwrite, assign) FSUnitType unitType;

@end
