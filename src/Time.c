// Time functions extracted from avr-libc

/*
 * (C)2012 Michael Duane Rice All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met:
 *
 * Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer. Redistributions in binary
 * form must reproduce the above copyright notice, this list of conditions
 * and the following disclaimer in the documentation and/or other materials
 * provided with the distribution. Neither the name of the copyright holders
 * nor the names of contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "Time.h"

/** One hour, expressed in seconds */
#define ONE_HOUR 3600

/** Angular degree, expressed in arc seconds */
#define ONE_DEGREE 3600

/** One day, expressed in seconds */
#define ONE_DAY 86400


enum _MONTHS_ {
    JANUARY,
    FEBRUARY,
    MARCH,
    APRIL,
    MAY,
    JUNE,
    JULY,
    AUGUST,
    SEPTEMBER,
    OCTOBER,
    NOVEMBER,
    DECEMBER
};

unsigned char
is_leap_year(int year)
{
    div_t           d;

    /* year must be divisible by 4 to be a leap year */
    if (year & 3)
        return 0;

    /* If theres a remainder after division by 100, year is not divisible by 100 or 400 */
    d = div(year, 100);
    if (d.rem)
        return 1;

    /* If the quotient is divisible by 4, then year is divisible by 400 */
    if ((d.quot & 3) == 0)
        return 1;

    return 0;
}


/*
    'Break down' a y2k time stamp into the elements of struct tm.
    Unlike mktime(), this function does not 'normalize' the elements of timeptr.

*/

uint32_t
mk_gmtime(uint16_t year, uint8_t mon, uint8_t mday, uint8_t hour, uint8_t min, uint8_t sec)
{

    uint32_t        ret;
    uint32_t        tmp;
    int             n, m, d, leaps;

    /*
        Determine elapsed whole days since the epoch to the beginning of this year. Since our epoch is
        at a conjunction of the leap cycles, we can do this rather quickly.
        */
    n = year - 2000;
    leaps = 0;
    if (n) {
        m = n - 1;
        leaps = m / 4;
        leaps -= m / 100;
        leaps++;
    }
    tmp = 365UL * n + leaps;

    /*
                Derive the day of year from month and day of month. We use the pattern of 31 day months
                followed by 30 day months to our advantage, but we must 'special case' Jan/Feb, and
                account for a 'phase change' between July and August (153 days after March 1).
            */
    d = mday - 1;   /* tm_mday is one based */

    /* handle Jan/Feb as a special case */
    if (mon < 2) {
        if (mon)
            d += 31;

    } else {
        n = 59 + is_leap_year(year);
        d += n;
        n = mon - MARCH;

        /* account for phase change */
        if (n > (JULY - MARCH))
            d += 153;
        n %= 5;

        /*
         * n is now an index into a group of alternating 31 and 30
         * day months... 61 day pairs.
         */
        m = n / 2;
        m *= 61;
        d += m;

        /*
         * if n is odd, we are in the second half of the
         * month pair
         */
        if (n & 1)
            d += 31;
    }

    /* Add day of year to elapsed days, and convert to seconds */
    tmp += d;
    tmp *= ONE_DAY;
    ret = tmp;

    /* compute 'fractional' day */
    tmp = hour;
    tmp *= ONE_HOUR;
    tmp += min * 60UL;
    tmp += sec;

    ret += tmp;

    return ret;
}

void
gmtime_r(const uint32_t timer, uint16_t *year, uint8_t *mon, uint8_t *mday, uint8_t *hour, uint8_t *min, uint8_t *sec)
{
    int32_t         fract;
    ldiv_t          lresult;
    div_t           result;
    uint16_t        days, n, leapyear, years;

    /* break down timer into whole and fractional parts of 1 day */
    days = timer / 86400UL;
    fract = timer % 86400UL;

    /*
            Extract hour, minute, and second from the fractional day
        */
    lresult = ldiv(fract, 60L);
    *sec = lresult.rem;
    result = div(lresult.quot, 60);
    *min = result.rem;
    *hour = result.quot;

    /*
        * Our epoch year has the property of being at the conjunction of all three 'leap cycles',
        * 4, 100, and 400 years ( though we can ignore the 400 year cycle in this library).
        *
        * Using this property, we can easily 'map' the time stamp into the leap cycles, quickly
        * deriving the year and day of year, along with the fact of whether it is a leap year.
        */

    /* map into a 100 year cycle */
    lresult = ldiv((long) days, 36525L);
    years = 100 * lresult.quot;

    /* map into a 4 year cycle */
    lresult = ldiv(lresult.rem, 1461L);
    years += 4 * lresult.quot;
    days = lresult.rem;
    if (years > 100)
        days++;

    /*
         * 'years' is now at the first year of a 4 year leap cycle, which will always be a leap year,
         * unless it is 100. 'days' is now an index into that cycle.
         */
    leapyear = 1;
    if (years == 100)
        leapyear = 0;

    /* compute length, in days, of first year of this cycle */
    n = 364 + leapyear;

    /*
     * if the number of days remaining is greater than the length of the
     * first year, we make one more division.
     */
    if (days > n) {
        days -= leapyear;
        leapyear = 0;
        result = div(days, 365);
        years += result.quot;
        days = result.rem;
    }
    *year = 2000 + years;

    /*
            Given the year, day of year, and leap year indicator, we can break down the
            month and day of month. If the day of year is less than 59 (or 60 if a leap year), then
            we handle the Jan/Feb month pair as an exception.
        */
    n = 59 + leapyear;
    if (days < n) {
        /* special case: Jan/Feb month pair */
        result = div(days, 31);
        *mon = result.quot;
        *mday = result.rem;
    } else {
        /*
            The remaining 10 months form a regular pattern of 31 day months alternating with 30 day
            months, with a 'phase change' between July and August (153 days after March 1).
            We proceed by mapping our position into either March-July or August-December.
            */
        days -= n;
        result = div(days, 153);
        uint8_t m;
        m = 2 + result.quot * 5;

        /* map into a 61 day pair of months */
        result = div(result.rem, 61);
        m += result.quot * 2;

        /* map into a month */
        result = div(result.rem, 31);
        m += result.quot;
        *mday = result.rem;
        *mon = m;
    }

    /*
            Cleanup and return
        */
    (*mday)++; /* tm_mday is 1 based */

}