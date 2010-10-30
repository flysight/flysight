#import "PlotAxisParameters.h"

@implementation PlotAxisParameters

@synthesize colorArray;
@synthesize axisName;
@synthesize unitName;
@synthesize dataNames;
@synthesize includeZero;

-(id) init
{
	self = [super init];
	if (self != nil)
	{
		colorArray = nil;
		axisName = nil;
		unitName = nil;
		dataNames = nil;
		includeZero = NO;
	}
	
	return self;
}

-(void) dealloc
{
	[colorArray release];
	[axisName release];
	[unitName release];
	[dataNames release];
	
	[super dealloc];
}

-(NSUInteger) count
{
	return [dataNames count];
}

-(NSString *) axisLabel
{
	NSString *label;
	if ([self.unitName compare: @""] != NSOrderedSame)
		label = [NSString stringWithFormat: @"%@ (%@)", self.axisName, self.unitName];
	else label = [NSString stringWithFormat: @"%@", self.axisName];

	return label;
}

@end
