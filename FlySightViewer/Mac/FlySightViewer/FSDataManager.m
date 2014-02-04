#import <math.h>

#import "FSDataManager.h"
#import "PlotAxisParameters.h"
#import "FSUnit.h"

NSString *FSDataNameElevation = @"elevation";
NSString *FSDataNameVHorizontal = @"vHorizontal";
NSString *FSDataNameVDown = @"vDown";
NSString *FSDataNameVTotal = @"vTotal";
NSString *FSDataNameGlideRatio = @"glideRatio";
NSString *FSDataNameDistance = @"distance";
NSString *FSDataNameTime = @"time";

NSString *FSDataNameLatitude = @"latitude";
NSString *FSDataNameLongitude = @"longitude";
NSString *FSDataNameVNorth = @"vNorth";
NSString *FSDataNameVEast = @"vEast";

static const double distance_epsilon = 1.0e-10;

static const double knots_to_base = 0.514444444;

PlotAxisParameters *axisParameters[FSDisplayElementCount];
NSDictionary *units;

NSString *getChecksum(NSString *string)
{
	int sum = 0;
	for (int i = 1; i < [string length]; ++i) sum ^= [string characterAtIndex: i];
	return [NSString stringWithFormat: @"%02X", sum];
}

void getVincentyDistance(double aLatitude, double aLongitude, double bLatitude, double bLongitude,
						 double *distance, double *bHeading)
{

// Check whether the points are coincident
	
	if ((fabs(aLatitude - bLatitude) < distance_epsilon) && (fabs(aLongitude - bLongitude) < distance_epsilon))
	{
		*distance = 0.0;
		*bHeading = 0.0;
		return;
	}
		
// Calculate the great circle distance between two points on an ellipsoid.  Adapted from
// MATLAB code by Michael Kleder
	
// Parameters for WGS84 ellipsoid
	
	double ellipsoidA = 6378137;
	double ellipsoidB = 6356752.31424518;
	double ellipsoidF = (ellipsoidA - ellipsoidB) / ellipsoidA;

// Convert inputs in degrees to radians:

	aLatitude *= M_PI / 180.0;
	aLongitude *= M_PI / 180.0;
	bLatitude *= M_PI / 180.0;
	bLongitude *= M_PI / 180.0;

// Correct for errors at exact poles by adjusting 0.6 millimeters:

	if (fabs(M_PI_2 - fabs(aLatitude)) < distance_epsilon) 
		aLatitude = (aLatitude < 0 ? -1.0 : 1.0) * (M_PI_2 - distance_epsilon);
	if (fabs(M_PI_2 - fabs(bLatitude)) < distance_epsilon) 
		bLatitude = (bLatitude < 0 ? -1.0 : 1.0) * (M_PI_2 - distance_epsilon);

// Go!
	
	double u1 = atan((1 - ellipsoidF) * tan(aLatitude));
	double u2 = atan((1 - ellipsoidF) * tan(bLatitude));
	aLongitude = fmod(aLongitude, 2.0 * M_PI);
	bLongitude = fmod(bLongitude, 2.0 * M_PI);
	double l = fabs(bLongitude - aLongitude);
	if (l > M_PI) l = 2.0 * M_PI - l;
	double lambda = l;
	double lambdaold = 0;
	int itercount = 0;
	double alpha, sigma, cos2sigma_m;
	while ((itercount == 0) || (fabs(lambda-lambdaold) > 1e-12))
	{
		if (++itercount > 50)
		{
			NSLog(@"WARNING: Points are essentially antipodal. Precision may be reduced slightly.");
			lambda = M_PI;
			break;
		}
		
		lambdaold = lambda;
		double sinSigma = sqrt(pow(cos(u2) * sin(lambda), 2.0) + 
			pow(cos(u1) * sin(u2) - sin(u1) * cos(u2) * cos(lambda), 2.0));
		double cosSigma = sin(u1) * sin(u2) + cos(u1) * cos(u2) * cos(lambda);
		sigma = atan2(sinSigma, cosSigma);
		alpha = asin(cos(u1) * cos(u2) * sin(lambda) / sin(sigma));
		cos2sigma_m = cos(sigma) - 2.0 * sin(u1) * sin(u2) / pow(cos(alpha), 2.0);
		double c = ellipsoidF / 16.0 * pow(cos(alpha), 2.0) * (4.0 + ellipsoidF * (4.0 - 3.0 * pow(cos(alpha), 2.0)));
		lambda = l + (1.0 - c) * ellipsoidF * sin(alpha) * (sigma + c * sin(sigma) * 
			(cos2sigma_m + c * cos(sigma) * (-1.0 + 2.0 * pow(cos2sigma_m, 2.0))));
		
		if (lambda > M_PI)
		{
			NSLog(@"WARNING: Points are essentially antipodal. Precision may be reduced slightly.");
			lambda = M_PI;
			break;
		}
	}
	double w2 = pow(cos(alpha) / ellipsoidB, 2.0) * (ellipsoidA * ellipsoidA - ellipsoidB * ellipsoidB);
	double a = 1 + w2 / 16384 * (4096 + w2 * (-768 + w2 * (320 - 175 * w2)));
	double b = w2 / 1024 * (256 + w2 * (-128 + w2 * (74 - 47 * w2)));
	double deltasigma = b * sin(sigma) * (cos2sigma_m + b / 4.0 * (cos(sigma) * (-1.0 + 2.0 * pow(cos2sigma_m, 2.0)) -
		b / 6.0 * cos2sigma_m * (-3.0 + 4.0 * pow(sin(sigma), 2.0)) * (-3.0 + 4.0 * pow(cos2sigma_m, 2.0))));

	*distance = ellipsoidB * a * (sigma - deltasigma);
	*bHeading = atan2(-cos(u1) * sin(lambda), -sin(u1) * cos(u2) + cos(u1) * sin(u2) * cos(lambda));
}

void processSentence(NSDictionary *sentenceDictionary, NSMutableDictionary *values)
{
	NSArray *readValues = [NSArray arrayWithObjects: @"time", @"knotsGroundSpeed", @"latitude",
						   @"longitude", @"trueHeading", @"altitude", nil];
	
	NSNumber *time = [sentenceDictionary valueForKey: @"time"];
	if (time == nil) return;
	
	NSMutableDictionary *thisEntry = [values objectForKey: time];
	if (thisEntry == nil)
	{
		thisEntry = [[NSMutableDictionary alloc] init];
		[values setObject: [thisEntry autorelease] forKey: time];
	}
	
	for (NSString *key in readValues)
	{
		NSNumber *value = [thisEntry valueForKey: key];
		if (value == nil)
		{
			value = [sentenceDictionary valueForKey: key];
			if (value != nil) [thisEntry setObject: value forKey: key];
		}
	}
}

BOOL parseNMEA_doubleWithSuffix(NSEnumerator *enumerator, NSMutableDictionary *dictionary, NSString *key, 
								NSString *expectedSuffix)
{
	NSString *valueString = [enumerator nextObject];	
	NSString *suffixString = [enumerator nextObject];
	
	if ([valueString length] == 0) return NO;
	if ([suffixString length] == 0) return NO;
	
	double value = [valueString doubleValue];
	if ([suffixString compare: expectedSuffix] != NSOrderedSame)
	{
		NSLog(@"WARNING: Unexpected suffix '%@' on %@ in NMEA sentence", suffixString, key);
	}
	[dictionary setObject: [NSNumber numberWithDouble: value] forKey: key];
	
	return YES;
}

BOOL parseNMEA_double(NSEnumerator *enumerator, NSMutableDictionary *dictionary, NSString *key)
{
	NSString *nextString = [enumerator nextObject];
	if ([nextString length] == 0) return NO;
	
	double value = [nextString doubleValue];
	[dictionary setObject: [NSNumber numberWithDouble: value] forKey: key];
	return YES;
}

BOOL parseNMEA_int(NSEnumerator *enumerator, NSMutableDictionary *dictionary, NSString *key)
{
	NSString *nextString = [enumerator nextObject];
	if ([nextString length] == 0) return NO;
	
	int value = [nextString intValue];
	[dictionary setObject: [NSNumber numberWithInt: value] forKey: key];
	return YES;
}

BOOL parseNMEA_time(NSString *string, NSMutableDictionary *dictionary)
{
	if ([string length] == 0) return NO;
	
	int timeHours = [[string substringWithRange: NSMakeRange(0, 2)] intValue];
	int timeMinutes = [[string substringWithRange: NSMakeRange(2, 2)] intValue];
	double timeSeconds = [[string substringFromIndex: 4] doubleValue];
	double time = 3600 * timeHours + 60 * timeMinutes + timeSeconds;
	[dictionary setObject: [NSNumber numberWithDouble: time] forKey: @"time"];
	
	return YES;
}

BOOL parseNMEA_date(NSString *string, NSMutableDictionary *dictionary)
{
	if ([string length] == 0) return NO;

	int dateDay = [[string substringWithRange: NSMakeRange(0, 2)] intValue];
	[dictionary setObject: [NSNumber numberWithInt: dateDay] forKey: @"day"];
	int dateMonth = [[string substringWithRange: NSMakeRange(2, 2)] intValue];
	[dictionary setObject: [NSNumber numberWithInt: dateMonth] forKey: @"month"];
	int dateYear = [[string substringWithRange: NSMakeRange(4, 2)] intValue];
	[dictionary setObject: [NSNumber numberWithInt: dateYear] forKey: @"year"];
	
	return YES;
}

BOOL parseNMEA_latitude(NSString *latitudeString, NSString *hemisphereString, NSMutableDictionary *dictionary)
{
	if ([latitudeString length] == 0) return NO;
	if ([hemisphereString length] == 0) return NO;

	int decimalLocation;
	NSRange decimalRange = [latitudeString rangeOfString: @"."];
	if (decimalRange.location == NSNotFound) decimalLocation = [latitudeString length];
	else decimalLocation = decimalRange.location;
	
	int latDegrees = [[latitudeString substringWithRange: NSMakeRange(0, decimalLocation - 2)] intValue];
	double latMinutes = [[latitudeString substringFromIndex: decimalLocation - 2] doubleValue];
	double latitude = latDegrees + latMinutes / 60.0;
	if ([hemisphereString compare: @"S"] == NSOrderedSame) latitude = -latitude;
	[dictionary setObject: [NSNumber numberWithDouble: latitude] forKey: @"latitude"];
	
	return YES;
}

BOOL parseNMEA_longitude(NSString *longitudeString, NSString *hemisphereString, NSMutableDictionary *dictionary)
{
	if ([longitudeString length] == 0) return NO;
	if ([hemisphereString length] == 0) return NO;
	
	int decimalLocation;
	NSRange decimalRange = [longitudeString rangeOfString: @"."];
	if (decimalRange.location == NSNotFound) decimalLocation = [longitudeString length];
	else decimalLocation = decimalRange.location;
	
	int longDegrees = [[longitudeString substringWithRange: NSMakeRange(0, decimalLocation - 2)] intValue];
	double longMinutes = [[longitudeString substringFromIndex: decimalLocation - 2] doubleValue];
	double longitude = longDegrees + longMinutes / 60.0;
	if ([hemisphereString compare: @"W"] == NSOrderedSame) longitude = -longitude;
	[dictionary setObject: [NSNumber numberWithDouble: longitude] forKey: @"longitude"];
	
	return YES;
}

NSDictionary *parseGPRMC(NSArray *components)
{
	if (([components count] != 12) && ([components count] != 13))
	{
		NSLog(@"ERROR: Unexpected number of components %lu in GPRMC sentence", [components count]);
		return nil;
	}
	
	NSMutableDictionary *returnValues = [[NSMutableDictionary alloc] init];
	[returnValues setObject: @"GPRMC" forKey: @"sentenceType"];
	
// Parse recommended minimum specific GPS/TRANSIT data
	
	NSEnumerator *componentEnumerator = [components objectEnumerator];
	[componentEnumerator nextObject];

	NSString *timeString = [componentEnumerator nextObject];
	NSString *statusString = [componentEnumerator nextObject];
	if (parseNMEA_time(timeString, returnValues))
	{	
		if ([statusString compare: @"A"] != NSOrderedSame)
		{
			NSLog(@"WARNING: Unexpected status '%@' in GPRMC sentence", statusString);
		}
	}
		
	NSString *latitudeString = [componentEnumerator nextObject];
	NSString *latHemisphereString = [componentEnumerator nextObject];
	parseNMEA_latitude(latitudeString, latHemisphereString, returnValues);
	
	NSString *longitudeString = [componentEnumerator nextObject];
	NSString *longHemisphereString = [componentEnumerator nextObject];
	parseNMEA_longitude(longitudeString, longHemisphereString, returnValues);

	parseNMEA_double(componentEnumerator, returnValues, @"knotsGroundSpeed");
	parseNMEA_double(componentEnumerator, returnValues, @"trueHeading");

	NSString *dateString = [componentEnumerator nextObject];
	parseNMEA_date(dateString, returnValues);

	NSString *variationString = [componentEnumerator nextObject];
	NSString *variationDirection = [componentEnumerator nextObject];
	if (([variationString length] > 0) && ([variationDirection length] > 0))
	{
		double magneticVariation = [variationString doubleValue];
		if ([variationDirection compare: @"W"] == NSOrderedSame) magneticVariation = -magneticVariation;
		[returnValues setObject: [NSNumber numberWithDouble: magneticVariation] forKey: @"magneticVariation"];
	}

	return [returnValues autorelease];
}

NSDictionary *parseGPVTG(NSArray *components)
{
	if (([components count] != 9) && ([components count] != 10))
	{
		NSLog(@"ERROR: Unexpected number of components %lu in GPVTG sentence", [components count]);
		return nil;
	}
	
	NSMutableDictionary *returnValues = [[NSMutableDictionary alloc] init];
	[returnValues setObject: @"GPVTG" forKey: @"sentenceType"];
	
// Parse track made good and ground speed
	
	NSEnumerator *componentEnumerator = [components objectEnumerator];
	[componentEnumerator nextObject];

	parseNMEA_doubleWithSuffix(componentEnumerator, returnValues, @"trueHeading", @"T");
	parseNMEA_doubleWithSuffix(componentEnumerator, returnValues, @"magneticHeading", @"M");
	parseNMEA_doubleWithSuffix(componentEnumerator, returnValues, @"knotsGroundSpeed", @"N");
	parseNMEA_doubleWithSuffix(componentEnumerator, returnValues, @"kmhGroundSpeed", @"K");
		
	return [returnValues autorelease];
}

NSDictionary *parseGPGLL(NSArray *components)
{
	NSMutableDictionary *returnValues = [[NSMutableDictionary alloc] init];
	[returnValues setObject: @"GPGLL" forKey: @"sentenceType"];
	
// Parse geographic position, latitude and longitude
	
	NSEnumerator *componentEnumerator = [components objectEnumerator];
	[componentEnumerator nextObject];
	
	NSString *latitudeString = [componentEnumerator nextObject];
	NSString *latHemisphereString = [componentEnumerator nextObject];
	parseNMEA_latitude(latitudeString, latHemisphereString, returnValues);
	
	NSString *longitudeString = [componentEnumerator nextObject];
	NSString *longHemisphereString = [componentEnumerator nextObject];
	parseNMEA_longitude(longitudeString, longHemisphereString, returnValues);
	
	NSString *timeString = [componentEnumerator nextObject];
	NSString *statusString = [componentEnumerator nextObject];
	if (parseNMEA_time(timeString, returnValues))
	{
		if ([statusString compare: @"A"] != NSOrderedSame)
		{
			NSLog(@"WARNING: Unexpected status '%@' in GPGLL sentence", statusString);
		}
	}
	
	return [returnValues autorelease];
}

NSDictionary *parseGPGSV(NSArray *components)
{
	if (([components count] < 8) || ([components count] % 4 != 0))
	{
		NSLog(@"ERROR: Unexpected number of components %lu in GPGSV sentence", [components count]);
		return nil;
	}
	
	NSMutableDictionary *returnValues = [[NSMutableDictionary alloc] init];
	[returnValues setObject: @"GPGSV" forKey: @"sentenceType"];
	
// Parse GPS satellites in view
	
	NSEnumerator *componentEnumerator = [components objectEnumerator];
	[componentEnumerator nextObject];

	parseNMEA_int(componentEnumerator, returnValues, @"numGSV");
	parseNMEA_int(componentEnumerator, returnValues, @"index");
	parseNMEA_int(componentEnumerator, returnValues, @"numSatellites");

	NSMutableDictionary *satelliteData = [[NSMutableDictionary alloc] init];
	NSString *prnString;
	do
	{
		NSMutableDictionary *thisData = [[[NSMutableDictionary alloc] init] autorelease];

		prnString = [componentEnumerator nextObject];
		parseNMEA_int(componentEnumerator, thisData, @"elevation");
		parseNMEA_int(componentEnumerator, thisData, @"azimuth");
		parseNMEA_int(componentEnumerator, thisData, @"SNR");
		if ([prnString length] != 0) 
		{
			NSNumber *satellitePRN = [NSNumber numberWithInt: [prnString intValue]];
			[satelliteData setObject: thisData forKey: satellitePRN];
		}
	} while ([prnString length] != 0);
	[returnValues setObject: [satelliteData autorelease] forKey: @"satelliteData"];

	return [returnValues autorelease];
}

NSDictionary *parseGPGSA(NSArray *components)
{
	if ([components count] != 18)
	{
		NSLog(@"ERROR: Unexpected number of components %lu in GPGSA sentence", [components count]);
		return nil;
	}

	NSMutableDictionary *returnValues = [[NSMutableDictionary alloc] init];
	[returnValues setObject: @"GPGSA" forKey: @"sentenceType"];

// Parse GPS dilution of precision and active satellites
	
	NSEnumerator *componentEnumerator = [components objectEnumerator];
	[componentEnumerator nextObject];
	
	NSString *modeString = [componentEnumerator nextObject];
	if ([modeString length] != 0) [returnValues setObject: modeString forKey: @"mode"];
	parseNMEA_int(componentEnumerator, returnValues, @"dimensions");

	NSMutableArray *prnArray = [[[NSMutableArray alloc] init] autorelease];
	for (int offset = 3; offset <= 14; ++offset)
	{
		NSString *prnString = [componentEnumerator nextObject];
		if ([prnString length] != 0) 
		{
			[prnArray addObject: [NSNumber numberWithInt: [prnString intValue]]];
		}
	}
	[returnValues setObject: prnArray forKey: @"prnArray"];

	parseNMEA_double(componentEnumerator, returnValues, @"pDOP");
	parseNMEA_double(componentEnumerator, returnValues, @"hDOP");
	parseNMEA_double(componentEnumerator, returnValues, @"vDOP");

	return [returnValues autorelease];
}

NSDictionary *parseGPGGA(NSArray *components)
{
	if ([components count] != 15)
	{
		NSLog(@"ERROR: Unexpected number of components %lu in GPGGA sentence", [components count]);
		return nil;
	}
	
	NSMutableDictionary *returnValues = [[NSMutableDictionary alloc] init];
	[returnValues setObject: @"GPGGA" forKey: @"sentenceType"];
	
// Parse global positioning system fix data
	
	NSEnumerator *componentEnumerator = [components objectEnumerator];
	[componentEnumerator nextObject];
	
	NSString *timeString = [componentEnumerator nextObject];
	parseNMEA_time(timeString, returnValues);
	
	NSString *latitudeString = [componentEnumerator nextObject];
	NSString *latHemisphereString = [componentEnumerator nextObject];
	parseNMEA_latitude(latitudeString, latHemisphereString, returnValues);
	
	NSString *longitudeString = [componentEnumerator nextObject];
	NSString *longHemisphereString = [componentEnumerator nextObject];
	parseNMEA_longitude(longitudeString, longHemisphereString, returnValues);

	parseNMEA_int(componentEnumerator, returnValues, @"fixQuality");
	parseNMEA_int(componentEnumerator, returnValues, @"numSatellites");
	parseNMEA_double(componentEnumerator, returnValues, @"hDOP");

	parseNMEA_doubleWithSuffix(componentEnumerator, returnValues, @"altitude", @"M");
	parseNMEA_doubleWithSuffix(componentEnumerator, returnValues, @"geoidHeight", @"M");

	parseNMEA_double(componentEnumerator, returnValues, @"timeSinceLastDGPS");
	parseNMEA_int(componentEnumerator, returnValues, @"stationID");

	return [returnValues autorelease];
}

@implementation FSDataManager

@synthesize arrayController;

+(void) initialize
{
	static const double units_ft = 3.2808399;   // 1 m
	static const double units_km_h = 3.6;       // 1 m/s
	static const double units_mph = 2.23693629; // 1 m/s
	
    if (self == [FSDataManager class])
	{
		
// Build units
		
		NSArray *distanceUnits = [NSArray arrayWithObjects: [FSUnit unitWithName: @"m" factor: 1.0],
															[FSUnit unitWithName: @"ft" factor: units_ft], 
															nil];
		
		NSArray *speedUnits = [NSArray arrayWithObjects: [FSUnit unitWithName: @"km/h" factor: units_km_h],
														 [FSUnit unitWithName: @"mph" factor: units_mph], 
														 nil];
		
		NSArray *timeUnits = [NSArray arrayWithObjects: [FSUnit unitWithName: @"s" factor: 1.0],
														[FSUnit unitWithName: @"s" factor: 1.0], 
														nil];
		
		NSArray *emptyUnits = [NSArray arrayWithObjects: [FSUnit unitWithName: @"" factor: 1.0],
														 [FSUnit unitWithName: @"" factor: 1.0], 
														 nil];
		
		NSMutableDictionary *mutableUnits = [[NSMutableDictionary alloc] init];
		
		[mutableUnits setObject: distanceUnits forKey: FSDataNameElevation];
		[mutableUnits setObject: speedUnits forKey: FSDataNameVHorizontal];
		[mutableUnits setObject: speedUnits forKey: FSDataNameVDown];
		[mutableUnits setObject: speedUnits forKey: FSDataNameVTotal];
		[mutableUnits setObject: emptyUnits forKey: FSDataNameGlideRatio];
		[mutableUnits setObject: distanceUnits forKey: FSDataNameDistance];
		[mutableUnits setObject: timeUnits forKey: FSDataNameTime];		

		units = mutableUnits;

// Build axis parameters
	
		axisParameters[FSDisplayElementNone] = nil;

		PlotAxisParameters *elevationParameters = [[PlotAxisParameters alloc] init];
		elevationParameters.axisName = @"Elevation";
		elevationParameters.unitName = nil;
		elevationParameters.dataNames = [NSArray arrayWithObject: FSDataNameElevation];
		elevationParameters.includeZero = NO;
		axisParameters[FSDisplayElementElevation] = elevationParameters;
		
		PlotAxisParameters *vHorizontalParameters = [[PlotAxisParameters alloc] init];
		vHorizontalParameters.axisName = @"Horizontal speed";
		vHorizontalParameters.unitName = nil;
		vHorizontalParameters.dataNames = [NSArray arrayWithObject: FSDataNameVHorizontal];
		vHorizontalParameters.includeZero = YES;
		axisParameters[FSDisplayElementVHorizontal] = vHorizontalParameters;
		
		PlotAxisParameters *vDownParameters = [[PlotAxisParameters alloc] init];
		vDownParameters.axisName = @"Vertical speed";
		vDownParameters.unitName = nil;
		vDownParameters.dataNames = [NSArray arrayWithObject: FSDataNameVDown];
		vDownParameters.includeZero = YES;
		axisParameters[FSDisplayElementVDown] = vDownParameters;
		
		PlotAxisParameters *vTotalParameters = [[PlotAxisParameters alloc] init];
		vTotalParameters.axisName = @"Total speed";
		vTotalParameters.unitName = nil;
		vTotalParameters.dataNames = [NSArray arrayWithObject: FSDataNameVTotal];
		vTotalParameters.includeZero = YES;
		axisParameters[FSDisplayElementVTotal] = vTotalParameters;
		
		PlotAxisParameters *glideRatioParameters = [[PlotAxisParameters alloc] init];
		glideRatioParameters.axisName = @"Glide ratio";
		glideRatioParameters.unitName = nil;
		glideRatioParameters.dataNames = [NSArray arrayWithObject: FSDataNameGlideRatio];
		glideRatioParameters.includeZero = NO;
		axisParameters[FSDisplayElementGlideRatio] = glideRatioParameters;
		
		PlotAxisParameters *distanceParameters = [[PlotAxisParameters alloc] init];
		distanceParameters.axisName = @"Horizontal distance";
		distanceParameters.unitName = nil;
		distanceParameters.dataNames = [NSArray arrayWithObject: FSDataNameDistance];
		distanceParameters.includeZero = NO;
		axisParameters[FSDisplayElementDistance] = distanceParameters;
		
		PlotAxisParameters *timeParameters = [[PlotAxisParameters alloc] init];
		timeParameters.axisName = @"Time";
		timeParameters.unitName = nil;
		timeParameters.dataNames = [NSArray arrayWithObject: FSDataNameTime];
		timeParameters.includeZero = NO;				
		axisParameters[FSDisplayElementTime] = timeParameters;
	}
}

+(PlotAxisParameters *) parametersForDisplayElement: (FSDisplayElement) displayElement
{
	if (displayElement < FSDisplayElementCount) return axisParameters[displayElement];
	else return nil;
}

-(id) init
{
	self = [super init];
	if (self != nil)
	{
		dataArray = nil;
		displayDataArray = [[NSMutableArray alloc] init];
		arrayController = [[NSArrayController alloc] init];
		arrayController.content = displayDataArray;
	}
	
	return self;
}

-(id) initWithCSVString: (NSString *) initString
{
	self = [self init];
	if (self != nil)
	{		
		NSScanner *scanner = [NSScanner scannerWithString: initString];
		NSCharacterSet *controlSet = [NSCharacterSet controlCharacterSet];
		NSString *line;

// Skip header

		[scanner scanUpToCharactersFromSet: controlSet intoString: &line];
		[scanner scanUpToCharactersFromSet: controlSet intoString: &line];

// Read dataArray lines

		BOOL firstLine = YES;
		int expectedComponentsCount;
		double dHorizontal = 0.0;
		double lastTime = 0.0;
		double firstTime;

		NSMutableArray *initArray = [[NSMutableArray alloc] init];
		NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
		dateFormatter.dateFormat = @"yyyy-MM-dd'T'HH:mm:ss.SS'Z'";
		dateFormatter.timeZone = [NSTimeZone timeZoneForSecondsFromGMT: 0];
		while ([scanner scanUpToCharactersFromSet: controlSet intoString: &line])
		{
			NSArray *components = [line componentsSeparatedByString: @","];

			if (firstLine) expectedComponentsCount = [components count];
			if (([components count] != expectedComponentsCount) || ([components count] < 12))
			{
				[initArray release];
				[self autorelease];
				return nil;
			}

			NSMutableDictionary *thisEntry = [[NSMutableDictionary alloc] init];

			NSDate *date = [dateFormatter dateFromString: [components objectAtIndex: 0]];
			double time = [date timeIntervalSinceReferenceDate];
			if (firstLine) firstTime = time;
			[thisEntry setValue: [NSNumber numberWithDouble: time - firstTime] forKey: FSDataNameTime];
			
			double latitude = [[components objectAtIndex: 1] doubleValue];
			[thisEntry setValue: [NSNumber numberWithDouble: latitude] forKey: FSDataNameLatitude];
			
			double longitude = [[components objectAtIndex: 2] doubleValue];
			[thisEntry setValue: [NSNumber numberWithDouble: longitude] forKey: FSDataNameLongitude];
			
			double elevation = [[components objectAtIndex: 3] doubleValue];
			[thisEntry setValue: [NSNumber numberWithDouble: elevation] forKey: FSDataNameElevation];

			double vNorth = [[components objectAtIndex: 4] doubleValue];
			[thisEntry setValue: [NSNumber numberWithDouble: vNorth] forKey: FSDataNameVNorth];
			
			double vEast = [[components objectAtIndex: 5] doubleValue];
			[thisEntry setValue: [NSNumber numberWithDouble: vEast] forKey: FSDataNameVEast];

			double vDown = [[components objectAtIndex: 6] doubleValue];
			[thisEntry setValue: [NSNumber numberWithDouble: vDown] forKey: FSDataNameVDown];
			
			double vHorizontal = sqrt(vNorth * vNorth + vEast * vEast);
			[thisEntry setValue: [NSNumber numberWithDouble: vHorizontal] forKey: FSDataNameVHorizontal];
			
			double vTotal = sqrt(vDown * vDown + vHorizontal * vHorizontal);
			[thisEntry setValue: [NSNumber numberWithDouble: vTotal] forKey: FSDataNameVTotal];
			
			double glideRatio;
			if (vDown != 0.0) glideRatio = vHorizontal / vDown;
			else glideRatio = 0.0;
			[thisEntry setValue: [NSNumber numberWithDouble: glideRatio] forKey: FSDataNameGlideRatio];
			
			if (!firstLine) dHorizontal += vHorizontal * (time - lastTime);
			[thisEntry setValue: [NSNumber numberWithDouble: dHorizontal] forKey: FSDataNameDistance];
			lastTime = time;
			
			[initArray addObject: [thisEntry autorelease]];
			firstLine = NO;
		}
		
		dataArray = initArray;	
	}
	
	return self;
}

-(id) initWithNMEAString: (NSString *) initString
{
	self = [self init];
	if (self != nil)
	{		
		NSScanner *scanner = [NSScanner scannerWithString: initString];
		NSCharacterSet *controlSet = [NSCharacterSet controlCharacterSet];
		NSString *line;
		
// Read NMEA data into a dictionary
		
		NSMutableDictionary *rawValues = [[NSMutableDictionary alloc] init];
		while ([scanner scanUpToCharactersFromSet: controlSet intoString: &line])
		{
			NSArray *parts = [line componentsSeparatedByString: @"*"];
			if ([parts count] != 2) continue;
			
			NSString *sentence = [parts objectAtIndex: 0];
			NSArray *components = [sentence componentsSeparatedByString: @","];
			NSString *sentenceCode = [components objectAtIndex: 0];

			NSString *trueValue = getChecksum(sentence);
			if ([trueValue compare: [parts objectAtIndex: 1]] != NSOrderedSame)
			{
				NSLog(@"WARNING: Bad checksum in NMEA data");
			}
			else
			{
				NSDictionary *sentenceDictionary = nil;
				if ([sentenceCode compare: @"$GPGGA"] == NSOrderedSame) sentenceDictionary = parseGPGGA(components);
				else if ([sentenceCode compare: @"$GPGSA"] == NSOrderedSame) sentenceDictionary = parseGPGSA(components);
				else if ([sentenceCode compare: @"$GPGSV"] == NSOrderedSame) sentenceDictionary = parseGPGSV(components);
				else if ([sentenceCode compare: @"$GPGLL"] == NSOrderedSame) sentenceDictionary = parseGPGLL(components);
				else if ([sentenceCode compare: @"$GPVTG"] == NSOrderedSame) sentenceDictionary = parseGPVTG(components);
				else if ([sentenceCode compare: @"$GPRMC"] == NSOrderedSame) sentenceDictionary = parseGPRMC(components);
				
				if (sentenceDictionary != nil) processSentence(sentenceDictionary, rawValues);
			}
		}
		
// Build an array of data sorted in increasing time order

		NSArray *sortedKeys = [[rawValues allKeys] sortedArrayUsingSelector: @selector(compare:)];

		NSMutableArray *initArray = [[NSMutableArray alloc] init];
		
		BOOL firstLine = YES;
		double dHorizontal = 0.0;
		double firstTime, lastTime, lastAltitude;
		double lastLatitude, lastLongitude;
		
		for (NSString *key in sortedKeys)
		{
			NSDictionary *rawEntry = [rawValues objectForKey: key];

			NSMutableDictionary *thisEntry = [[NSMutableDictionary alloc] init];
			BOOL good = YES;

			NSNumber *time = [rawEntry objectForKey: @"time"];
			double timeValue = [time doubleValue];
			if (firstLine) firstTime = timeValue;
			if (time != nil) [thisEntry setValue: [NSNumber numberWithDouble: timeValue - firstTime] forKey: FSDataNameTime];
			else good = NO;
			
			NSNumber *latitude = [rawEntry objectForKey: @"latitude"];
			if (latitude != nil) [thisEntry setValue: latitude forKey: FSDataNameLatitude];
			else good = NO;
			
			NSNumber *longitude = [rawEntry objectForKey: @"longitude"];
			if (longitude != nil) [thisEntry setValue: longitude forKey: FSDataNameLongitude];
			else good = NO;
			
			NSNumber *altitude = [rawEntry objectForKey: @"altitude"];
			if (altitude != nil) [thisEntry setValue: altitude forKey: FSDataNameElevation];
			else good = NO;

			if (good)
			{
				double altitudeValue = [altitude doubleValue];
				double latitudeValue = [latitude doubleValue];
				double longitudeValue = [longitude doubleValue];

				double vHorizontal, angle;
				NSNumber *knotsGroundSpeed = [rawEntry objectForKey: @"knotsGroundSpeed"];
				if (knotsGroundSpeed != nil) 
				{
					vHorizontal = [knotsGroundSpeed doubleValue] * knots_to_base;
					
					NSNumber *trueHeading = [rawEntry objectForKey: @"trueHeading"];
					angle = [trueHeading doubleValue] * M_PI / 180.0;
				}
				else if (!firstLine)
				{
					double hDistance;
					getVincentyDistance(lastLatitude, lastLongitude, latitudeValue, longitudeValue,
										&hDistance, &angle);
					vHorizontal = hDistance / (timeValue - lastTime);
				}
				
				if (!firstLine)
				{
					[thisEntry setValue: [NSNumber numberWithDouble: vHorizontal] forKey: FSDataNameVHorizontal];

					double vNorth = vHorizontal * cos(angle);
					double vEast = vHorizontal * sin(angle);
					[thisEntry setValue: [NSNumber numberWithDouble: vNorth] forKey: FSDataNameVNorth];
					[thisEntry setValue: [NSNumber numberWithDouble: vEast] forKey: FSDataNameVEast];
					
					double vDown = -(altitudeValue - lastAltitude) / (timeValue - lastTime);
					[thisEntry setValue: [NSNumber numberWithDouble: vDown] forKey: FSDataNameVDown];

					double vTotal = sqrt(vDown * vDown + vHorizontal * vHorizontal);
					[thisEntry setValue: [NSNumber numberWithDouble: vTotal] forKey: FSDataNameVTotal];

					double glideRatio;
					if (vDown != 0.0) glideRatio = vHorizontal / vDown;
					else glideRatio = 0.0;
					[thisEntry setValue: [NSNumber numberWithDouble: glideRatio] forKey: FSDataNameGlideRatio];
				}
								
				if (!firstLine) dHorizontal += vHorizontal * (timeValue - lastTime);
				[thisEntry setValue: [NSNumber numberWithDouble: dHorizontal] forKey: FSDataNameDistance];

				[initArray addObject: [thisEntry autorelease]];

				lastTime = timeValue;
				lastAltitude = altitudeValue;
				lastLatitude = latitudeValue;
				lastLongitude = longitudeValue;
				firstLine = NO;
			}
		}
		
		[rawValues release];

		if ([initArray count] == 0)
		{
			[initArray release];
			[self autorelease];
			return nil;
		}
		
		dataArray = initArray;		
	}
	
	return self;
}

-(void) dealloc
{
	[dataArray release];
	[arrayController release];
	
	[super dealloc];
}

-(void) setDisplayUnitType: (FSUnitType) newUnitType
{
	for (int i = 0; i < FSDisplayElementCount; ++i)
	{
		NSString *dataName = [axisParameters[i].dataNames objectAtIndex: 0];
		FSUnit *thisUnit = [[units objectForKey: dataName] objectAtIndex: newUnitType];
		axisParameters[i].unitName = thisUnit.name;
	}
	
	[displayDataArray removeAllObjects];
	
	NSMutableSet *includedKeys = [[NSMutableSet alloc] init];
	for (int i = 0; i < FSDisplayElementCount; ++i)
		[includedKeys addObjectsFromArray: axisParameters[i].dataNames];
	
	for (NSDictionary *arrayEntry in dataArray)
	{
		NSMutableDictionary *newEntry = [[NSMutableDictionary alloc] init];
		for (NSString *thisKey in includedKeys)
		{
			FSUnit *thisUnit = [[units objectForKey: thisKey] objectAtIndex: newUnitType];
			NSNumber *rawValue = [arrayEntry objectForKey: thisKey];
			[newEntry setObject: [thisUnit applyToNumber: rawValue] forKey: thisKey];
		}
		[displayDataArray addObject: [newEntry autorelease]];
	}
	
	[includedKeys release];
}

-(NSUInteger) count
{
	return [dataArray count];
}

-(void) exportToCSV: (NSMutableString *) string range: (NSRange) indices
{
	NSRange allRange = NSMakeRange(0, [dataArray count]);
	indices = NSIntersectionRange(allRange, indices);
	
	[string appendString: @"time,lat,lon,elev,vel_n,vel_e,vel_d\n"];
	for (int i = indices.location; i < indices.location + indices.length; ++i)
	{
		NSDictionary *entry = [dataArray objectAtIndex: i];
		[string appendFormat: @"%d,", (int)([[entry objectForKey: FSDataNameTime] doubleValue] * 1.0E3)];
		[string appendFormat: @"%d,", (int)([[entry objectForKey: FSDataNameLatitude] doubleValue] * 1.0E7)];
		[string appendFormat: @"%d,", (int)([[entry objectForKey: FSDataNameLongitude] doubleValue] * 1.0E7)];
		[string appendFormat: @"%d,", (int)([[entry objectForKey: FSDataNameElevation] doubleValue] * 1.0E3)];
		[string appendFormat: @"%d,", (int)([[entry objectForKey: FSDataNameVNorth] doubleValue] * 1.0E2)];
		[string appendFormat: @"%d,", (int)([[entry objectForKey: FSDataNameVEast] doubleValue] * 1.0E2)];
		[string appendFormat: @"%d\n", (int)([[entry objectForKey: FSDataNameVDown] doubleValue] * 1.0E2)];		
	}
}

-(void) exportToNMEA: (NSMutableString *) string range: (NSRange) indices
{
	NSRange allRange = NSMakeRange(0, [dataArray count]);
	indices = NSIntersectionRange(allRange, indices);
	
	for (int i = indices.location; i < indices.location + indices.length; ++i)
	{
		NSDictionary *entry = [dataArray objectAtIndex: i];
		
		double time = [[entry objectForKey: FSDataNameTime] doubleValue];
		int timeHours = (int)(time / 3600);
		int timeMinutes = (int)(time / 60) - 60 * timeHours;
		double timeSeconds = time - 3600 * timeHours - 60 * timeMinutes;
		NSString *timeString = [NSString stringWithFormat: @"%02d%02d%06.3lf", timeHours, timeMinutes, timeSeconds];

		
		double latitude = [[entry objectForKey: FSDataNameLatitude] doubleValue];
		NSString *latitudeHemisphere = (latitude < 0) ? @"S" : @"N";
		latitude = fabs(latitude);
		int latitudeDegrees = (int)(latitude);
		double latitudeMinutes = (latitude - latitudeDegrees) * 60;
		NSString *latitudeString = [NSString stringWithFormat: @"%02d%07.4lf,%@", latitudeDegrees, latitudeMinutes,
									latitudeHemisphere];

		double longitude = [[entry objectForKey: FSDataNameLongitude] doubleValue];
		NSString *longitudeHemisphere = (longitude < 0) ? @"W" : @"E";
		longitude = fabs(longitude);
		int longitudeDegrees = (int)(longitude);
		double longitudeMinutes = (longitude - longitudeDegrees) * 60;
		NSString *longitudeString = [NSString stringWithFormat: @"%03d%07.4lf,%@", longitudeDegrees, longitudeMinutes,
									 longitudeHemisphere];
		
		double knotsGroundSpeed = [[entry objectForKey: FSDataNameVHorizontal] doubleValue] / knots_to_base;
		double kmhGroundSpeed = [[entry objectForKey: FSDataNameVHorizontal] doubleValue] * 3.6;
		double vEast = [[entry objectForKey: FSDataNameVEast] doubleValue];
		double vNorth = [[entry objectForKey: FSDataNameVNorth] doubleValue];
		double trueHeading = atan2(vEast, vNorth) * 180 / M_PI;
		if (trueHeading < 0.0) trueHeading += 360.0;
		
		double elevation = [[entry objectForKey: FSDataNameElevation] doubleValue];

		NSString *gprmcString = [NSString stringWithFormat: @"$GPRMC,%@,A,%@,%@,%.2lf,%.2lf,,,,A", timeString, 
								 latitudeString, longitudeString, knotsGroundSpeed, trueHeading];
		NSString *gprmcChecksum = getChecksum(gprmcString);
	    [string appendFormat: @"%@*%@\n", gprmcString, gprmcChecksum];

		NSString *gpggaString = [NSString stringWithFormat: @"$GPGGA,%@,%@,%@,,,,%.1lf,M,,,,", timeString, 
								 latitudeString, longitudeString, elevation];
		NSString *gpggaChecksum = getChecksum(gpggaString);
	    [string appendFormat: @"%@*%@\n", gpggaString, gpggaChecksum];

		NSString *gpvtgString = [NSString stringWithFormat: @"$GPVTG,%.1lf,T,,,%.1lf,N,%.1lf,K", trueHeading, 
								 knotsGroundSpeed, kmhGroundSpeed];
		NSString *gpvtgChecksum = getChecksum(gpvtgString);
	    [string appendFormat: @"%@*%@\n", gpvtgString, gpvtgChecksum];
	}
}

-(void) exportToKML: (NSMutableString *) string range: (NSRange) indices
{
	NSRange allRange = NSMakeRange(0, [dataArray count]);
	indices = NSIntersectionRange(allRange, indices);
	
	[string appendString: @"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"];
	[string appendString: @"<kml xmlns=\"http://earth.google.com/kml/2.1\">\n"];
	[string appendString: @"<Document>\n"];
	[string appendString: @"\t<Style id=\"highlightStyle\">\n"];
	[string appendString: @"\t\t<LineStyle>\n"];
	[string appendString: @"\t\t\t<color>ff0000ff</color>\n"];
	[string appendString: @"\t\t\t<width>4</width>\n"];
	[string appendString: @"\t\t</LineStyle>\n"];
	[string appendString: @"\t</Style>\n"];
	[string appendString: @"\t<Style id=\"normalStyle\">\n"];
	[string appendString: @"\t\t<LineStyle>\n"];
	[string appendString: @"\t\t\t<color>ff0000ff</color>\n"];
	[string appendString: @"\t\t\t<width>4</width>\n"];
	[string appendString: @"\t\t</LineStyle>\n"];
	[string appendString: @"\t</Style>\n"];
	[string appendString: @"\t<StyleMap id=\"lineStyle\">\n"];
	[string appendString: @"\t\t<Pair>\n"];
	[string appendString: @"\t\t\t<key>normal</key>\n"];
	[string appendString: @"\t\t\t<styleUrl>#normalStyle</styleUrl>\n"];
	[string appendString: @"\t\t</Pair>\n"];
	[string appendString: @"\t\t<Pair>\n"];
	[string appendString: @"\t\t\t<key>highlight</key>\n"];
	[string appendString: @"\t\t\t<styleUrl>#highlightStyle</styleUrl>\n"];
	[string appendString: @"\t\t</Pair>\n"];
	[string appendString: @"\t</StyleMap>\n"];
	[string appendString: @"\t<name>Exported track</name>\n"];
	 
	[string appendString: @"\t<Placemark>\n"];
	[string appendString: @"\t\t<name>Exported track</name>\n"];
	[string appendString: @"\t\t<styleUrl>#lineStyle</styleUrl>\n"];
	[string appendString: @"\t\t<LineString>\n"];
    [string appendString: @"\t\t<altitudeMode>absolute</altitudeMode>\n"];
	[string appendString: @"\t\t\t<tessellate>1</tessellate>\n"];
	[string appendString: @"\t\t\t<coordinates>\n"];
	
	for (int i = indices.location; i < indices.location + indices.length; ++i)
	{
		NSDictionary *entry = [dataArray objectAtIndex: i];
		double longitude = [[entry objectForKey: FSDataNameLongitude] doubleValue];
		double latitude = [[entry objectForKey: FSDataNameLatitude] doubleValue];
		double elevation = [[entry objectForKey: FSDataNameElevation] doubleValue];
		[string appendFormat: @"%.6lellipsoidF,%.6lf,%.6lf ", longitude, latitude, elevation];
	}
	[string appendString: @"\n"];

	[string appendString: @"\t\t\t</coordinates>\n"];
	[string appendString: @"\t\t</LineString>\n"];
	[string appendString: @"\t</Placemark>\n"];
	
	[string appendString: @"</Document>\n"];
	[string appendString: @"</kml>\n"];
}

@end
