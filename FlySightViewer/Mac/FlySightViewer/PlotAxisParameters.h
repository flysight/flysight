#import <Cocoa/Cocoa.h>
#import "PlotColorArray.h"

@interface PlotAxisParameters : NSObject
{
	PlotColorArray *colorArray;
	NSString *axisName;
	NSString *unitName;
	NSArray *dataNames;
	BOOL includeZero;
}

-(NSUInteger) count;

-(NSString *) axisLabel;

@property(nonatomic, readwrite, copy) PlotColorArray *colorArray;
@property(nonatomic, readwrite, copy) NSString *axisName;
@property(nonatomic, readwrite, copy) NSString *unitName;
@property(nonatomic, readwrite, copy) NSArray *dataNames;
@property(nonatomic, readwrite, assign) BOOL includeZero;

@end
