#import <Cocoa/Cocoa.h>

@interface FSUnit : NSObject
{
	NSString *name;
	double factor;     // Multiplier to turn fundamental units into this unit
}

@property(nonatomic, readonly) NSString *name;
@property(nonatomic, readonly) double factor;

+(FSUnit *) unitWithName: (NSString *) initName factor: (double) initFactor;

-(id) initWithName: (NSString *) initName factor: (double) initFactor;

-(NSNumber *) applyToNumber: (NSNumber *) number;

@end
