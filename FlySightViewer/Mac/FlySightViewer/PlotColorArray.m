#import "PlotColorArray.h"
#import <CorePlot/CPColor.h>

@implementation PlotColorArray

+(PlotColorArray *) arrayFromBaseValues: (CGFloat *) baseValues
{
	NSMutableArray *newArray = [NSMutableArray arrayWithCapacity: 3];
	
	[newArray addObject: [CPColor colorWithComponentRed: baseValues[0] 
												  green: baseValues[1]
												   blue: baseValues[2]
												  alpha: 1.0]];
	
	[newArray addObject: [CPColor colorWithComponentRed: 0.5 * (1.0 + baseValues[0])
												  green: 0.5 * (1.0 + baseValues[1])
												   blue: 0.5 * (1.0 + baseValues[2])
												  alpha: 1.0]];
	
	[newArray addObject: [CPColor colorWithComponentRed: 0.5 * (0.0 + baseValues[0])
												  green: 0.5 * (0.0 + baseValues[1])
												   blue: 0.5 * (0.0 + baseValues[2])
												  alpha: 1.0]];
	
	return (PlotColorArray *)newArray;
}

+(PlotColorArray *) blueColorArray
{
	static PlotColorArray *colorArray = nil;
	if (colorArray == nil)
	{
		CGFloat baseValues[3] = { (CGFloat)74/256, (CGFloat)126/256, (CGFloat)187/256 };
		colorArray = [[self arrayFromBaseValues: baseValues] retain];
	}
	return colorArray;
}

+(PlotColorArray *) redColorArray
{
	static PlotColorArray *colorArray = nil;
	if (colorArray == nil)
	{
		CGFloat baseValues[3] = { (CGFloat)190/256, (CGFloat)75/256, (CGFloat)72/256 };
		colorArray = [[self arrayFromBaseValues: baseValues] retain];
	}
	return colorArray;
}

+(PlotColorArray *) greenColorArray
{
	static PlotColorArray *colorArray = nil;
	if (colorArray == nil)
	{
		CGFloat baseValues[3] = { (CGFloat)152/256, (CGFloat)185/256, (CGFloat)84/256 };
		colorArray = [[self arrayFromBaseValues: baseValues] retain];
	}
	return colorArray;
}

@end
