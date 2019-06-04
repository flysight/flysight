/***************************************************************************
**                                                                        **
**  FlySight firmware                                                     **
**  Copyright 2018 Michael Cooper, Will Glynn                             **
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

#include <avr/eeprom.h>
#include <avr/pgmspace.h>

#include <stdlib.h>
#include <string.h>

#include "Board/LEDs.h"
#include "FatFS/ff.h"
#include "Config.h"
#include "Debug.h"
#include "Log.h"
#include "Main.h"
#include "Tone.h"
#include "UBX.h"
#include "Version.h"

#define FALSE 0
#define TRUE  (!FALSE)

static const char Config_default[] PROGMEM = "\
; Firmware version " FLYSIGHT_VERSION "\r\n\
\r\n\
; For information on configuring FlySight, please go to\r\n\
;     http://flysight.ca/wiki\r\n\
\r\n\
; GPS settings\r\n\
\r\n\
Model:     7     ; Dynamic model\r\n\
                 ;   0 = Portable\r\n\
                 ;   2 = Stationary\r\n\
                 ;   3 = Pedestrian\r\n\
                 ;   4 = Automotive\r\n\
                 ;   5 = Sea\r\n\
                 ;   6 = Airborne with < 1 G acceleration\r\n\
                 ;   7 = Airborne with < 2 G acceleration\r\n\
                 ;   8 = Airborne with < 4 G acceleration\r\n\
Rate:      200   ; Measurement rate (ms)\r\n\
\r\n\
; Tone settings\r\n\
\r\n\
Mode:      2     ; Measurement mode\r\n\
                 ;   0 = Horizontal speed\r\n\
                 ;   1 = Vertical speed\r\n\
                 ;   2 = Glide ratio\r\n\
                 ;   3 = Inverse glide ratio\r\n\
                 ;   4 = Total speed\r\n\
                 ;   11 = Dive angle\r\n\
Min:       0     ; Lowest pitch value\r\n\
                 ;   cm/s        in Mode 0, 1, or 4\r\n\
                 ;   ratio * 100 in Mode 2 or 3\r\n\
                 ;   degrees     in Mode 11\r\n\
Max:       300   ; Highest pitch value\r\n\
                 ;   cm/s        in Mode 0, 1, or 4\r\n\
                 ;   ratio * 100 in Mode 2 or 3\r\n\
                 ;   degrees     in Mode 11\r\n\
Limits:    1     ; Behaviour when outside bounds\r\n\
                 ;   0 = No tone\r\n\
                 ;   1 = Min/max tone\r\n\
                 ;   2 = Chirp up/down\r\n\
                 ;   3 = Chirp down/up\r\n\
Volume:    6     ; 0 (min) to 8 (max)\r\n\
\r\n\
; Rate settings\r\n\
\r\n\
Mode_2:    9     ; Determines tone rate\r\n\
                 ;   0 = Horizontal speed\r\n\
                 ;   1 = Vertical speed\r\n\
                 ;   2 = Glide ratio\r\n\
                 ;   3 = Inverse glide ratio\r\n\
                 ;   4 = Total speed\r\n\
                 ;   8 = Magnitude of Value 1\r\n\
                 ;   9 = Change in Value 1\r\n\
                 ;   11 = Dive angle\r\n\
Min_Val_2: 300   ; Lowest rate value\r\n\
                 ;   cm/s          when Mode 2 = 0, 1, or 4\r\n\
                 ;   ratio * 100   when Mode 2 = 2 or 3\r\n\
                 ;   percent * 100 when Mode 2 = 9\r\n\
                 ;   degrees       when Mode 2 = 11\r\n\
Max_Val_2: 1500  ; Highest rate value\r\n\
                 ;   cm/s          when Mode 2 = 0, 1, or 4\r\n\
                 ;   ratio * 100   when Mode 2 = 2 or 3\r\n\
                 ;   percent * 100 when Mode 2 = 9\r\n\
                 ;   degrees       when Mode 2 = 11\r\n\
Min_Rate:  100   ; Minimum rate (Hz * 100)\r\n\
Max_Rate:  500   ; Maximum rate (Hz * 100)\r\n\
Flatline:  0     ; Flatline at minimum rate\r\n\
                 ;   0 = No\r\n\
                 ;   1 = Yes\r\n\
\r\n\
; Speech settings\r\n\
\r\n\
Sp_Rate:   0     ; Speech rate (s)\r\n\
                 ;   0 = No speech\r\n\
Sp_Volume: 8     ; 0 (min) to 8 (max)\r\n\
\r\n\
Sp_Mode:   2     ; Speech mode\r\n\
                 ;   0 = Horizontal speed\r\n\
                 ;   1 = Vertical speed\r\n\
                 ;   2 = Glide ratio\r\n\
                 ;   3 = Inverse glide ratio\r\n\
                 ;   4 = Total speed\r\n\
                 ;   5 = Altitude above DZ_Elev\r\n\
                 ;   11 = Dive angle\r\n\
Sp_Units:  1     ; Speech units\r\n\
                 ;   0 = km/h or m\r\n\
                 ;   1 = mph or feet\r\n\
Sp_Dec:    1     ; Speech precision\r\n\
                 ;   Altitude step in Mode 5\r\n\
                 ;   Decimal places in all other Modes\r\n\
\r\n\
; Thresholds\r\n\
\r\n\
V_Thresh:  1000  ; Minimum vertical speed for tone (cm/s)\r\n\
H_Thresh:  0     ; Minimum horizontal speed for tone (cm/s)\r\n\
\r\n\
; Miscellaneous\r\n\
\r\n\
Use_SAS:   1     ; Use skydiver's airspeed\r\n\
                 ;   0 = No\r\n\
                 ;   1 = Yes\r\n\
TZ_Offset: 0     ; Timezone offset of output files in seconds\r\n\
                 ;   -14400 = UTC-4 (EDT)\r\n\
                 ;   -18000 = UTC-5 (EST, CDT)\r\n\
                 ;   -21600 = UTC-6 (CST, MDT)\r\n\
                 ;   -25200 = UTC-7 (MST, PDT)\r\n\
                 ;   -28800 = UTC-8 (PST)\r\n\
\r\n\
; Initialization\r\n\
\r\n\
Init_Mode: 0     ; When the FlySight is powered on\r\n\
                 ;   0 = Do nothing\r\n\
                 ;   1 = Test speech mode\r\n\
                 ;   2 = Play file\r\n\
Init_File: 0     ; File to be played\r\n\
\r\n\
; Alarm settings\r\n\
\r\n\
; WARNING: GPS measurements depend on very weak signals\r\n\
;          received from orbiting satellites. As such, they\r\n\
;          are prone to interference, and should NEVER be\r\n\
;          relied upon for life saving purposes.\r\n\
\r\n\
;          UNDER NO CIRCUMSTANCES SHOULD THESE ALARMS BE\r\n\
;          USED TO INDICATE DEPLOYMENT OR BREAKOFF ALTITUDE.\r\n\
\r\n\
; NOTE:    Alarm elevations are given in meters above ground\r\n\
;          elevation, which is specified in DZ_Elev.\r\n\
\r\n\
Win_Above:     0 ; Window above each alarm (m)\r\n\
Win_Below:     0 ; Window below each alarm (m)\r\n\
DZ_Elev:       0 ; Ground elevation (m above sea level)\r\n\
\r\n\
Alarm_Elev:    0 ; Alarm elevation (m above ground level)\r\n\
Alarm_Type:    0 ; Alarm type\r\n\
                 ;   0 = No alarm\r\n\
                 ;   1 = Beep\r\n\
                 ;   2 = Chirp up\r\n\
                 ;   3 = Chirp down\r\n\
                 ;   4 = Play file\r\n\
Alarm_File:    0 ; File to be played\r\n\
\r\n\
; Altitude mode settings\r\n\
\r\n\
; WARNING: GPS measurements depend on very weak signals\r\n\
;          received from orbiting satellites. As such, they\r\n\
;          are prone to interference, and should NEVER be\r\n\
;          relied upon for life saving purposes.\r\n\
\r\n\
;          UNDER NO CIRCUMSTANCES SHOULD ALTITUDE MODE BE\r\n\
;          USED TO INDICATE DEPLOYMENT OR BREAKOFF ALTITUDE.\r\n\
\r\n\
; NOTE:    Altitude is given relative to ground elevation,\r\n\
;          which is specified in DZ_Elev. Altitude mode will\r\n\
;          not function below 1500 m above ground.\r\n\
\r\n\
Alt_Units:     1 ; Altitude units\r\n\
                 ;   0 = m\r\n\
                 ;   1 = ft\r\n\
Alt_Step:      0 ; Altitude between announcements\r\n\
                 ;   0 = No altitude\r\n\
\r\n\
; Silence windows\r\n\
\r\n\
; NOTE:    Silence windows are given in meters above ground\r\n\
;          elevation, which is specified in DZ_Elev. Tones\r\n\
;          will be silenced during these windows and only\r\n\
;          alarms will be audible.\r\n\
\r\n\
Win_Top:       0 ; Silence window top (m)\r\n\
Win_Bottom:    0 ; Silence window bottom (m)\r\n";

static const char Config_Model[] PROGMEM      = "Model";
static const char Config_Rate[] PROGMEM       = "Rate";
static const char Config_Mode[] PROGMEM       = "Mode";
static const char Config_Min[] PROGMEM        = "Min";
static const char Config_Max[] PROGMEM        = "Max";
static const char Config_Limits[] PROGMEM     = "Limits";
static const char Config_Volume[] PROGMEM     = "Volume";
static const char Config_Mode_2[] PROGMEM     = "Mode_2";
static const char Config_Min_Val_2[] PROGMEM  = "Min_Val_2";
static const char Config_Max_Val_2[] PROGMEM  = "Max_Val_2";
static const char Config_Min_Rate[] PROGMEM   = "Min_Rate";
static const char Config_Max_Rate[] PROGMEM   = "Max_Rate";
static const char Config_Flatline[] PROGMEM   = "Flatline";
static const char Config_Sp_Mode[] PROGMEM    = "Sp_Mode";
static const char Config_Sp_Units[] PROGMEM   = "Sp_Units";
static const char Config_Sp_Rate[] PROGMEM    = "Sp_Rate";
static const char Config_Sp_Dec[] PROGMEM     = "Sp_Dec";
static const char Config_Sp_Volume[] PROGMEM  = "Sp_Volume";
static const char Config_V_Thresh[] PROGMEM   = "V_Thresh";
static const char Config_H_Thresh[] PROGMEM   = "H_Thresh";
static const char Config_Use_SAS[] PROGMEM    = "Use_SAS";
static const char Config_Window[] PROGMEM     = "Window";
static const char Config_Window_Above[] PROGMEM = "Win_Above";
static const char Config_Window_Below[] PROGMEM = "Win_Below";
static const char Config_DZ_Elev[] PROGMEM    = "DZ_Elev";
static const char Config_Alarm_Elev[] PROGMEM = "Alarm_Elev";
static const char Config_Alarm_Type[] PROGMEM = "Alarm_Type";
static const char Config_Alarm_File[] PROGMEM = "Alarm_File";
static const char Config_TZ_Offset[] PROGMEM  = "TZ_Offset";
static const char Config_Init_Mode[] PROGMEM  = "Init_Mode";
       const char Config_Init_File[] PROGMEM  = "Init_File";
static const char Config_Win_Top[] PROGMEM    = "Win_Top";
static const char Config_Win_Bottom[] PROGMEM = "Win_Bottom";
static const char Config_Alt_Units[] PROGMEM  = "Alt_Units";
static const char Config_Alt_Step[] PROGMEM   = "Alt_Step";

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

static FRESULT Config_ReadSingle(
	const char *dir,
	const char *filename)
{
	size_t  len;
	char    *name;
	char    *result;
	int32_t val;

	FRESULT res;

	res = f_chdir(dir);
	if (res != FR_OK) return res;
	
	res = f_open(&Main_file, filename, FA_READ);
	if (res != FR_OK) return res;

	while (!f_eof(&Main_file))
	{
		f_gets(UBX_buffer.buffer, sizeof(UBX_buffer.buffer), &Main_file);

		len = strcspn(UBX_buffer.buffer, ";");
		UBX_buffer.buffer[len] = 0;
		
		name = strtok(UBX_buffer.buffer, " \r\n\t:");
		if (name == 0) continue ;
		
		result = strtok(0, " \r\n\t:");
		if (result == 0) continue ;
		
		val = atol(result);
		
		#define HANDLE_VALUE(s,w,r,t) \
			if ((t) && !strcmp_P(name, (s))) { (w) = (r); }

		HANDLE_VALUE(Config_Model,     UBX_model,        val, val >= 0 && val <= 8);
		HANDLE_VALUE(Config_Rate,      UBX_rate,         val, val >= 100);
		HANDLE_VALUE(Config_Mode,      UBX_mode,         val, (val >= 0 && val <= 4) || (val == 11));
		HANDLE_VALUE(Config_Min,       UBX_min,          val, TRUE);
		HANDLE_VALUE(Config_Max,       UBX_max,          val, TRUE);
		HANDLE_VALUE(Config_Limits,    UBX_limits,       val, val >= 0 && val <= 2);
		HANDLE_VALUE(Config_Volume,    Tone_volume,      8 - val, val >= 0 && val <= 8);
		HANDLE_VALUE(Config_Mode_2,    UBX_mode_2,       val, (val >= 0 && val <= 4) || (val >= 8 && val <= 9) || (val == 11));
		HANDLE_VALUE(Config_Min_Val_2, UBX_min_2,        val, TRUE);
		HANDLE_VALUE(Config_Max_Val_2, UBX_max_2,        val, TRUE);
		HANDLE_VALUE(Config_Min_Rate,  UBX_min_rate,     val * TONE_RATE_ONE_HZ / 100, val >= 0);
		HANDLE_VALUE(Config_Max_Rate,  UBX_max_rate,     val * TONE_RATE_ONE_HZ / 100, val >= 0);
		HANDLE_VALUE(Config_Flatline,  UBX_flatline,     val, val == 0 || val == 1);
		HANDLE_VALUE(Config_Sp_Rate,   UBX_sp_rate,      val * 1000, val >= 0 && val <= 32);
		HANDLE_VALUE(Config_Sp_Volume, Tone_sp_volume,   8 - val, val >= 0 && val <= 8);
		HANDLE_VALUE(Config_V_Thresh,  UBX_threshold,    val, TRUE);
		HANDLE_VALUE(Config_H_Thresh,  UBX_hThreshold,   val, TRUE);
		HANDLE_VALUE(Config_Use_SAS,   UBX_use_sas,      val, val == 0 || val == 1);
		HANDLE_VALUE(Config_Window,    UBX_alarm_window_above, val * 1000, TRUE);
		HANDLE_VALUE(Config_Window,    UBX_alarm_window_below, val * 1000, TRUE);
		HANDLE_VALUE(Config_Window_Above, UBX_alarm_window_above, val * 1000, TRUE);
		HANDLE_VALUE(Config_Window_Below, UBX_alarm_window_below, val * 1000, TRUE);
		HANDLE_VALUE(Config_DZ_Elev,   UBX_dz_elev,      val * 1000, TRUE);
		HANDLE_VALUE(Config_TZ_Offset, Log_tz_offset,    val, TRUE);
		HANDLE_VALUE(Config_Init_Mode, UBX_init_mode,    val, val >= 0 && val <= 2);
		HANDLE_VALUE(Config_Alt_Units, UBX_alt_units,    val, val >= 0 && val <= 1);
		HANDLE_VALUE(Config_Alt_Step,  UBX_alt_step,     val, val >= 0);
		
		#undef HANDLE_VALUE
		
		if (!strcmp_P(name, Config_Init_File))
		{
			result[8] = '\0';
			strcpy(UBX_init_filename, result);
		}
		
		if (!strcmp_P(name, Config_Alarm_Elev) && UBX_num_alarms < UBX_MAX_ALARMS)
		{
			++UBX_num_alarms;
			UBX_alarms[UBX_num_alarms - 1].elev = val * 1000;
			UBX_alarms[UBX_num_alarms - 1].type = 0;
			UBX_alarms[UBX_num_alarms - 1].filename[0] = '\0';
		}
		if (!strcmp_P(name, Config_Alarm_Type) && UBX_num_alarms <= UBX_MAX_ALARMS)
		{
			UBX_alarms[UBX_num_alarms - 1].type = val;
		}
		if (!strcmp_P(name, Config_Alarm_File) && UBX_num_alarms <= UBX_MAX_ALARMS)
		{
			result[8] = '\0';
			strcpy(UBX_alarms[UBX_num_alarms - 1].filename, result);
		}
		
		if (!strcmp_P(name, Config_Win_Top) && UBX_num_windows < UBX_MAX_WINDOWS)
		{
			++UBX_num_windows;
			UBX_windows[UBX_num_windows - 1].top = val * 1000;
		}
		if (!strcmp_P(name, Config_Win_Bottom) && UBX_num_windows <= UBX_MAX_WINDOWS)
		{
			UBX_windows[UBX_num_windows - 1].bottom = val * 1000;
		}

		if (!strcmp_P(name, Config_Sp_Mode) && UBX_num_speech < UBX_MAX_SPEECH)
		{
			++UBX_num_speech;
			UBX_speech[UBX_num_speech - 1].mode = val;
			UBX_speech[UBX_num_speech - 1].units = UBX_UNITS_MPH;
			UBX_speech[UBX_num_speech - 1].decimals = 1;
		}
		if (!strcmp_P(name, Config_Sp_Units) && UBX_num_speech <= UBX_MAX_SPEECH)
		{
			UBX_speech[UBX_num_speech - 1].units = val;
		}
		if (!strcmp_P(name, Config_Sp_Dec) && UBX_num_speech <= UBX_MAX_SPEECH)
		{
			result[8] = '\0';
			UBX_speech[UBX_num_speech - 1].decimals = val;
		}
	}
	
	f_close(&Main_file);
	
	return FR_OK;
}

void Config_Read(void)
{
	FRESULT res;

	res = Config_ReadSingle("\\", "config.txt");
	
	if (res != FR_OK)
	{
		res = f_chdir("\\");
		res = f_open(&Main_file, "config.txt", FA_WRITE | FA_CREATE_ALWAYS);
		if (res != FR_OK) 
		{
			Main_activeLED = LEDS_RED;
			LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);
			return ;
		}

		Config_WriteString_P(Config_default, &Main_file);
		f_close(&Main_file);
	}

	eeprom_read_block(UBX_buffer.filename, CONFIG_FNAME_ADDR, CONFIG_FNAME_LEN);

	if (UBX_buffer.filename[0] != 0 && UBX_buffer.filename[0] != 0xff)
	{
		res = Config_ReadSingle("\\config", UBX_buffer.filename);
	}
}
