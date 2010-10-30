#import <Cocoa/Cocoa.h>

@interface PlotColorArray : NSArray
{
}

+(PlotColorArray *) arrayFromBaseValues: (CGFloat *) baseValues;

+(PlotColorArray *) blueColorArray;
+(PlotColorArray *) redColorArray;
+(PlotColorArray *) greenColorArray;

@end
