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

static const uint16_t Tone_step_table[] PROGMEM =
{
	 461,  474,  486,  499,  512,  524,  537,  550,
	 562,  575,  588,  600,  613,  626,  638,  651,
	 664,  676,  689,  702,  714,  727,  740,  752,
	 765,  778,  790,  803,  815,  828,  841,  853,
	 866,  879,  891,  904,  917,  929,  942,  955,
	 967,  980,  993, 1005, 1018, 1031, 1043, 1056,
	1069, 1081, 1094, 1107, 1119, 1132, 1145, 1157,
	1170, 1183, 1195, 1208, 1221, 1233, 1246, 1259,
	1271, 1284, 1297, 1309, 1322, 1335, 1347, 1360,
	1373, 1385, 1398, 1411, 1423, 1436, 1449, 1461,
	1474, 1487, 1499, 1512, 1525, 1537, 1550, 1563,
	1575, 1588, 1601, 1613, 1626, 1639, 1651, 1664,
	1677, 1689, 1702, 1715, 1727, 1740, 1753, 1765,
	1778, 1791, 1803, 1816, 1829, 1841, 1854, 1867,
	1879, 1892, 1905, 1917, 1930, 1943, 1955, 1968,
	1981, 1993, 2006, 2019, 2031, 2044, 2057, 2069,
	2082, 2095, 2107, 2120, 2133, 2145, 2158, 2171,
	2183, 2196, 2209, 2221, 2234, 2247, 2259, 2272,
	2285, 2297, 2310, 2323, 2335, 2348, 2361, 2373,
	2386, 2399, 2411, 2424, 2437, 2449, 2462, 2475,
	2487, 2500, 2513, 2525, 2538, 2551, 2563, 2576,
	2589, 2601, 2614, 2627, 2639, 2652, 2665, 2677,
	2690, 2703, 2715, 2728, 2741, 2753, 2766, 2779,
	2791, 2804, 2817, 2829, 2842, 2855, 2867, 2880,
	2893, 2905, 2918, 2931, 2943, 2956, 2969, 2981,
	2994, 3007, 3019, 3032, 3045, 3057, 3070, 3083,
	3095, 3108, 3121, 3133, 3146, 3159, 3171, 3184,
	3197, 3209, 3222, 3235, 3247, 3260, 3273, 3285,
	3298, 3311, 3323, 3336, 3349, 3361, 3374, 3387,
	3399, 3412, 3425, 3437, 3450, 3463, 3475, 3488,
	3501, 3513, 3526, 3539, 3551, 3564, 3577, 3589,
	3602, 3614, 3627, 3640, 3652, 3665, 3678, 3690
};

const uint16_t Tone_max_pitch    = 65280L;
const uint16_t Tone_one_semitone = 65280L / 36; // full scale is 3 octaves

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
	index = MIN(index, Tone_max_pitch - 1);
	const uint16_t step_1 = pgm_read_word(&Tone_step_table[index / 256]);
	const uint16_t step_2 = pgm_read_word(&Tone_step_table[index / 256 + 1]);

	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_next_step = step_1 + (step_2 - step_1) * (index % 256) / 256;
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
