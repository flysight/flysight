/***************************************************************************
**                                                                        **
**  FlySight firmware                                                     **
**  Copyright 2018 Michael Cooper, Luke Hederman                          **
**                                                                        **
**  This program is free software: you can redistribute it and/or modify  **
**  it under the terms of the GNU General Public License as published by  **
**  the Free Software Foundation, either version 3 of the License, or     **
**  (at your option) any later version.                                   **
**                                                                        **
**  This program is distributed in the hope that it will be useful,       **
**  but WITHOUT ANY WARRANTY; without even the implied warranty of        **
**  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         **
**  GNU General Public License for more details.                          **
**                                                                        **
**  You should have received a copy of the GNU General Public License     **
**  along with this program.  If not, see <http://www.gnu.org/licenses/>. **
**                                                                        **
****************************************************************************
**  Contact: Michael Cooper                                               **
**  Website: http://flysight.ca/                                          **
****************************************************************************/

#include <stdio.h>
#include <math.h>

#include "UBX.h"
#include "Nav.h"

// Code based on Eric Moore's Deathpod3000 code
// http://deathpod3000.wordpress.com/

//Convert degrees to radians
float dtor(float degrees)
{
	return (degrees * M_PI / 180);
}

//Convert radians to degrees
float rtod(float radians)
{
	return (radians * 180.0 / M_PI);
}

//Round float to integer
int32_t round_nearest(float f)
{
	return (int32_t)(f + 0.5);
}

//Calculate relative bearing given bearing and heading in degrees
int calcRelBearing(int bearing, int heading)
{
	int relBearing;
	relBearing = bearing - heading;
	if(relBearing > 180)
	{
		relBearing = bearing - (heading + 360);
	}
	if(relBearing < -180)
	{
		relBearing = bearing - (heading - 360);
	}
	return (int)relBearing;
}

//Calculate bearing from lat1/lon1 to lat2/lon2
//Note lat1/lon1/lat2/lon2 must be in radians
//Returns int bearing in degrees
int calcBearing(float lat1, float lon1, float lat2, float lon2)
{
	float bearing = 0.0;
	float dLon, y, x, cosLat2;

	//determine angle
	dLon = lon2 - lon1;
	cosLat2= cos(lat2);
	y = sin(dLon) * cosLat2;
	x = (cos(lat1) * sin(lat2)) - (sin(lat1) * cosLat2 * cos(dLon));
	bearing = atan2(y, x);
	bearing = rtod(bearing); //convert to degrees
	bearing = fmod((bearing + 360.0), 360); //use mod to turn -90 = 270
	return round_nearest(bearing);
}

//Calculate relative bearing from lat lon to destination given heading from GPS
//Heading in degrees
int calcDirection(int32_t lat, int32_t lon, int32_t head)
{
	float cLat, cLon, dLat, dLon;
	int heading;
	int bearing;  // bearing required to get from current position to destination
	int relBearing; // bearing change relative to current heading

	const int32_t GeoCo_Scale = 10000000;
	const int32_t Heading_Scale = 100000;

	//Current position in radians
	cLat = dtor((float)lat/GeoCo_Scale);
	cLon = dtor((float)lon/GeoCo_Scale);
	//Destination in radians from settings file.
	dLat = dtor((float)UBX_dLat/GeoCo_Scale);
	dLon = dtor((float)UBX_dLon/GeoCo_Scale);
	//convert heading
	heading=round_nearest((float)head/Heading_Scale);
	//calculate required bearing
	bearing = calcBearing(cLat,cLon,dLat,dLon);
	//calculate relative bearing
	relBearing = calcRelBearing(bearing,heading);
	return relBearing;
}

//Calculate distance form lat1/lon1 to lat2/lon2 using haversine formula
//Note lat1/lon1/lat2/lon2 must be in radians
//Returns int32_t distance in same units as EARTH_RADIUS
int32_t calcDistanceRad(float lat1, float lon1, float lat2, float lon2)
{
	float dlon, dlat, a, c;
	float dist = 0.0;
	const int32_t EARTH_RADIUS = 6371100; //radius of the earth (6371100 meters) in feet 20925656.2
	dlon = lon2 - lon1;
	dlat = lat2 - lat1;
	a = pow(sin(dlat/2),2) + cos(lat1) * cos(lat2) * pow(sin(dlon/2),2);
	//c = 2 * atan2(sqrt(a), sqrt(1-a)); //parameters should be reversed for VBA\Excel
	c = 2 * asin(sqrt(a));

	dist = EARTH_RADIUS * c;
	return round_nearest(dist);
}

//Calculate distance
int32_t calcDistance(int32_t lat1, int32_t lon1, int32_t lat2, int32_t lon2)
{
	float cLat, cLon, dLat, dLon;
	int32_t dist;
	const int32_t GeoCo_Scale = 10000000;
	cLat = dtor((float)lat1/GeoCo_Scale);
	cLon = dtor((float)lon1/GeoCo_Scale);
	dLat = dtor((float)lat2/GeoCo_Scale);
	dLon = dtor((float)lon2/GeoCo_Scale);
	
	dist = calcDistanceRad(cLat,cLon,dLat,dLon);
	return dist;
}
