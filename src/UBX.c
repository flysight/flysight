/***************************************************************************
**                                                                        **
**  FlySight firmware                                                     **
**  Copyright 2018 Michael Cooper, Tom van Dijck                          **
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

#include <math.h>
#include <stddef.h>
#include <stdio.h>
#include <string.h>

#include <util/delay.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/pgmspace.h>

#include "Board/LEDs.h"
#include "Log.h"
#include "Main.h"
#include "Power.h"
#include "Stack.h"
#include "Timer.h"
#include "Tone.h"
#include "uart.h"
#include "UBX.h"

/*
#define STACK_PAINTING	// Define to enable stack debugging
*/

#define ABS(a)   ((a) < 0     ? -(a) : (a))
#define MIN(a,b) (((a) < (b)) ?  (a) : (b))
#define MAX(a,b) (((a) > (b)) ?  (a) : (b))

#define UBX_INVALID_VALUE   INT32_MAX

#define UBX_TIMEOUT         500 // ACK/NAK timeout (ms)
#define UBX_MAX_PAYLOAD_LEN 52

#define UBX_SYNC_1          0xb5
#define UBX_SYNC_2          0x62

#define UBX_NAV             0x01
#define UBX_NAV_POSLLH      0x02
#define UBX_NAV_STATUS      0x03
#define UBX_NAV_SOL         0x06
#define UBX_NAV_VELNED      0x12
#define UBX_NAV_TIMEUTC     0x21

#define UBX_ACK             0x05
#define UBX_ACK_NAK         0x00
#define UBX_ACK_ACK         0x01

#define UBX_CFG             0x06
#define UBX_CFG_PRT         0x00
#define UBX_CFG_MSG         0x01
#define UBX_CFG_RST         0x04
#define UBX_CFG_RATE        0x08
#define UBX_CFG_NAV5        0x24

#define UBX_NMEA            0xf0
#define UBX_NMEA_GPGGA      0x00
#define UBX_NMEA_GPGLL      0x01
#define UBX_NMEA_GPGSA      0x02
#define UBX_NMEA_GPGSV      0x03
#define UBX_NMEA_GPRMC      0x04
#define UBX_NMEA_GPVTG      0x05

#define UBX_SAVED_LEN       4

#define UBX_MSG_POSLLH      0x01
#define UBX_MSG_SOL         0x02
#define UBX_MSG_VELNED      0x04
#define UBX_MSG_TIMEUTC     0x08
#define UBX_MSG_ALL         (UBX_MSG_POSLLH | UBX_MSG_SOL | UBX_MSG_VELNED | UBX_MSG_TIMEUTC)

#define UBX_ALT_MIN         1500UL // Minimum announced altitude (m)

#define UBX_HAS_FIX         0x01
#define UBX_FIRST_FIX       0x02
#define UBX_SAY_ALTITUDE    0x04
#define UBX_VERTICAL_ACC    0x08

static const uint16_t UBX_sas_table[] PROGMEM =
{
	1024, 1077, 1135, 1197,
	1265, 1338, 1418, 1505,
	1600, 1704, 1818, 1944
};

static uint8_t  UBX_msg_class;
static uint8_t  UBX_msg_id;
static uint16_t UBX_payload_len;
static uint8_t  UBX_payload[UBX_MAX_PAYLOAD_LEN];

typedef struct
{
	uint8_t msgClass;  // Message class
	uint8_t msgID;     // Message identifier
	uint8_t rate;      // Send rate
}
UBX_cfg_msg;

typedef struct
{
	uint8_t  portID;       // Port identifier number
	uint8_t  reserved0;    // Reserved
	uint16_t txReady;      // TX ready pin configuration
	uint32_t mode;         // UART mode
	uint32_t baudRate;     // Baud rate (bits/sec)
	uint16_t inProtoMask;  // Input protocols
	uint16_t outProtoMask; // Output protocols
	uint16_t flags;        // Flags
	uint16_t reserved5;    // Always set to zero
}
UBX_cfg_prt;

typedef struct
{
	uint16_t measRate; // Measurement rate             (ms)
	uint16_t navRate;  // Nagivation rate, in number 
	                   //   of measurement cycles
	uint16_t timeRef;  // Alignment to reference time:
	                   //   0 = UTC time; 1 = GPS time
}
UBX_cfg_rate;

typedef struct
{
	uint16_t navBbrMask; // BBR sections to clear
	uint8_t  resetMode;  // Reset type
	uint8_t  res;        // Reserved
}
UBX_cfg_rst;

typedef struct
{
	uint16_t mask;             // Only masked parameters will be applied
	uint8_t  dynModel;         // Dynamic platform model
	uint8_t  fixMode;          // Position fixing mode
	int32_t  fixedAlt;         // Fixed altitude (MSL) for 2D mode       (m)
	uint32_t fixedAltVar;      // Fixed altitude variance for 2D mode    (m^2)
	int8_t   minElev;          // Minimum elevation for satellite        (deg)
	uint8_t  drLimit;          // Maximum time to perform dead reckoning (s)
	uint16_t pDop;             // Position DOP mask
	uint16_t tDop;             // Time DOP mask
	uint16_t pAcc;             // Position accuracy mask                 (m)
	uint16_t tAcc;             // Time accuracy mask                     (m)
	uint8_t  staticHoldThresh; // Static hold threshold                  (cm/s)
	uint8_t  res1;             // Reserved, set to 0
	uint32_t res2;             // Reserved, set to 0
	uint32_t res3;             // Reserved, set to 0
	uint32_t res4;             // Reserved, set to 0
}
UBX_cfg_nav5;

typedef struct
{
	uint32_t iTOW;     // GPS time of week             (ms)
	int32_t  lon;      // Longitude                    (deg)
	int32_t  lat;      // Latitude                     (deg)
	int32_t  height;   // Height above ellipsoid       (mm)
	int32_t  hMSL;     // Height above mean sea level  (mm)
	uint32_t hAcc;     // Horizontal accuracy estimate (mm)
	uint32_t vAcc;     // Vertical accuracy estimate   (mm)
}
UBX_nav_posllh;

typedef struct
{
	uint32_t iTOW;     // GPS time of week             (ms)
	uint8_t  gpsFix;   // GPS fix type
	uint8_t  flags;    // Navigation status flags
	uint8_t  diffStat; // Differential status
	uint8_t  res;      // Reserved
	uint32_t ttff;     // Time to first fix            (ms)
	uint32_t msss;     // Time since startup           (ms)
}
UBX_nav_status;

typedef struct
{
	uint32_t iTOW;     // GPS time of week             (ms)
	int32_t  fTOW;     // Fractional nanoseconds       (ns)
	int16_t  week;     // GPS week
	uint8_t  gpsFix;   // GPS fix type
	uint8_t  flags;    // Fix status flags
	int32_t  ecefX;    // ECEF X coordinate            (cm)
	int32_t  ecefY;    // ECEF Y coordinate            (cm)
	int32_t  ecefZ;    // ECEF Z coordinate            (cm)
	uint32_t pAcc;     // 3D position accuracy         (cm)
	int32_t  ecefVX;   // ECEF X velocity              (cm/s)
	int32_t  ecefVY;   // ECEF Y velocity              (cm/s)
	int32_t  ecefVZ;   // ECEF Z velocity              (cm/s)
	uint32_t sAcc;     // Speed accuracy               (cm/s)
	uint16_t pDOP;     // Position DOP
	uint8_t  res1;     // Reserved
	uint8_t  numSV;    // Number of SVs in solution
	uint32_t res2;     // Reserved
}
UBX_nav_sol;

typedef struct
{
	uint32_t iTOW;     // GPS time of week             (ms)
	int32_t  velN;     // North velocity               (cm/s)
	int32_t  velE;     // East velocity                (cm/s)
	int32_t  velD;     // Down velocity                (cm/s)
	uint32_t speed;    // 3D speed                     (cm/s)
	uint32_t gSpeed;   // Ground speed                 (cm/s)
	int32_t  heading;  // 2D heading                   (deg)
	uint32_t sAcc;     // Speed accuracy estimate      (cm/s)
	uint32_t cAcc;     // Heading accuracy estimate    (deg)
}
UBX_nav_velned;

typedef struct
{
	uint32_t iTOW;     // GPS time of week             (ms)
	uint32_t tAcc;     // Time accuracy estimate       (ns)
	int32_t  nano;     // Nanoseconds of second        (ns)
	uint16_t year;     // Year                         (1999..2099)
	uint8_t  month;    // Month                        (1..12)
	uint8_t  day;      // Day of month                 (1..31)
	uint8_t  hour;     // Hour of day                  (0..23)
	uint8_t  min;      // Minute of hour               (0..59)
	uint8_t  sec;      // Second of minute             (0..59)
	uint8_t  valid;    // Validity flags
}
UBX_nav_timeutc;

typedef struct
{
	uint8_t clsID;     // Class ID of acknowledged message
	uint8_t msgID;     // Message ID of acknowledged message
}
UBX_ack_ack;

typedef struct
{
	uint8_t clsID;     // Class ID of not-acknowledged message
	uint8_t msgID;     // Message ID of not-acknowledged message
}
UBX_ack_nak;

uint8_t  UBX_model         = 7;
uint16_t UBX_rate          = 200;
uint8_t  UBX_mode          = 2;
int32_t  UBX_min           = 0;
int32_t  UBX_max           = 300;

uint8_t  UBX_mode_2        = 9;
int32_t  UBX_min_2         = 300;
int32_t  UBX_max_2         = 1500;
int32_t  UBX_min_rate      = 100;
int32_t  UBX_max_rate      = 500;
uint8_t  UBX_flatline      = 0;
uint8_t  UBX_limits        = 1;
uint8_t  UBX_use_sas       = 1;

UBX_speech_t UBX_speech[UBX_MAX_SPEECH];
uint8_t      UBX_num_speech = 0;
uint8_t      UBX_cur_speech = 0;
uint16_t     UBX_sp_rate    = 0;

uint8_t  UBX_alt_units     = UBX_UNITS_FEET;
uint32_t UBX_alt_step      = 0;

uint8_t  UBX_init_mode     = 0;
char     UBX_init_filename[9];

static uint16_t UBX_sp_counter = 0;

uint32_t UBX_threshold     = 1000;
uint32_t UBX_hThreshold    = 0;

UBX_alarm_t UBX_alarms[UBX_MAX_ALARMS];
uint8_t     UBX_num_alarms   = 0;
uint32_t    UBX_alarm_window_above = 0;
uint32_t    UBX_alarm_window_below = 0;

static uint32_t UBX_time_of_week = 0;
static uint8_t  UBX_msg_received = 0;

UBX_buffer_t UBX_buffer;

UBX_window_t UBX_windows[UBX_MAX_WINDOWS];
uint8_t      UBX_num_windows = 0;

int32_t UBX_dz_elev = 0;

typedef struct
{
	int32_t  lon;      // Longitude                    (deg)
	int32_t  lat;      // Latitude                     (deg)
	int32_t  hMSL;     // Height above mean sea level  (mm)
	uint32_t hAcc;     // Horizontal accuracy estimate (mm)
	uint32_t vAcc;     // Vertical accuracy estimate   (mm)

	uint8_t  gpsFix;   // GPS fix type
	uint8_t  numSV;    // Number of SVs in solution

	int32_t  velN;     // North velocity               (cm/s)
	int32_t  velE;     // East velocity                (cm/s)
	int32_t  velD;     // Down velocity                (cm/s)
	uint32_t speed;    // 3D speed                     (cm/s)
	uint32_t gSpeed;   // Ground speed                 (cm/s)
	int32_t  heading;  // 2D heading                   (deg)
	uint32_t sAcc;     // Speed accuracy estimate      (cm/s)
	uint32_t cAcc;     // Heading accuracy estimate    (deg)

	int32_t  nano;     // Nanoseconds of second        (ns)
	uint16_t year;     // Year                         (1999..2099)
	uint8_t  month;    // Month                        (1..12)
	uint8_t  day;      // Day of month                 (1..31)
	uint8_t  hour;     // Hour of day                  (0..23)
	uint8_t  min;      // Minute of hour               (0..59)
	uint8_t  sec;      // Second of minute             (0..59)
}
UBX_saved_t ;
static UBX_saved_t UBX_saved[UBX_SAVED_LEN];

static uint8_t UBX_read  = 0;
static uint8_t UBX_write = 0;

static uint8_t UBX_flags = 0;
static uint8_t UBX_prev_flags = 0;

static int32_t UBX_prevHMSL;

static uint8_t UBX_suppress_tone = 0;

static char UBX_speech_buf[16] = "\0";
static char *UBX_speech_ptr = UBX_speech_buf;

#ifdef STACK_PAINTING
static const char UBX_header[] PROGMEM = 
	"time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,heading,cAcc,gpsFix,numSV,stack\r\n"
	",(deg),(deg),(m),(m/s),(m/s),(m/s),(m),(m),(m/s),(deg),(deg),,,\r\n";
#else
static const char UBX_header[] PROGMEM = 
	"time,lat,lon,hMSL,velN,velE,velD,hAcc,vAcc,sAcc,heading,cAcc,gpsFix,numSV\r\n"
	",(deg),(deg),(m),(m/s),(m/s),(m/s),(m),(m),(m/s),(deg),(deg),,\r\n";
#endif

static enum
{
	st_idle,
	st_flush_1,
	st_flush_2,
	st_flush_3
}
UBX_state = st_idle;

extern int disk_is_ready(void);

void UBX_Update(void)
{
	static uint16_t counter;

	static enum
	{
		st_solid,
		st_blinking
	}
	state = st_solid;

	switch (state)
	{
	case st_solid:
		if (UBX_flags & UBX_HAS_FIX)
		{
			counter = 0;
			state = st_blinking;
		}
		break;
	case st_blinking:
		if (!(UBX_flags & UBX_HAS_FIX))
		{
			LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);
			state = st_solid;
		}
		break;
	}
	
	if (state == st_blinking)
	{
		if (counter == 0)
		{
			LEDs_ChangeLEDs(LEDS_ALL_LEDS, 0);
		}
		else if (counter == 900)
		{
			LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);
		}

		counter = (counter + 1) % 1000;
	}
}

static uint8_t UBX_HandleByte(
	unsigned char ch)
{
	uint8_t ret = 0;

	static enum
	{
		st_sync_1, 
		st_sync_2, 
		st_class, 
		st_id,
		st_length_1,
		st_length_2,
		st_payload,
		st_ck_a,
		st_ck_b
	}
	state = st_sync_1;
	
	static uint8_t ck_a, ck_b;
	static uint16_t index;
	
	switch (state)
	{
	case st_sync_1:
		if (ch == UBX_SYNC_1)
		{
			state = st_sync_2;
		}
		break;
	case st_sync_2:
		if (ch == UBX_SYNC_2)
		{
			state = st_class;
		}
		else
		{
			state = st_sync_1;
		}
		break;
	case st_class:
		UBX_msg_class = ch;
		ck_a = ck_b = ch;
		state = st_id;
		break;
	case st_id:
		UBX_msg_id = ch;
		ck_a += ch;
		ck_b += ck_a;
		state = st_length_1;
		break;
	case st_length_1:
		UBX_payload_len = ch;
		ck_a += ch;
		ck_b += ck_a;
		state = st_length_2;
		break;
	case st_length_2:
		UBX_payload_len += ch << 8;
		ck_a += ch;
		ck_b += ck_a;
		if (UBX_payload_len == 0)
		{
			state = st_ck_a;
		}
		else if (UBX_payload_len <= UBX_MAX_PAYLOAD_LEN)
		{
			state = st_payload;
			index = 0;
		}
		else
		{
			state = st_sync_1;
		}
		break;
	case st_payload:
		UBX_payload[index++] = ch;
		ck_a += ch;
		ck_b += ck_a;
		if (index == UBX_payload_len)
		{
			state = st_ck_a;
		}
		break;
	case st_ck_a:
		if (ck_a == ch)
		{
			state = st_ck_b;
		}
		else
		{
			state = st_sync_1;
		}
		break;
	case st_ck_b:
		if (ck_b == ch)
		{
			ret = 1;
		}
		state = st_sync_1;
		break;
	}

	return ret;
}

static uint8_t UBX_WaitForAck(
	uint8_t  msg_class,
	uint8_t  msg_id,
	uint16_t timeout)
{
	unsigned int ch;

	Timer_Set(timeout);
	
	while (Timer_Get() != 0)
	{
		if ((ch = uart_getc()) != UART_NO_DATA)
		{
			if (UBX_HandleByte(ch))
			{
				if (UBX_msg_class == UBX_ACK &&
				    UBX_msg_id == UBX_ACK_ACK)
				{
					UBX_ack_ack ack = *((UBX_ack_ack *) UBX_payload);
					if (ack.clsID == msg_class &&
					    ack.msgID == msg_id)
					{
						return 1; // ACK
					}
				}
				else if (UBX_msg_class == UBX_ACK &&
				         UBX_msg_id == UBX_ACK_NAK)
				{
					UBX_ack_nak nak = *((UBX_ack_nak *) UBX_payload);
					if (nak.clsID == msg_class &&
					    nak.msgID == msg_id)
					{
						return 0; // NAK
					}
				}
			}
		}
	}

	return 0;
}

static void UBX_SendMessage(
	uint8_t  msg_class,
	uint8_t  msg_id,
	uint16_t size,
	void     *data)
{
	uint16_t i;
	uint8_t  *bytes = (uint8_t *) data;
	uint8_t  ck_a = 0, ck_b = 0;

	uart_putc(UBX_SYNC_1);
	uart_putc(UBX_SYNC_2);

	#define SEND_BYTE(a) uart_putc(a); ck_a += a; ck_b += ck_a;

	SEND_BYTE(msg_class);
	SEND_BYTE(msg_id);

	SEND_BYTE(size & 0xff);
	SEND_BYTE((size >> 8) & 0xff);

	for (i = 0; i < size; ++i)
	{
		SEND_BYTE(bytes[i]);
	}

	#undef SEND_BYTE
	
	uart_putc(ck_a);
	uart_putc(ck_b);
}

static void UBX_SetTone(
	int32_t val_1,
	int32_t min_1,
	int32_t max_1,
	int32_t val_2,
	int32_t min_2,
	int32_t max_2)
{
	#define UNDER(val,min,max) ((min < max) ? (val <= min) : (val >= min))
	#define OVER(val,min,max)  ((min < max) ? (val >= max) : (val <= max))

	if (val_1 != UBX_INVALID_VALUE &&
	    val_2 != UBX_INVALID_VALUE)
	{
		if (UNDER(val_2, min_2, max_2))
		{
			if (UBX_flatline)
			{
				Tone_SetRate(TONE_RATE_FLATLINE);
			}
			else
			{
				Tone_SetRate(UBX_min_rate);
			}
		}
		else if (OVER(val_2, min_2, max_2))
		{
			Tone_SetRate(UBX_max_rate - 1);
		}
		else
		{
			Tone_SetRate(UBX_min_rate + (UBX_max_rate - UBX_min_rate) * (val_2 - min_2) / (max_2 - min_2));
		}

		if (UNDER(val_1, min_1, max_1))
		{
			if (UBX_limits == 0)
			{
				Tone_SetRate(0);
			}
			else if (UBX_limits == 1)
			{
				Tone_SetPitch(0);
				Tone_SetChirp(0);
			}
			else if (UBX_limits == 2)
			{
				Tone_SetPitch(0);
				Tone_SetChirp(TONE_CHIRP_MAX);
			}
			else
			{
				Tone_SetPitch(TONE_MAX_PITCH - 1);
				Tone_SetChirp(-TONE_CHIRP_MAX);
			}
		}
		else if (OVER(val_1, min_1, max_1))
		{
			if (UBX_limits == 0)
			{
				Tone_SetRate(0);
			}
			else if (UBX_limits == 1)
			{
				Tone_SetPitch(TONE_MAX_PITCH - 1);
				Tone_SetChirp(0);
			}
			else if (UBX_limits == 2)
			{
				Tone_SetPitch(TONE_MAX_PITCH - 1);
				Tone_SetChirp(-TONE_CHIRP_MAX);
			}
			else
			{
				Tone_SetPitch(0);
				Tone_SetChirp(TONE_CHIRP_MAX);
			}
		}
		else
		{
			Tone_SetPitch(TONE_MAX_PITCH * (val_1 - min_1) / (max_1 - min_1));
			Tone_SetChirp(0);
		}
	}
	else
	{
		Tone_SetRate(0);
	}
		
	#undef OVER
	#undef UNDER
}

static void UBX_GetValues(
	UBX_saved_t *current,
	uint8_t mode, 
	int32_t *val, 
	int32_t *min, 
	int32_t *max)
{
	uint16_t speed_mul = 1024;

	if (UBX_use_sas)
	{
		if (current->hMSL < 0)
		{
			speed_mul = pgm_read_word(&UBX_sas_table[0]);
		}
		else if (current->hMSL >= 11534336L)
		{
			speed_mul = pgm_read_word(&UBX_sas_table[11]);
		}
		else
		{
			int32_t h = current->hMSL / 1024	;
			uint16_t i = h / 1024;
			uint16_t j = h % 1024;
			uint16_t y1 = pgm_read_word(&UBX_sas_table[i]);
			uint16_t y2 = pgm_read_word(&UBX_sas_table[i + 1]);
			speed_mul = y1 + ((y2 - y1) * j) / 1024;
		}
	}

	switch (mode)
	{
	case 0: // Horizontal speed
		*val = (current->gSpeed * 1024) / speed_mul;
		break;
	case 1: // Vertical speed
		*val = (current->velD * 1024) / speed_mul;
		break;
	case 2: // Glide ratio
		if (current->velD != 0)
		{
			*val = 10000 * (int32_t) current->gSpeed / current->velD;
			*min *= 100;
			*max *= 100;
		}
		break;
	case 3: // Inverse glide ratio
		if (current->gSpeed != 0)
		{
			*val = 10000 * current->velD / (int32_t) current->gSpeed;
			*min *= 100;
			*max *= 100;
		}
		break;
	case 4: // Total speed
		*val = (current->speed * 1024) / speed_mul;
		break;
	case 11: // Dive angle
		*val = atan2(current->velD, current->gSpeed) / M_PI * 180;
		break;
	}
}

static void UBX_SpeakValue(
	UBX_saved_t *current)
{
	uint16_t speed_mul = 1024;
	
	char *end_ptr;

	if (UBX_use_sas)
	{
		if (current->hMSL < 0)
		{
			speed_mul = pgm_read_word(&UBX_sas_table[0]);
		}
		else if (current->hMSL >= 11534336L)
		{
			speed_mul = pgm_read_word(&UBX_sas_table[11]);
		}
		else
		{
			int32_t h = current->hMSL / 1024	;
			uint16_t i = h / 1024;
			uint16_t j = h % 1024;
			uint16_t y1 = pgm_read_word(&UBX_sas_table[i]);
			uint16_t y2 = pgm_read_word(&UBX_sas_table[i + 1]);
			speed_mul = y1 + ((y2 - y1) * j) / 1024;
		}
	}

	switch (UBX_speech[UBX_cur_speech].units)
	{
	case UBX_UNITS_KMH:
		speed_mul = (uint16_t) (((uint32_t) speed_mul * 18204) / 65536);
		break;
	case UBX_UNITS_MPH:
		speed_mul = (uint16_t) (((uint32_t) speed_mul * 29297) / 65536);
		break;
	}

	// Step 0: Initialize speech pointers, leaving room at the end for one unit character
	
	UBX_speech_ptr = UBX_speech_buf + sizeof(UBX_speech_buf) - 1;
	end_ptr = UBX_speech_ptr;

	// Step 1: Get speech value with 2 decimal places
	
	switch (UBX_speech[UBX_cur_speech].mode)
	{
	case 0: // Horizontal speed
		UBX_speech_ptr = Log_WriteInt32ToBuf(UBX_speech_ptr, (current->gSpeed * 1024) / speed_mul, 2, 1, 0);
		break;
	case 1: // Vertical speed
		UBX_speech_ptr = Log_WriteInt32ToBuf(UBX_speech_ptr, (current->velD * 1024) / speed_mul, 2, 1, 0);
		break;
	case 2: // Glide ratio
		if (current->velD != 0)
		{
			UBX_speech_ptr = Log_WriteInt32ToBuf(UBX_speech_ptr, 100 * (int32_t) current->gSpeed / current->velD, 2, 1, 0);
		}
		break;
	case 3: // Inverse glide ratio
		if (current->gSpeed != 0)
		{
			UBX_speech_ptr = Log_WriteInt32ToBuf(UBX_speech_ptr, 100 * (int32_t) current->velD / current->gSpeed, 2, 1, 0);
		}
		else
		{
			*(--UBX_speech_ptr) = 0;
		}
		break;
	case 4: // Total speed
		UBX_speech_ptr = Log_WriteInt32ToBuf(UBX_speech_ptr, (current->speed * 1024) / speed_mul, 2, 1, 0);
		break;
	case 11: // Dive angle
		UBX_speech_ptr = Log_WriteInt32ToBuf(UBX_speech_ptr, 100 * atan2(current->velD, current->gSpeed) / M_PI * 180, 2, 1, 0);
		break;
	}

	// Step 1.5: Include label
	if (UBX_num_speech > 1)
	{
		*(--UBX_speech_ptr) = UBX_speech[UBX_cur_speech].mode + 1;
		*(--UBX_speech_ptr) = '>';
	}
	
	// Step 2: Truncate to the desired number of decimal places

	if (UBX_speech[UBX_cur_speech].decimals == 0) end_ptr -= 4;
	else end_ptr -= 3 - UBX_speech[UBX_cur_speech].decimals;
	
	// Step 3: Add units if needed, e.g., *(end_ptr++) = 'k';
	
	switch (UBX_speech[UBX_cur_speech].mode)
	{
	case 0: // Horizontal speed
	case 1: // Vertical speed
	case 2: // Glide ratio
	case 3: // Inverse glide ratio
	case 4: // Total speed
		break;
	}
	
	// Step 4: Terminate with a null

	*(end_ptr++) = 0;
}

static char *UBX_NumberToSpeech(
	uint32_t number,
	char *ptr)
{
	// Adapted from https://stackoverflow.com/questions/2729752/converting-numbers-in-to-words-c-sharp

    if (number == 0)
	{
		*(ptr++) = '0';
		return ptr;
	}

    if (number < 0)
	{
		*(ptr++) = '-';
        return UBX_NumberToSpeech(-number, ptr);
	}

    if ((number / 1000) > 0)
    {
        ptr = UBX_NumberToSpeech(number / 1000, ptr);
		*(ptr++) = 'k';
        number %= 1000;
    }

    if ((number / 100) > 0)
    {
        ptr = UBX_NumberToSpeech(number / 100, ptr);
		*(ptr++) = 'h';
        number %= 100;
    }

    if (number > 0)
    {
		if (number < 10)
		{
			*(ptr++) = '0' + number;
		}
		else if (number < 20)
		{
			*(ptr++) = 't';
			*(ptr++) = '0' + (number - 10);
		}
        else
        {
			*(ptr++) = 'x';
			*(ptr++) = '0' + (number / 10);

            if ((number % 10) > 0)
				*(ptr++) = '0' + (number % 10);
        }
    }

    return ptr;
}

static void UBX_UpdateAlarms(
	UBX_saved_t *current)
{
	uint8_t i, suppress_tone, suppress_alt;
	int32_t step_size, step, step_elev;

	suppress_tone = 0;
	suppress_alt = 0;

	for (i = 0; i < UBX_num_alarms; ++i)
	{
		const int32_t alarm_elev = UBX_alarms[i].elev + UBX_dz_elev;

		if ((current->hMSL <= alarm_elev + UBX_alarm_window_above) &&
		    (current->hMSL >= alarm_elev - UBX_alarm_window_below))
		{
			suppress_tone = 1;
			break;
		}
	}
	
	for (i = 0; i < UBX_num_windows; ++i)
	{
		if ((UBX_windows[i].bottom + UBX_dz_elev <= current->hMSL) &&
		    (UBX_windows[i].top + UBX_dz_elev >= current->hMSL))
		{
			suppress_tone = 1;
			suppress_alt = 1;
			break;
		}
	}
	
	if (UBX_alt_step > 0)
	{
		if (UBX_alt_units == UBX_UNITS_METERS)
		{
			step_size = 10000 * UBX_alt_step;
		}
		else
		{
			step_size = 3048 * UBX_alt_step;
		}

		step = ((current->hMSL - UBX_dz_elev) * 10 + step_size / 2) / step_size;
		step_elev = step * step_size / 10 + UBX_dz_elev;

		if ((current->hMSL <= step_elev + UBX_alarm_window_above) &&
		    (current->hMSL >= step_elev - UBX_alarm_window_below) &&
		    (current->hMSL - UBX_dz_elev >= UBX_ALT_MIN * 1000))
		{
			suppress_tone = 1;
		}
	}
	
	if (suppress_tone && !UBX_suppress_tone)
	{
		*UBX_speech_ptr = 0;
		Tone_SetRate(0);
		Tone_Stop();
	}
	
	UBX_suppress_tone = suppress_tone;

	if (UBX_prev_flags & UBX_HAS_FIX)
	{
		int32_t min = MIN(UBX_prevHMSL, current->hMSL);
		int32_t max = MAX(UBX_prevHMSL, current->hMSL);
		
		for (i = 0; i < UBX_num_alarms; ++i)
		{
			const int32_t alarm_elev = UBX_alarms[i].elev + UBX_dz_elev;

			if (alarm_elev >= min && alarm_elev < max)
			{
				switch (UBX_alarms[i].type)
				{
				case 1:	// beep
					Tone_Beep(TONE_MAX_PITCH - 1, 0, TONE_LENGTH_125_MS);
					break ;
				case 2:	// chirp up
					Tone_Beep(0, TONE_CHIRP_MAX, TONE_LENGTH_125_MS);
					break ;
				case 3:	// chirp down
					Tone_Beep(TONE_MAX_PITCH - 1, -TONE_CHIRP_MAX, TONE_LENGTH_125_MS);
					break ;
				case 4:	// play file
					strcpy(UBX_buffer.filename, UBX_alarms[i].filename);
					strcat(UBX_buffer.filename, ".wav");
					Tone_Play(UBX_buffer.filename);
					break;
				}
				
				break;
			}
		}

		if ((UBX_alt_step > 0) &&
		    (i == UBX_num_alarms) &&
		    (UBX_prevHMSL - UBX_dz_elev >= UBX_ALT_MIN * 1000) &&
		    (*UBX_speech_ptr == 0) &&
		    !(UBX_flags & UBX_SAY_ALTITUDE) &&
		    !suppress_alt)
		{
			if ((step_elev >= min && step_elev < max) &&
			    ABS(current->velD) >= UBX_threshold &&
			    current->gSpeed >= UBX_hThreshold)
			{
				UBX_speech_ptr = UBX_speech_buf;
				UBX_speech_ptr = UBX_NumberToSpeech(step * UBX_alt_step, UBX_speech_ptr);
				*(UBX_speech_ptr++) = (UBX_alt_units == UBX_UNITS_METERS) ? 'm' : 'f';
				*(UBX_speech_ptr++) = 0;
				UBX_speech_ptr = UBX_speech_buf;
			}
		}
	}
}

static void UBX_UpdateTones(
	UBX_saved_t *current)
{
	static int32_t x0 = UBX_INVALID_VALUE, x1, x2;
	
	int32_t val_1 = UBX_INVALID_VALUE, min_1 = UBX_min, max_1 = UBX_max;
	int32_t val_2 = UBX_INVALID_VALUE, min_2 = UBX_min_2, max_2 = UBX_max_2;

	UBX_GetValues(current, UBX_mode, &val_1, &min_1, &max_1);

	if (UBX_mode_2 == 8)
	{
		UBX_GetValues(current, UBX_mode, &val_2, &min_2, &max_2);
		if (val_2 != UBX_INVALID_VALUE)
		{
			val_2 = ABS(val_2);
		}
	}
	else if (UBX_mode_2 == 9)
	{
		x2 = x1;
		x1 = x0;
		x0 = val_1;

		if (x0 != UBX_INVALID_VALUE && 
			x1 != UBX_INVALID_VALUE && 
			x2 != UBX_INVALID_VALUE)
		{
			val_2 = (int32_t) 1000 * (x2 - x0) / (2 * UBX_rate);
			val_2 = (int32_t) 10000 * ABS(val_2) / ABS(max_1 - min_1);
		}
	}
	else
	{
		UBX_GetValues(current, UBX_mode_2, &val_2, &min_2, &max_2);
	}

	if (!UBX_suppress_tone)
	{
		if (ABS(current->velD) >= UBX_threshold && 
			current->gSpeed >= UBX_hThreshold)
		{
			UBX_SetTone(val_1, min_1, max_1, val_2, min_2, max_2);
				
			if (UBX_sp_rate != 0 && 
			    UBX_num_speech != 0 && 
			    UBX_sp_counter >= UBX_sp_rate)
			{
				UBX_SpeakValue(current);
				UBX_cur_speech = (UBX_cur_speech + 1) % UBX_num_speech;
				UBX_sp_counter = 0;
			}
		}
		else
		{
			Tone_SetRate(0);
		}
	}

	if (UBX_sp_counter < UBX_sp_rate)
	{
		UBX_sp_counter += UBX_rate;
	}
}

static void UBX_ReceiveMessage(
	uint8_t msg_received, 
	uint32_t time_of_week)
{
	UBX_saved_t *current = UBX_saved + (UBX_write % UBX_SAVED_LEN);

	if (time_of_week != UBX_time_of_week)
	{
		UBX_time_of_week = time_of_week;
		UBX_msg_received = 0;
	}

	UBX_msg_received |= msg_received;

	if (UBX_msg_received == UBX_MSG_ALL)
	{
		if (current->gpsFix == 0x03)
		{
			UBX_flags |= UBX_HAS_FIX;

			UBX_UpdateAlarms(current);
			UBX_UpdateTones(current);

			if (!Log_IsInitialized())
			{
				Power_Hold();

				Log_Init(
					current->year,
					current->month,
					current->day,
					current->hour,
					current->min,
					current->sec);

				Log_WriteString(UBX_header);
				UBX_state = st_flush_1;

				UBX_flags |= UBX_FIRST_FIX;
				UBX_flags |= UBX_SAY_ALTITUDE;
			}

			++UBX_write;
		}
		else
		{
			UBX_flags &= ~UBX_HAS_FIX;
			Tone_SetRate(0);
		}

		if (current->vAcc < 10000)
		{
			UBX_flags |= UBX_VERTICAL_ACC;
		}
		else
		{
			UBX_flags &= ~UBX_VERTICAL_ACC;
		}

		UBX_prev_flags = UBX_flags;
		UBX_prevHMSL = current->hMSL;

		UBX_msg_received = 0;
	}
}

static void UBX_HandleNavSol(void)
{
	UBX_saved_t *current = UBX_saved + (UBX_write % UBX_SAVED_LEN);
	UBX_nav_sol *nav_sol = (UBX_nav_sol *) UBX_payload;

	current->gpsFix = nav_sol->gpsFix;
	current->numSV  = nav_sol->numSV;

	UBX_ReceiveMessage(UBX_MSG_SOL, nav_sol->iTOW);
}

static void UBX_HandlePosition(void)
{
	UBX_saved_t *current = UBX_saved + (UBX_write % UBX_SAVED_LEN);
	UBX_nav_posllh *nav_pos_llh = (UBX_nav_posllh *) UBX_payload;

	current->lon  = nav_pos_llh->lon;
	current->lat  = nav_pos_llh->lat;
	current->hMSL = nav_pos_llh->hMSL;
	current->hAcc = nav_pos_llh->hAcc;
	current->vAcc = nav_pos_llh->vAcc;

	UBX_ReceiveMessage(UBX_MSG_POSLLH, nav_pos_llh->iTOW);
}

static void UBX_HandleVelocity(void)
{
	UBX_saved_t *current = UBX_saved + (UBX_write % UBX_SAVED_LEN);
	UBX_nav_velned *nav_velned = (UBX_nav_velned *) UBX_payload;

	current->velN    = nav_velned->velN;
	current->velE    = nav_velned->velE;
	current->velD    = nav_velned->velD;
	current->speed   = nav_velned->speed;
	current->gSpeed  = nav_velned->gSpeed;
	current->heading = nav_velned->heading;
	current->sAcc    = nav_velned->sAcc;
	current->cAcc    = nav_velned->cAcc;

	UBX_ReceiveMessage(UBX_MSG_VELNED, nav_velned->iTOW);
}

static void UBX_HandleTimeUTC(void)
{
	UBX_saved_t *current = UBX_saved + (UBX_write % UBX_SAVED_LEN);
	UBX_nav_timeutc *nav_timeutc = (UBX_nav_timeutc *) UBX_payload;

	current->nano  = nav_timeutc->nano;
	current->year  = nav_timeutc->year;
	current->month = nav_timeutc->month;
	current->day   = nav_timeutc->day;
	current->hour  = nav_timeutc->hour;
	current->min   = nav_timeutc->min;
	current->sec   = nav_timeutc->sec;

	UBX_ReceiveMessage(UBX_MSG_TIMEUTC, nav_timeutc->iTOW);
}

static void UBX_HandleMessage(void)
{
	if ((uint8_t) (UBX_read + UBX_SAVED_LEN) == UBX_write)
	{
		++UBX_read;
	}

	switch (UBX_msg_class)
	{
	case UBX_NAV:
		switch (UBX_msg_id)
		{
		case UBX_NAV_SOL:
			UBX_HandleNavSol();
			break;
		case UBX_NAV_POSLLH:
			UBX_HandlePosition();
			break;
		case UBX_NAV_VELNED:
			UBX_HandleVelocity();
			break;
		case UBX_NAV_TIMEUTC:
			UBX_HandleTimeUTC();
			break;
		}
		break;
	}
}

void UBX_Init(void)
{
	UBX_cfg_msg cfg_msg[] =
	{
		{UBX_NMEA, UBX_NMEA_GPGGA,  0},
		{UBX_NMEA, UBX_NMEA_GPGLL,  0},
		{UBX_NMEA, UBX_NMEA_GPGSA,  0},
		{UBX_NMEA, UBX_NMEA_GPGSV,  0},
		{UBX_NMEA, UBX_NMEA_GPRMC,  0},
		{UBX_NMEA, UBX_NMEA_GPVTG,  0},
		{UBX_NAV,  UBX_NAV_POSLLH,  1},
		{UBX_NAV,  UBX_NAV_VELNED,  1},
		{UBX_NAV,  UBX_NAV_SOL,     1},
		{UBX_NAV,  UBX_NAV_TIMEUTC, 1}
	};

	size_t n = sizeof(cfg_msg) / sizeof(UBX_cfg_msg);
	size_t i;

	UBX_cfg_rate cfg_rate =
	{
		.measRate   = UBX_rate, // Measurement rate (ms)
		.navRate    = 1,        // Navigation rate (cycles)
		.timeRef    = 0         // UTC time
	};

	UBX_cfg_rst cfg_rst =
	{
		.navBbrMask = 0x0000,   // Hot start
		.resetMode  = 0x09      // Controlled GPS start
	};
	
	UBX_cfg_nav5 cfg_nav5 =
	{
		.mask       = 0x0001,   // Apply dynamic model settings
		.dynModel   = UBX_model // Airborne with < 1 g acceleration
	};
	
	UBX_cfg_prt cfg_prt =
	{
		.portID       = 1,      // UART 1
		.reserved0    = 0,      // Reserved
		.txReady      = 0,      // no TX ready
		.mode         = 0x08d0, // 8N1
		.baudRate     = 38400,  // Baudrate in bits/second
		.inProtoMask  = 0x0001, // UBX protocol
		.outProtoMask = 0x0001, // UBX protocol
		.flags        = 0,      // Flags bit mask
		.reserved5    = 0       // Reserved, set to 0
	};

	do
	{
		uart_init(51); // 9600 baud

		UBX_SendMessage(UBX_CFG, UBX_CFG_PRT, sizeof(cfg_prt), &cfg_prt);

		// NOTE: We don't wait for ACK here since some FlySights will already be
		//       set to 38400 baud.

		while (!uart_tx_empty());

		uart_init(12); // 38400 baud

		_delay_ms(10); // wait for GPS UART to reset

		UBX_SendMessage(UBX_CFG, UBX_CFG_PRT, sizeof(cfg_prt), &cfg_prt);
	}
	while (!UBX_WaitForAck(UBX_CFG, UBX_CFG_PRT, UBX_TIMEOUT));

	#define SEND_MESSAGE(c,m,d) \
		do { \
			UBX_SendMessage(c,m,sizeof(d),&d); \
		} while (!UBX_WaitForAck(c,m,UBX_TIMEOUT));

	for (i = 0; i < n; ++i)
	{
		SEND_MESSAGE(UBX_CFG, UBX_CFG_MSG, cfg_msg[i]);
	}
	
	SEND_MESSAGE(UBX_CFG, UBX_CFG_RATE, cfg_rate);
	SEND_MESSAGE(UBX_CFG, UBX_CFG_NAV5, cfg_nav5);
	
	#undef SEND_MESSAGE

	UBX_SendMessage(UBX_CFG, UBX_CFG_RST, sizeof(cfg_rst), &cfg_rst);
}

void UBX_Task(void)
{
#ifdef STACK_PAINTING
	static int32_t stack_count = 4096;
	int32_t temp;
#endif
	
	unsigned int ch;

	UBX_saved_t *current;
	char *ptr;

	while (!((ch = uart_getc()) & UART_NO_DATA))
	{
		if (UBX_HandleByte(ch))
		{
			UBX_HandleMessage();
		}
	}
	
	switch (UBX_state)
	{
	case st_idle:
		if (Tone_CanWrite() && disk_is_ready() && UBX_read != UBX_write)
		{
			current = UBX_saved + (UBX_read % UBX_SAVED_LEN);

			Power_Hold();

			ptr = UBX_buffer.buffer + sizeof(UBX_buffer.buffer);
			*(--ptr) = 0;

			*(--ptr) = '\n';
#ifdef STACK_PAINTING
			temp = Stack_Count();
			if (temp < stack_count)
			{
				stack_count = temp;
			}

			ptr = Log_WriteInt32ToBuf(ptr, stack_count,      0, 0, '\r');
			ptr = Log_WriteInt32ToBuf(ptr, current->numSV,   0, 0, ',');
#else
			ptr = Log_WriteInt32ToBuf(ptr, current->numSV,   0, 0, '\r');
#endif
			ptr = Log_WriteInt32ToBuf(ptr, current->gpsFix,  0, 0, ',');
			ptr = Log_WriteInt32ToBuf(ptr, current->cAcc,    5, 1, ',');
			ptr = Log_WriteInt32ToBuf(ptr, current->heading, 5, 1, ',');
			ptr = Log_WriteInt32ToBuf(ptr, current->sAcc,    2, 1, ',');
			ptr = Log_WriteInt32ToBuf(ptr, current->vAcc,    3, 1, ',');
			ptr = Log_WriteInt32ToBuf(ptr, current->hAcc,    3, 1, ',');
			ptr = Log_WriteInt32ToBuf(ptr, current->velD,    2, 1, ',');
			ptr = Log_WriteInt32ToBuf(ptr, current->velE,    2, 1, ',');
			ptr = Log_WriteInt32ToBuf(ptr, current->velN,    2, 1, ',');
			ptr = Log_WriteInt32ToBuf(ptr, current->hMSL,    3, 1, ',');
			ptr = Log_WriteInt32ToBuf(ptr, current->lon,     7, 1, ',');
			ptr = Log_WriteInt32ToBuf(ptr, current->lat,     7, 1, ',');
			*(--ptr) = ',';
			ptr = Log_WriteInt32ToBuf(ptr, (current->nano + 5000000) / 10000000, 2, 0, 'Z');
			ptr = Log_WriteInt32ToBuf(ptr, current->sec,     2, 0, '.');
			ptr = Log_WriteInt32ToBuf(ptr, current->min,     2, 0, ':');
			ptr = Log_WriteInt32ToBuf(ptr, current->hour,    2, 0, ':');
			ptr = Log_WriteInt32ToBuf(ptr, current->day,     2, 0, 'T');
			ptr = Log_WriteInt32ToBuf(ptr, current->month,   2, 0, '-');
			ptr = Log_WriteInt32ToBuf(ptr, current->year,    4, 0, '-');
			++UBX_read;

			f_puts(ptr, &Main_file);
			UBX_state = st_flush_1;
		}
		break;
	case st_flush_1:
		if (Tone_CanWrite() && disk_is_ready())
		{
			f_sync_1(&Main_file);
			UBX_state = st_flush_2;
		}
		break;
	case st_flush_2:
		if (Tone_CanWrite() && disk_is_ready())
		{
			f_sync_2(&Main_file);
			UBX_state = st_flush_3;
		}
		break;
	case st_flush_3:
		if (Tone_CanWrite() && disk_is_ready())
		{
			f_sync_3(&Main_file);
			Power_Release();
			UBX_state = st_idle;
		}
		break;
	}

	if (*UBX_speech_ptr)
	{
		if (Tone_IsIdle() && disk_is_ready())
		{
			Tone_Hold();
		
			if (*UBX_speech_ptr == '-')
			{
				Tone_Play("minus.wav");
			}
			else if (*UBX_speech_ptr == '.')
			{
				Tone_Play("dot.wav");
			}
			else if (*UBX_speech_ptr == 'h')
			{
				Tone_Play("00.wav");
			}
			else if (*UBX_speech_ptr == 'k')
			{
				Tone_Play("000.wav");
			}
			else if (*UBX_speech_ptr == 'm')
			{
				Tone_Play("meters.wav");
			}
			else if (*UBX_speech_ptr == 'f')
			{
				Tone_Play("feet.wav");
			}
			else if (*UBX_speech_ptr == 't')
			{
				++UBX_speech_ptr;
				UBX_buffer.filename[1] = *UBX_speech_ptr;
				UBX_buffer.filename[0] = '1';
				UBX_buffer.filename[2] = '.';
				UBX_buffer.filename[3] = 'w';
				UBX_buffer.filename[4] = 'a';
				UBX_buffer.filename[5] = 'v';
				UBX_buffer.filename[6] = 0;

				Tone_Play(UBX_buffer.filename);
			}
			else if (*UBX_speech_ptr == 'x')
			{
				++UBX_speech_ptr;
				UBX_buffer.filename[0] = *UBX_speech_ptr;
				UBX_buffer.filename[1] = '0';
				UBX_buffer.filename[2] = '.';
				UBX_buffer.filename[3] = 'w';
				UBX_buffer.filename[4] = 'a';
				UBX_buffer.filename[5] = 'v';
				UBX_buffer.filename[6] = 0;

				Tone_Play(UBX_buffer.filename);
			}
			else if (*UBX_speech_ptr == '>')
			{
				++UBX_speech_ptr;
				switch ((*UBX_speech_ptr) - 1)
				{
					case 0:
						Tone_Play("horz.wav");
						break;
					case 1:
						Tone_Play("vert.wav");
						break;
					case 2:
						Tone_Play("glide.wav");
						break;
					case 3:
						Tone_Play("iglide.wav");
						break;
					case 4:
						Tone_Play("speed.wav");
						break;
					case 11:
						Tone_Play("dive.wav");
						break;
				}
			}
			else
			{
				UBX_buffer.filename[0] = *UBX_speech_ptr;
				UBX_buffer.filename[1] = '.';
				UBX_buffer.filename[2] = 'w';
				UBX_buffer.filename[3] = 'a';
				UBX_buffer.filename[4] = 'v';
				UBX_buffer.filename[5] = 0;
				
				Tone_Play(UBX_buffer.filename);
			}
			
			++UBX_speech_ptr;
		}
	}
	else
	{
		Tone_Release();

		if ((UBX_flags & UBX_FIRST_FIX) && Tone_IsIdle())
		{
			UBX_flags &= ~UBX_FIRST_FIX;
			Tone_Beep(TONE_MAX_PITCH - 1, 0, TONE_LENGTH_125_MS);
		}

		if ((UBX_alt_step > 0) && 
		    (UBX_flags & UBX_SAY_ALTITUDE) && 
		    (UBX_flags & UBX_VERTICAL_ACC) && 
			Tone_IsIdle())
		{
			UBX_flags &= ~UBX_SAY_ALTITUDE;
			UBX_speech_ptr = UBX_speech_buf;

			if (UBX_alt_units == UBX_UNITS_METERS)
			{
				UBX_speech_ptr = UBX_NumberToSpeech((UBX_prevHMSL - UBX_dz_elev) / 1000, UBX_speech_ptr);
				*(UBX_speech_ptr++) = 'm';
			}
			else
			{
				UBX_speech_ptr = UBX_NumberToSpeech((UBX_prevHMSL - UBX_dz_elev) * 10 / 3048, UBX_speech_ptr);
				*(UBX_speech_ptr++) = 'f';
			}

			*(UBX_speech_ptr++) = 0;
			UBX_speech_ptr = UBX_speech_buf;
		}
	}
}
