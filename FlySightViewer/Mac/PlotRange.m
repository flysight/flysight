#import "PlotRange.h"

@implementation PlotRange

@synthesize location;
@synthesize length;

+(PlotRange *) plotRangeWithLocation: (double) location length: (double) length
{
	PlotRange *value = [[PlotRange alloc] init];
	value.location = location;
	value.length = length;
	return [value autorelease];
}

-(double) maxValue
{
	return location + length;
}

-(double) minValue
{
	return location;
}

@end
