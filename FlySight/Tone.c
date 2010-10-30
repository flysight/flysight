#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/pgmspace.h>
#include <util/atomic.h>

#include "Tone.h"

#define MIN(a,b) ((a) < (b) ? (a) : (b))

#define AUDIO_DDR  DDRB
#define AUDIO_MASK (1 << 6)

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

static volatile uint16_t Tone_next_step = 461;
static volatile uint16_t Tone_step      = 461;
static volatile uint16_t Tone_rate      = 0;
       volatile uint16_t Tone_volume    = 0;
static volatile uint16_t Tone_length    = TONE_LENGTH_125_MS;
static volatile uint16_t Tone_count     = 0;

static uint8_t Tone_enabled = 0;

ISR(TIMER1_OVF_vect)
{	
	static uint16_t phase = 0;

	if (++Tone_count < Tone_length)
	{
		uint8_t val = pgm_read_byte(&Tone_sine_table[phase >> 8]);
		OCR1A = OCR1B = 128 - (128 >> Tone_volume) + (val >> Tone_volume);
		phase += Tone_step;
	}
	else
	{
		TCCR1A = 0;
		TCCR1B = 0;
		TIMSK1 = 0;
		phase = 0;
	}
}

void Tone_Init(void)
{
	TCCR1A = (1 << WGM10);
	TCCR1B = (1 << WGM12);
	
	DDRB |= (1 << 6) | (1 << 5);

	Tone_enabled = 1;
}

void Tone_Update(void)
{
	if (Tone_enabled)
	{
		static uint16_t tone_timer = 0;

		if (0 - tone_timer < Tone_rate)
		{
			Tone_step = Tone_next_step;
			Tone_count = 0;

			if (TIMSK1 == 0)
			{
				TCCR1A = (1 << COM1A1) | (1 << COM1A0) | (1 << COM1B1) | (1 << WGM10);
				TCCR1B = (1 << WGM12) | (1 << CS10);
				TIMSK1 = (1 << TOIE1);
				OCR1B  = 128;
			}
		}

		tone_timer += Tone_rate;
	}
}

void Tone_SetRate(
	uint16_t rate)
{
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_rate = rate;
	}
}

void Tone_SetPitch(
	uint16_t index)
{
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_next_step = ((int32_t) index * 3242 + 30212096) >> 16;
	}
}

void Tone_Beep(
	uint16_t pitch,
	uint16_t length)
{
	uint16_t prev_step   = Tone_next_step;
	uint16_t prev_length = Tone_length;

	while (TIMSK1);

	Tone_SetPitch(pitch);

	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_length = length;
	}

	Tone_step = Tone_next_step;
	Tone_count = 0;

	TCCR1A = (1 << COM1A1) | (1 << COM1A0) | (1 << COM1B1) | (1 << WGM10);
	TCCR1B = (1 << WGM12) | (1 << CS10);
	TIMSK1 = (1 << TOIE1);
	OCR1B  = 128;
	
	while (TIMSK1);
	
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_next_step = prev_step;
		Tone_length    = prev_length;
	}
}
