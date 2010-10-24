#include <stdio.h>
#include <string.h>

#include "Board/LEDs.h"
#include "FatFS/ff.h"
#include "Log.h"
#include "Main.h"
#include "Power.h"

#define FILE_NUMBER_ADDR 0

static FIL     Log_csv;
static uint8_t Log_initialized = 0;
static DWORD   Log_fattime;

DWORD get_fattime(void)
{
	return Log_fattime;
}

void Log_Flush(void)
{
	if (Log_initialized)
	{
		f_sync(&Log_csv);
	}
}

void Log_WriteCSV(
	char *format,
	...)
{
	if (Log_initialized)
	{
		char    buffer[150];
		va_list args;

		va_start(args, format);
		vsprintf(buffer, format, args);
		
		f_puts(buffer, &Log_csv);
		
		va_end(args);
	}
}

void Log_WriteInt32(
	int32_t val,
	int8_t  dec)
{
	char    fmt[10];
	char    buf[20];
	uint8_t len;
	char    temp;
	
	sprintf(fmt, "%%0%dld", dec + 1);
	sprintf(buf, fmt, val);
	len = strlen(buf);
	
	temp = buf[len - dec];
	buf[len - dec] = 0;
	f_puts(buf, &Log_csv);

	f_puts(".", &Log_csv);
	
	buf[len - dec] = temp;
	f_puts(&buf[len - dec], &Log_csv);
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

	Log_fattime = ((DWORD) (year - 1980) << 25) + 
	              ((DWORD) month         << 21) + 
	              ((DWORD) day           << 16) + 
	              ((DWORD) hour          << 11) + 
	              ((DWORD) min           << 5)  + 
	              ((DWORD) (sec / 2));

	sprintf(fname, "%02u-%02u-%02u", year % 100, month, day);
	
	res = f_mkdir(fname);
	res = f_chdir(fname);

	sprintf(fname, "%02u-%02u-%02u.csv", hour, min, sec);

	res = f_open(&Log_csv, fname, FA_WRITE | FA_CREATE_ALWAYS);
	if (res != FR_OK)
	{
		Main_activeLED = LEDS_RED;
		LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);
		return ;
	}

	f_puts("time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,gpsFix,numSV\r\n", 
		   &Log_csv);
	f_puts(",(deg),(deg),(m),(m/s),(m/s),(m/s),(m),(m),(m/s),,,\r\n", 
		   &Log_csv);
	
	Log_initialized = 1;
}

uint8_t Log_IsInitialized(void)
{
	return Log_initialized;
}
