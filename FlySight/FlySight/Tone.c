#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/pgmspace.h>
#include <util/atomic.h>

#include "Tone.h"

#define MIN(a,b) ((a) < (b) ? (a) : (b))

#define AUDIO_DDR  DDRB
#define AUDIO_MASK (1 << 6)

#define TONE_SOURCE_BEEP  (1 << 0)
#define TONE_SOURCE_ALARM (1 << 1)

#define TONE_MAX_STEP (TONE_MAX_PITCH * 3242L + 30212096L)
#define TONE_MIN_STEP (30212096L)
#define TONE_RGE_STEP (TONE_MAX_STEP - TONE_MIN_STEP)

static const uint8_t Tone_sine_table[] PROGMEM =
{
	128, 131, 134, 137, 140, 143, 146, 149,
	153, 156, 159, 162, 165, 168, 171, 174,
	177, 180, 182, 185, 188, 191, 194, 196,
	199, 201, 204, 207, 209, 211, 214, 216,
	218, 220, 223, 225, 227, 229, 231, 232,
	234, 236, 238, 239, 241, 242, 243, 245,
	246, 247, 248, 249, 250, 251, 252, 253,
	253, 254, 254, 255, 255, 255, 255, 255,
	255, 255, 255, 255, 255, 254, 254, 253,
	253, 252, 251, 251, 250, 249, 248, 247,
	245, 244, 243, 241, 240, 238, 237, 235,
	233, 232, 230, 228, 226, 224, 222, 219,
	217, 215, 213, 210, 208, 205, 203, 200,
	198, 195, 192, 189, 187, 184, 181, 178,
	175, 172, 169, 166, 163, 160, 157, 154,
	151, 148, 145, 142, 139, 135, 132, 129,
	126, 123, 120, 116, 113, 110, 107, 104,
	 101, 98,  95,  92,  89,  86,  83,  80,
	 77,  74,  71,  68,  66,  63,  60,  57,
	 55,  52,  50,  47,  45,  42,  40,  38,
	 36,  33,  31,  29,  27,  25,  23,  22,
	 20,  18,  17,  15,  14,  12,  11,  10,
	  8,   7,   6,   5,   4,   4,   3,   2,
	  2,   1,   1,   0,   0,   0,   0,   0,
	  0,   0,   0,   0,   0,   1,   1,   2,
	  2,   3,   4,   5,   6,   7,   8,   9,
	 10,  12,  13,  14,  16,  17,  19,  21,
	 23,  24,  26,  28,  30,  32,  35,  37,
	 39,  41,  44,  46,  48,  51,  54,  56,
	 59,  61,  64,  67,  70,  73,  75,  78,
	 81,  84,  87,  90,  93,  96,  99, 102,
	106, 109, 112, 115, 118, 121, 124, 128
};

static volatile uint32_t Tone_beep_chirp   = 0;
static volatile uint32_t Tone_beep_step    = (uint32_t) 461 << 16;
static volatile uint16_t Tone_beep_count   = 0;
static volatile uint16_t Tone_beep_length  = TONE_LENGTH_125_MS;
static volatile uint16_t Tone_beep_rate    = 0;

static volatile uint32_t Tone_next_chirp   = 0;
static volatile uint32_t Tone_next_step    = (uint32_t) 461 << 16;

static volatile uint32_t Tone_alarm_chirp  = 0;
static volatile uint32_t Tone_alarm_step   = (uint32_t) 461 << 16;
static volatile uint16_t Tone_alarm_count  = 0;
static volatile uint16_t Tone_alarm_length = TONE_LENGTH_125_MS;
static volatile uint16_t Tone_alarm_rate   = 0;

       volatile uint16_t Tone_volume       = 0;

static volatile uint8_t  Tone_source_flags = 0;

static void Tone_EnableSource(
	uint8_t flags)
{
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		if (!Tone_source_flags)
		{
			TCCR1A = (1 << COM1A1) | (1 << COM1A0) | (1 << COM1B1) | (1 << WGM10);
			TCCR1B = (1 << WGM12) | (1 << CS10);
			TIMSK1 = (1 << TOIE1);
			OCR1B  = 128;
		}

		Tone_source_flags |= flags;
	}
}

static void Tone_DisableSource(
	uint8_t flags)
{
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_source_flags &= ~flags;
		
		if (!Tone_source_flags)
		{
			TCCR1A = 0;
			TCCR1B = 0;
			TIMSK1 = 0;
		}
	}
}

ISR(TIMER1_OVF_vect)
{	
	static uint16_t beep_phase  = 0;
	static uint16_t alarm_phase = 0;

	if (Tone_source_flags & TONE_SOURCE_ALARM)
	{
		uint8_t val = pgm_read_byte(&Tone_sine_table[alarm_phase >> 8]);
		OCR1A = OCR1B = 128 - (128 >> Tone_volume) + (val >> Tone_volume);
	}
	else if (Tone_source_flags & TONE_SOURCE_BEEP)
	{
		uint8_t val = pgm_read_byte(&Tone_sine_table[beep_phase >> 8]);
		OCR1A = OCR1B = 128 - (128 >> Tone_volume) + (val >> Tone_volume);
	}

	if (Tone_source_flags & TONE_SOURCE_ALARM)
	{
		if (++Tone_alarm_count < Tone_alarm_length)
		{
			alarm_phase += (Tone_alarm_step >> 16);
			Tone_alarm_step += Tone_alarm_chirp;

			while (Tone_alarm_step <  TONE_MIN_STEP) Tone_alarm_step += TONE_RGE_STEP;
			while (Tone_alarm_step >= TONE_MAX_STEP) Tone_alarm_step -= TONE_RGE_STEP;
		}
		else
		{
			Tone_DisableSource(TONE_SOURCE_ALARM);
			alarm_phase = 0;
		}

	}
	
	if (Tone_source_flags & TONE_SOURCE_BEEP)
	{
		if (++Tone_beep_count < Tone_beep_length)
		{
			beep_phase += (Tone_beep_step >> 16);
			Tone_beep_step += Tone_beep_chirp;

			while (Tone_beep_step <  TONE_MIN_STEP) Tone_beep_step += TONE_RGE_STEP;
			while (Tone_beep_step >= TONE_MAX_STEP) Tone_beep_step -= TONE_RGE_STEP;
		}
		else
		{
			Tone_DisableSource(TONE_SOURCE_BEEP);
			beep_phase = 0;
		}
	}
}

void Tone_Init(void)
{
	TCCR1A = (1 << WGM10);
	TCCR1B = (1 << WGM12);
	DDRB |= (1 << 6) | (1 << 5);
}

void Tone_Update(void)
{
	static uint16_t tone_timer = 0;

	if (0 - tone_timer < Tone_beep_rate)
	{
		Tone_beep_chirp = Tone_next_chirp;
		Tone_beep_step = Tone_next_step;
		Tone_beep_count = 0;
		Tone_EnableSource (TONE_SOURCE_BEEP);
	}

	tone_timer += Tone_beep_rate;
}

void Tone_SetRate(
	uint16_t rate)
{
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_beep_rate = rate;
	}
}

void Tone_SetPitch(
	uint16_t pitch)
{
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_next_step = (int32_t) pitch * 3242 + 30212096;
	}
}

void Tone_SetChirp(
	uint32_t chirp)
{
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_next_chirp = chirp;
	}
}

void Tone_Alarm(
	uint16_t pitch,
	uint32_t chirp,
	uint16_t length)
{
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_alarm_chirp = chirp;
		Tone_alarm_step = (int32_t) pitch * 3242 + 30212096;
		Tone_alarm_count = 0;
		Tone_alarm_length = length;
	}
	
	Tone_EnableSource (TONE_SOURCE_ALARM);
}
