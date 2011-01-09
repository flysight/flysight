#include <avr/pgmspace.h>

#include <stdlib.h>
#include <string.h>

#include "Board/LEDs.h"
#include "FatFS/ff.h"
#include "Main.h"
#include "Tone.h"
#include "UBX.h"

#define FALSE 0
#define TRUE  (!FALSE)

static const char Config_default[] PROGMEM = "\
; GPS settings\r\n\
\r\n\
Model:     6    ; Dynamic model\r\n\
                ;   0 = Portable\r\n\
                ;   2 = Stationary\r\n\
                ;   3 = Pedestrian\r\n\
                ;   4 = Automotive\r\n\
                ;   5 = Sea\r\n\
                ;   6 = Airborne with < 1 G acceleration\r\n\
                ;   7 = Airborne with < 2 G acceleration\r\n\
                ;   8 = Airborne with < 4 G acceleration\r\n\
Rate:      200  ; Measurement rate (ms)\r\n\
\r\n\
; Tone settings\r\n\
\r\n\
Mode:      2    ; Measurement mode\r\n\
                ;   0 = Horizontal speed\r\n\
                ;   1 = Vertical speed\r\n\
                ;   2 = Glide ratio\r\n\
                ;   3 = Inverse glide ratio\r\n\
                ;   4 = Total speed\r\n\
Min:       0    ; Lowest pitch value\r\n\
                ;   cm/s        in Mode 0, 1, or 4\r\n\
                ;   ratio * 100 in Mode 2 or 3\r\n\
Max:       300  ; Highest pitch value\r\n\
                ;   cm/s        in Mode 0, 1, or 4\r\n\
                ;   ratio * 100 in Mode 2 or 3\r\n\
Volume:    6    ; 0 (min) to 8 (max)\r\n\
\r\n\
; Rate settings\r\n\
\r\n\
Mode_2:    9    ; Determines tone rate\r\n\
                ;   0 = Horizontal speed\r\n\
                ;   1 = Vertical speed\r\n\
                ;   2 = Glide ratio\r\n\
                ;   3 = Inverse glide ratio\r\n\
                ;   4 = Total speed\r\n\
                ;   9 = Change in Value 1\r\n\
Min_Val_2: 300  ; Lowest rate value\r\n\
                ;   cm/s          when Mode 2 = 0, 1, or 4\r\n\
                ;   ratio * 100   when Mode 2 = 2 or 3\r\n\
                ;   percent * 100 when Mode 2 = 9\r\n\
Max_Val_2: 1500 ; Highest rate value\r\n\
                ;   cm/s          when Mode 2 = 0, 1, or 4\r\n\
                ;   ratio * 100   when Mode 2 = 2 or 3\r\n\
                ;   percent * 100 when Mode 2 = 9\r\n\
Min_Rate:  100  ; Minimum rate (Hz * 100)\r\n\
Max_Rate:  500  ; Maximum rate (Hz * 100)\r\n\
Flatline:  0    ; Flatline at minimum rate\r\n\
                ;   0 = No\r\n\
                ;   1 = Yes\r\n\
\r\n\
; Thresholds\r\n\
\r\n\
Threshold: 1000 ; Minimum vertical speed for tone (cm/s)\r\n\
H_Thresh:  0    ; Minimum horiztonal speed for tone (cm/s)\r\n\
Speed_Acc: 150  ; Speed accuracy threshold (cm/s)";

static void Config_WriteString_P(
	const char *str,
	FIL        *file)
{
	char ch;

	while ((ch = pgm_read_byte(str++)))
	{
		f_putc(ch, file);
	}
}

void Config_Read(void)
{
	FIL     cfg_file;
	
	char    buf[80];
	size_t  len;
	char    *name;
	char    *result;
	
	int32_t val;

	FRESULT res;
	
	res = f_open(&cfg_file, "config.txt", FA_READ);
	if (res != FR_OK)
	{
		res = f_open(&cfg_file, "config.txt", FA_WRITE | FA_CREATE_ALWAYS);
		if (res != FR_OK) 
		{
			Main_activeLED = LEDS_RED;
			LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);
			return ;
		}

		Config_WriteString_P(Config_default, &cfg_file);
		f_close(&cfg_file);

		res = f_open(&cfg_file, "config.txt", FA_READ);
		if (res != FR_OK)
		{
			Main_activeLED = LEDS_RED;
			LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);
			return ;
		}
	}
	
	while (!f_eof(&cfg_file))
	{
		f_gets(buf, sizeof(buf), &cfg_file);

		len = strcspn(buf, ";");
		buf[len] = 0;
		
		name = strtok(buf, " \t:");
		if (name == 0) continue ;
		
		result = strtok(0, " \t:");
		if (result == 0) continue ;
		
		val = atoi(result);
		
		#define HANDLE_VALUE(s,w,r,t) \
			if ((t) && !strcmp(name, (s))) { (w) = (r); }

		HANDLE_VALUE("Model",     UBX_model,         val,     val >= 0 && val <= 8);
		HANDLE_VALUE("Rate",      UBX_rate,          val,     val >= 200);
		HANDLE_VALUE("Mode",      UBX_mode,          val,     val >= 0 && val <= 4);
		HANDLE_VALUE("Min",       UBX_min,           val,     TRUE);
		HANDLE_VALUE("Max",       UBX_max,           val,     TRUE);
		HANDLE_VALUE("Volume",    Tone_volume,       8 - val, val >= 0 && val <= 8);
		HANDLE_VALUE("Mode_2",    UBX_mode_2,        val,     (val >= 0 && val <= 4) || (val == 9));
		HANDLE_VALUE("Min_Val_2", UBX_min_2,         val,     TRUE);
		HANDLE_VALUE("Max_Val_2", UBX_max_2,         val,     TRUE);
		HANDLE_VALUE("Min_Rate",  UBX_min_rate,      val * TONE_RATE_ONE_HZ / 100, val >= 0);
		HANDLE_VALUE("Max_Rate",  UBX_max_rate,      val * TONE_RATE_ONE_HZ / 100, val >= 0);
		HANDLE_VALUE("Flatline",  UBX_flatline,      val,     val == 0 || val == 1);
		HANDLE_VALUE("Threshold", UBX_threshold,     val,     TRUE);
		HANDLE_VALUE("H_Thresh",  UBX_hThreshold,    val,     TRUE);
		HANDLE_VALUE("Speed_Acc", UBX_sAccThreshold, val,     TRUE);
		
		#undef HANDLE_VALUE
	}
	
	f_close(&cfg_file);
}
