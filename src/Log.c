#include <avr/pgmspace.h>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "Board/LEDs.h"
#include "FatFS/ff.h"
#include "Log.h"
#include "Main.h"
#include "Power.h"
#include "Time.h"

#define FILE_NUMBER_ADDR 0

int16_t Log_tz_offset = 0;

static uint8_t Log_initialized = 0;
static DWORD   Log_fattime;

static const char Log_header[] PROGMEM = 
	"time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,gpsFix,numSV\r\n"
	",(deg),(deg),(m),(m/s),(m/s),(m/s),(m),(m),(m/s),,,\r\n";

DWORD get_fattime(void)
{
	return Log_fattime;
}

void Log_Flush(void)
{
	if (Log_initialized)
	{
		f_sync(&Main_file);
	}
}

void Log_WriteChar(char ch)
{
    f_putc(ch, &Main_file);
}

char *Log_WriteInt32ToBuf(char *ptr, int32_t val, int8_t dec, int8_t dot, char delimiter)
{
    int32_t value = val > 0 ? val : -val;

    *--ptr = 0;
    *--ptr = delimiter;
    while (value > 0 || dec > 0)
    {
        ldiv_t res = ldiv(value, 10);
        *--ptr = res.rem + '0';
        value = res.quot;
        if (--dec == 0 && dot)
        {
            *--ptr = '.';
        }
    }
    if (*ptr == '.' || *ptr == 0)
    {
        *--ptr = '0';
    }
    if (val < 0)
    {
        *--ptr = '-';
    }
	
	return ptr;
}

void Log_WriteInt32(int32_t val, int8_t dec, int8_t dot, char delimiter)
{
	char buf[16];

    f_puts(Log_WriteInt32ToBuf(buf + sizeof(buf), val, dec, dot, delimiter), &Main_file);
}

static void Log_ToDate(char* name, uint8_t a, uint8_t b, uint8_t c)
{
    name[0] = '0' + (a / 10); 
    name[1] = '0' + (a % 10); 
    name[2] = '-';
    name[3] = '0' + (b / 10); 
    name[4] = '0' + (b % 10); 
    name[5] = '-';
    name[6] = '0' + (c / 10); 
    name[7] = '0' + (c % 10);
    name[8] = 0;
}

static void Log_WriteString_P(
	const char *str,
	FIL        *file)
{
	char ch;

	while ((ch = pgm_read_byte(str++)))
	{
		f_putc(ch, file);
	}
}

void Log_Init(
	uint16_t year,
	uint8_t  month,
	uint8_t  day,
	uint8_t  hour,
	uint8_t  min,
	uint8_t  sec)
{
	char    fname[13];

	FRESULT res;

	if (Log_initialized) return ;
	
	// Convert UTC YMD-HMS to a single value, offset it, and convert back
	{
		uint32_t timestamp = mk_gmtime(year, month, day, hour, min, sec);
		timestamp += Log_tz_offset;
		gmtime_r(timestamp, &year, &month, &day, &hour, &min, &sec);
	}

	Log_fattime = ((DWORD) (year - 1980) << 25) + 
	              ((DWORD) month         << 21) + 
	              ((DWORD) day           << 16) + 
	              ((DWORD) hour          << 11) + 
	              ((DWORD) min           << 5)  + 
	              ((DWORD) (sec / 2));

    // create folder.
    year = year % 100;
    Log_ToDate(fname, year, month, day);

	res = f_mkdir(fname);
	res = f_chdir(fname);

    // create file.
    Log_ToDate(fname, hour, min, sec);
    fname[ 8] = '.';
    fname[ 9] = 'c';
    fname[10] = 's';
    fname[11] = 'v';
    fname[12] = 0;

	res = f_open(&Main_file, fname, FA_WRITE | FA_CREATE_ALWAYS);
	if (res != FR_OK)
	{
		Main_activeLED = LEDS_RED;
		LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);
		return ;
	}

	Log_WriteString_P(Log_header, &Main_file);
	
	Log_initialized = 1;
}

uint8_t Log_IsInitialized(void)
{
	return Log_initialized;
}
