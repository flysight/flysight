#import <Cocoa/Cocoa.h>

@class PlotAxisParameters;

typedef enum
{
	FSDisplayElementNone,
	FSDisplayElementElevation,
	FSDisplayElementVHorizontal,
	FSDisplayElementVDown,
	FSDisplayElementVTotal,
	FSDisplayElementGlideRatio,
	FSDisplayElementDistance,
	FSDisplayElementTime,
	FSDisplayElementCount
} FSDisplayElement;

typedef enum
{
	FSUnitTypeMetric,
	FSUnitTypeImperial
} FSUnitType;

@interface FSDataManager : NSObject
{
	NSArray *dataArray;
	NSMutableArray *displayDataArray;
	NSArrayController *arrayController;
}

+(PlotAxisParameters *) parametersForDisplayElement: (FSDisplayElement) displayElement;

-(id) initWithCSVString: (NSString *) initString;
-(id) initWithNMEAString: (NSString *) initString;

-(void) setDisplayUnitType: (FSUnitType) newUnitType;
-(NSUInteger) count;

-(void) exportToCSV: (NSMutableString *) string range: (NSRange) indices;
-(void) exportToNMEA: (NSMutableString *) string range: (NSRange) indices;
-(void) exportToKML: (NSMutableString *) string range: (NSRange) indices;

@property (nonatomic, readonly) NSArrayController *arrayController;

@end
