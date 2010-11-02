#import <Cocoa/Cocoa.h>

@interface PlotRange : NSObject
{
	double location, length;
}

+(PlotRange *) plotRangeWithLocation: (double) location length: (double) length;

@property(nonatomic, readwrite, assign) double location;
@property(nonatomic, readwrite, assign) double length;

-(double) maxValue;
-(double) minValue;

@end
