#import "FSUnit.h"

@implementation FSUnit

@synthesize name;
@synthesize factor;

+(FSUnit *) unitWithName: (NSString *) initName factor: (double) initFactor
{
	FSUnit *returnValue = [[FSUnit alloc] initWithName: initName factor: initFactor];
	return [returnValue autorelease];
}

-(id) initWithName: (NSString *) initName factor: (double) initFactor
{
	self = [super init];
	if (self != nil)
	{
		name = [initName copy];
		factor = initFactor;
	}
	
	return self;
}

-(void) dealloc
{
	[name release];
	[super dealloc];
}

-(NSNumber *) applyToNumber: (NSNumber *) number
{
	double originalValue = [number doubleValue];
	return [NSNumber numberWithDouble: originalValue * factor];
}

@end
