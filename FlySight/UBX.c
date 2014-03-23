#include <stddef.h>
#include <stdio.h>

#include <util/delay.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/pgmspace.h>

#include "Board/LEDs.h"
#include "Log.h"
#include "Main.h"
#include "Power.h"
#include "Timer.h"
#include "Tone.h"
#include "uart.h"
#include "UBX.h"

#define ABS(a)   ((a) < 0     ? -(a) : (a))
#define MIN(a,b) (((a) < (b)) ?  (a) : (b))
#define MAX(a,b) (((a) > (b)) ?  (a) : (b))

#define UBX_INVALID_VALUE   INT32_MAX

#define UBX_TIMEOUT         500 // ACK/NAK timeout (ms)
#define UBX_MAX_PAYLOAD_LEN 64

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

#define UBX_WRITE_IDLE      0
#define UBX_WRITE_START     1

#define UBX_UNITS_KMH       0
#define UBX_UNITS_MPH       1

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

uint8_t  UBX_model         = 6;
uint16_t UBX_rate          = 200;
uint8_t  UBX_mode          = 2;
int32_t  UBX_min           = 0;
int32_t  UBX_max           = 300;

uint8_t  UBX_mode_2        = 9;
int32_t  UBX_min_2         = 300;
int32_t  UBX_max_2         = 1500;
uint32_t UBX_min_rate      = 100;
uint32_t UBX_max_rate      = 500;
uint8_t  UBX_flatline      = 0;
uint8_t  UBX_limits        = 1;
uint8_t  UBX_use_sas       = 1;

uint8_t  UBX_sp_mode       = 2;
uint8_t  UBX_sp_units      = UBX_UNITS_MPH;
uint16_t UBX_sp_rate       = 0;
uint8_t  UBX_sp_decimals   = 0;

static uint16_t UBX_sp_counter = 0;

uint32_t UBX_threshold     = 1000;
uint32_t UBX_hThreshold    = 0;

UBX_alarm UBX_alarms[UBX_MAX_ALARMS];
uint8_t   UBX_num_alarms   = 0;
uint32_t  UBX_alarm_window = 0;

static UBX_nav_posllh  UBX_nav_pos_llh_saved;
static UBX_nav_sol     UBX_nav_sol_saved;
static UBX_nav_velned  UBX_nav_velned_saved;
static UBX_nav_timeutc UBX_nav_timeutc_saved;

static volatile uint8_t UBX_hasFix        = 0;
static volatile uint8_t UBX_prevFix       = 0;
static          uint8_t UBX_suppress_tone = 0;

static int8_t   UBX_write_state = UBX_WRITE_IDLE;

static char UBX_speech_buf[16] = "\0";
static char *UBX_speech_ptr = UBX_speech_buf;

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
		if (UBX_hasFix)
		{
			counter = 0;
			state = st_blinking;
		}
		break;
	case st_blinking:
		if (!UBX_hasFix)
		{
			LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);
			state = st_solid;
		}
		break;
	}
	
	if (state == st_blinking)
	{
		if (counter == 100)
		{
			LEDs_ChangeLEDs(LEDS_ALL_LEDS, 0);
		}
		else if (counter == 1000)
		{
			LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);
			counter = 0;
		}

		++counter;
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
			else
			{
				Tone_SetPitch(0);
				Tone_SetChirp(TONE_CHIRP_MAX);
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
			else
			{
				Tone_SetPitch(TONE_MAX_PITCH - 1);
				Tone_SetChirp(-TONE_CHIRP_MAX);
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

static void UBX_HandleNavSol(void)
{
	UBX_nav_sol_saved = *((UBX_nav_sol *) UBX_payload);

	UBX_prevFix = UBX_hasFix;

	if (UBX_nav_sol_saved.gpsFix == 0x03)
	{
		UBX_hasFix = 1;
	}
	else
	{
		UBX_SetTone(UBX_INVALID_VALUE, 0, 0, 0, 0, 0);
		UBX_hasFix = 0;
	}
}

static void UBX_HandlePosition(void)
{
	const int32_t prev_hMSL = UBX_nav_pos_llh_saved.hMSL;
	int32_t hMSL;
	
	uint8_t i, suppress_tone;

	UBX_nav_pos_llh_saved = *((UBX_nav_posllh *) UBX_payload);
	hMSL = UBX_nav_pos_llh_saved.hMSL;

	if (UBX_hasFix && UBX_prevFix)
	{
		int32_t min = MIN(prev_hMSL, hMSL);
		int32_t max = MAX(prev_hMSL, hMSL);

		int i;
	
		for (i = 0; i < UBX_num_alarms; ++i)
		{
			const int32_t elev = UBX_alarms[i].elev;
		
			if (elev >= min && elev <  max)
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
				case 4:	// warble
					Tone_Beep(0, 5 * TONE_CHIRP_MAX, TONE_LENGTH_125_MS);
					break ;
				}
				
				break;
			}
		}
	}

	if (UBX_hasFix)
	{
		suppress_tone = 0;
	
		for (i = 0; i < UBX_num_alarms; ++i)
		{
			const int32_t diff = UBX_alarms[i].elev - UBX_nav_pos_llh_saved.hMSL;
		
			if (ABS (diff) < UBX_alarm_window)
			{
				suppress_tone = 1;
				break;
			}
		}
		
		if (suppress_tone && !UBX_suppress_tone)
		{
			Tone_SetRate(0);
			Tone_Stop();
		}
		
		UBX_suppress_tone = suppress_tone;
	}
}

static void UBX_GetValues(
	uint8_t mode, 
	int32_t *val, 
	int32_t *min, 
	int32_t *max)
{
	uint16_t speed_mul = 1024;

	if (UBX_use_sas)
	{
		if (UBX_nav_pos_llh_saved.height < 0)
		{
			speed_mul = pgm_read_word(&UBX_sas_table[0]);
		}
		else if (UBX_nav_pos_llh_saved.height >= 11534336L)
		{
			speed_mul = pgm_read_word(&UBX_sas_table[11]);
		}
		else
		{
			int32_t h = UBX_nav_pos_llh_saved.height / 1024	;
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
		*val = (UBX_nav_velned_saved.gSpeed * 1024) / speed_mul;
		break;
	case 1: // Vertical speed
		*val = (UBX_nav_velned_saved.velD * 1024) / speed_mul;
		break;
	case 2: // Glide ratio
		if (UBX_nav_velned_saved.velD != 0)
		{
			*val = 10000 * (int32_t) UBX_nav_velned_saved.gSpeed / UBX_nav_velned_saved.velD;
			*min *= 100;
			*max *= 100;
		}
		break;
	case 3: // Inverse glide ratio
		if (UBX_nav_velned_saved.gSpeed != 0)
		{
			*val = 10000 * UBX_nav_velned_saved.velD / (int32_t) UBX_nav_velned_saved.gSpeed;
			*min *= 100;
			*max *= 100;
		}
		break;
	case 4: // Total speed
		*val = (UBX_nav_velned_saved.speed * 1024) / speed_mul;
		break;
	}
}

static void UBX_SpeakValue(void)
{
	uint16_t speed_mul = 1024;

	if (UBX_use_sas)
	{
		if (UBX_nav_pos_llh_saved.height < 0)
		{
			speed_mul = pgm_read_word(&UBX_sas_table[0]);
		}
		else if (UBX_nav_pos_llh_saved.height >= 11534336L)
		{
			speed_mul = pgm_read_word(&UBX_sas_table[11]);
		}
		else
		{
			int32_t h = UBX_nav_pos_llh_saved.height / 1024	;
			uint16_t i = h / 1024;
			uint16_t j = h % 1024;
			uint16_t y1 = pgm_read_word(&UBX_sas_table[i]);
			uint16_t y2 = pgm_read_word(&UBX_sas_table[i + 1]);
			speed_mul = y1 + ((y2 - y1) * j) / 1024;
		}
	}

	switch (UBX_sp_units)
	{
	case UBX_UNITS_KMH:
		speed_mul = (uint16_t) (((uint32_t) speed_mul * 18204) / 65536);
		break;
	case UBX_UNITS_MPH:
		speed_mul = (uint16_t) (((uint32_t) speed_mul * 29297) / 65536);
		break;
	}

	UBX_speech_ptr = UBX_speech_buf + sizeof(UBX_speech_buf) - 1;
	*UBX_speech_ptr = 0;

	switch (UBX_sp_mode)
	{
	case 0: // Horizontal speed
		UBX_speech_ptr = Log_WriteInt32ToBuf(UBX_speech_ptr + 1, (UBX_nav_velned_saved.gSpeed * 1024) / speed_mul, 2, 1, 0);
		break;
	case 1: // Vertical speed
		UBX_speech_ptr = Log_WriteInt32ToBuf(UBX_speech_ptr + 1, (UBX_nav_velned_saved.velD * 1024) / speed_mul, 2, 1, 0);
		break;
	case 2: // Glide ratio
		if (UBX_nav_velned_saved.velD != 0)
		{
			UBX_speech_ptr = Log_WriteInt32ToBuf(UBX_speech_ptr + 1, 100 * (int32_t) UBX_nav_velned_saved.gSpeed / UBX_nav_velned_saved.velD, 2, 1, 0);
		}
		break;
	case 3: // Inverse glide ratio
		if (UBX_nav_velned_saved.gSpeed != 0)
		{
			UBX_speech_ptr = Log_WriteInt32ToBuf(UBX_speech_ptr + 1, 100 * (int32_t) UBX_nav_velned_saved.velD / UBX_nav_velned_saved.gSpeed, 2, 1, 0);
		}
		break;
	case 4: // Total speed
		UBX_speech_ptr = Log_WriteInt32ToBuf(UBX_speech_ptr + 1, (UBX_nav_velned_saved.speed * 1024) / speed_mul, 2, 1, 0);
		break;
	}

	if (UBX_sp_decimals == 0)
	{
		UBX_speech_buf[sizeof(UBX_speech_buf) - 5] = 0;
	}
	else
	{
		UBX_speech_buf[sizeof(UBX_speech_buf) - 4 + UBX_sp_decimals] = 0;
	}
}

static void UBX_HandleVelocity(void)
{
	static int32_t x0 = -1, x1, x2;
	
	int32_t val_1 = UBX_INVALID_VALUE, min_1 = UBX_min, max_1 = UBX_max;
	int32_t val_2 = UBX_INVALID_VALUE, min_2 = UBX_min_2, max_2 = UBX_max_2;

	UBX_nav_velned_saved = *((UBX_nav_velned *) UBX_payload);

	if (ABS(UBX_nav_velned_saved.velD) >= UBX_threshold && 
	    UBX_nav_velned_saved.gSpeed >= UBX_hThreshold)
	{
		UBX_GetValues(UBX_mode,   &val_1, &min_1, &max_1);
		UBX_GetValues(UBX_mode_2, &val_2, &min_2, &max_2);
	}
	else
	{
		UBX_sp_counter = 0;
	}

	if (UBX_mode_2 == 8)
	{
		UBX_GetValues(UBX_mode, &val_2, &min_2, &max_2);
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

	if (UBX_hasFix && !UBX_suppress_tone)
	{
		UBX_SetTone(val_1, min_1, max_1, val_2, min_2, max_2);
			
		if (UBX_sp_rate != 0 && 
		    UBX_sp_counter >= UBX_sp_rate)
		{
			UBX_SpeakValue();
			UBX_sp_counter = 0;
		}
	}

	UBX_sp_counter += UBX_rate;
}

static void UBX_HandleTimeUTC(void)
{
	UBX_nav_timeutc_saved = *((UBX_nav_timeutc *) UBX_payload);

	if (UBX_hasFix)
	{
		if (!Log_IsInitialized())
		{
			Power_Hold();
			
			Log_Init(
				UBX_nav_timeutc_saved.year,
				UBX_nav_timeutc_saved.month,
				UBX_nav_timeutc_saved.day,
				UBX_nav_timeutc_saved.hour,
				UBX_nav_timeutc_saved.min,
				UBX_nav_timeutc_saved.sec);
		
			Tone_FlushWhenReady();

			Tone_Beep(TONE_MAX_PITCH - 1, 0, TONE_LENGTH_125_MS);
		}
		
		UBX_write_state = UBX_WRITE_START;
	}
}

static void UBX_HandleMessage(void)
{
	// NOTE: Messages come in the order below.

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

	uint8_t success = 1;
	
	uart_init(51); // 9600 baud
	
	UBX_SendMessage(UBX_CFG, UBX_CFG_PRT, sizeof(cfg_prt), &cfg_prt);

	// NOTE: We don't wait for ACK here since some FlySights will already be
	//       set to 38400 baud.
	
	while (!uart_tx_empty());

	uart_init(12); // 38400 baud

	_delay_ms(10); // wait for GPS UART to reset
	
	UBX_SendMessage(UBX_CFG, UBX_CFG_PRT, sizeof(cfg_prt), &cfg_prt);
	if (!UBX_WaitForAck(UBX_CFG, UBX_CFG_PRT, UBX_TIMEOUT)) success = 0;

	#define SEND_MESSAGE(c,m,d) { \
		UBX_SendMessage(c,m,sizeof(d),&d); \
		if (!UBX_WaitForAck(c,m,UBX_TIMEOUT)) success = 0; }

	for (i = 0; i < n; ++i)
	{
		SEND_MESSAGE(UBX_CFG, UBX_CFG_MSG, cfg_msg[i]);
	}
	
	SEND_MESSAGE(UBX_CFG, UBX_CFG_RATE, cfg_rate);
	SEND_MESSAGE(UBX_CFG, UBX_CFG_NAV5, cfg_nav5);
	SEND_MESSAGE(UBX_CFG, UBX_CFG_RST,  cfg_rst);
	
	#undef SEND_MESSAGE

	if (!success)
	{
		LEDs_ChangeLEDs(LEDS_ALL_LEDS, LEDS_RED);
		while (1);
	}
}


void UBX_Task(void)
{
	unsigned int ch;
	char fname[13];

#ifdef TONE_DEBUG
	PORTF |= (1 << 0);
#endif

	if ((ch = uart_getc()) != UART_NO_DATA)
	{
		if (UBX_HandleByte(ch))
		{
			UBX_HandleMessage();
		}
	}
	
	if (UBX_write_state != UBX_WRITE_IDLE && Tone_CanWrite())
	{
		Power_Hold();
		
		switch (UBX_write_state++)
		{
			case 1:
				Log_WriteInt32(UBX_nav_timeutc_saved.year,  4, 0, '-');
				Log_WriteInt32(UBX_nav_timeutc_saved.month, 2, 0, '-');
				Log_WriteInt32(UBX_nav_timeutc_saved.day,   2, 0, 'T');
				Log_WriteInt32(UBX_nav_timeutc_saved.hour,  2, 0, ':');
				break;
			case 2:
				Log_WriteInt32(UBX_nav_timeutc_saved.min,   2, 0, ':');
				Log_WriteInt32(UBX_nav_timeutc_saved.sec,   2, 0, '.');
				Log_WriteInt32((UBX_nav_timeutc_saved.nano + 5000000) / 10000000, 2, 0, 'Z');
				Log_WriteChar(',');
				break;
			case 3:
				Log_WriteInt32(UBX_nav_pos_llh_saved.lat,   7, 1, ',');
				break;
			case 4:
				Log_WriteInt32(UBX_nav_pos_llh_saved.lon,   7, 1, ',');
				break;
			case 5:
				Log_WriteInt32(UBX_nav_pos_llh_saved.hMSL,  3, 1, ',');
				Log_WriteInt32(UBX_nav_velned_saved.velN,   2, 1, ',');
				break;
			case 6:
				Log_WriteInt32(UBX_nav_velned_saved.velE,   2, 1, ',');
				Log_WriteInt32(UBX_nav_velned_saved.velD,   2, 1, ',');
				Log_WriteInt32(UBX_nav_pos_llh_saved.hAcc,  3, 1, ',');
				break;
			case 7:
				Log_WriteInt32(UBX_nav_pos_llh_saved.vAcc,  3, 1, ',');
				Log_WriteInt32(UBX_nav_velned_saved.sAcc,   2, 1, ',');
				Log_WriteInt32(UBX_nav_sol_saved.gpsFix,    0, 0, ',');
				Log_WriteInt32(UBX_nav_sol_saved.numSV,     0, 0, '\r');
				Log_WriteChar('\n');
				UBX_write_state = UBX_WRITE_IDLE;
				break;
		}

		Tone_FlushWhenReady();
	}
	
	if (*UBX_speech_ptr && Tone_IsIdle())
	{
		if (*UBX_speech_ptr == '-')
		{
			Tone_Play("minus.wav");
		}
		else if (*UBX_speech_ptr == '.')
		{
			Tone_Play("dot.wav");
		}
		else if (*UBX_speech_ptr >= '0' && *UBX_speech_ptr <= '9')
		{
			fname[0] = *UBX_speech_ptr;
			fname[1] = '.';
			fname[2] = 'w';
			fname[3] = 'a';
			fname[4] = 'v';
			fname[5] = 0;
			
			Tone_Play(fname);
		}
		
		++UBX_speech_ptr;
	}
	
#ifdef TONE_DEBUG
	PORTF &= ~(1 << 0);
#endif
}
