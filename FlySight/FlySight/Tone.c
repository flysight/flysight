#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/pgmspace.h>
#include <util/atomic.h>

#include "FatFS/ff.h"
#include "Tone.h"

#define MIN(a,b) ((a) < (b) ? (a) : (b))

#define AUDIO_DDR  DDRB
#define AUDIO_MASK (1 << 6)

#define TONE_PLAY_BUF_SIZE 512
#define TONE_PLAY_BUF_HALF (TONE_PLAY_BUF_SIZE / 2)

#define TONE_PLAY_IDLE   0
#define TONE_PLAY_ACTIVE 1
#define TONE_PLAY_READ   2

#define TONE_SOURCE_BEEP 1
#define TONE_SOURCE_FILE 2

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

static volatile uint16_t Tone_next_step  = 461;
static volatile uint32_t Tone_step       = (uint32_t) 461 << 16;
static volatile uint16_t Tone_rate       = 0;
       volatile uint16_t Tone_volume     = 0;
static volatile uint16_t Tone_length     = TONE_LENGTH_125_MS;
static volatile uint16_t Tone_count      = 0;
static volatile uint32_t Tone_next_chirp = 0;
static volatile uint32_t Tone_chirp      = 0; 

static          uint8_t  Tone_enabled      = 0;
static volatile uint8_t  Tone_source_flags = 0;

static          uint8_t  Tone_play_buf[TONE_PLAY_BUF_SIZE];
static volatile uint16_t Tone_play_pos;
static volatile uint16_t Tone_play_pos_end;
static volatile uint16_t Tone_play_pos_read;
static volatile uint8_t  Tone_play_status = TONE_PLAY_IDLE;

static FIL Tone_play_file;

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
	if (Tone_source_flags & TONE_SOURCE_BEEP)
	{
		static uint16_t phase = 0;

		if (++Tone_count < Tone_length)
		{
			uint8_t val = pgm_read_byte(&Tone_sine_table[phase >> 8]);
			OCR1A = OCR1B = 128 - (128 >> Tone_volume) + (val >> Tone_volume);
			phase += (Tone_step >> 16);
			Tone_step += Tone_chirp;
		}
		else
		{
			Tone_DisableSource(TONE_SOURCE_BEEP);
			phase = 0;
		}
	}
	else if (Tone_source_flags & TONE_SOURCE_FILE)
	{
		uint8_t val = Tone_play_buf[Tone_play_pos];
		OCR1A = OCR1B = 128 - (128 >> Tone_volume) + (val >> Tone_volume);
		Tone_play_pos = (Tone_play_pos + 1) % TONE_PLAY_BUF_SIZE;
		
		if (Tone_play_pos == Tone_play_pos_end)
		{
			Tone_DisableSource(TONE_SOURCE_FILE);
			Tone_play_status = TONE_PLAY_IDLE;
		}
		else if (Tone_play_pos == Tone_play_pos_read)
		{
			Tone_play_status = TONE_PLAY_READ;
		}
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
			while (Tone_source_flags & TONE_SOURCE_BEEP);
		
			Tone_step = (uint32_t) Tone_next_step << 16;
			Tone_chirp = Tone_next_chirp;
			Tone_count = 0;
			
			Tone_EnableSource (TONE_SOURCE_BEEP);
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

void Tone_SetChirp(
	uint32_t chirp)
{
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_next_chirp = chirp;
	}
}

void Tone_Beep(
	uint16_t pitch,
	uint16_t length)
{
	uint16_t prev_step   = Tone_next_step;
	uint32_t prev_chirp  = Tone_next_chirp;
	uint16_t prev_length = Tone_length;

	while (Tone_source_flags & TONE_SOURCE_BEEP);

	Tone_SetPitch(pitch);
	Tone_SetChirp(0);

	Tone_length = length;

	Tone_step = (uint32_t) Tone_next_step << 16;
	Tone_chirp = Tone_next_chirp;
	Tone_count = 0;

	Tone_EnableSource (TONE_SOURCE_BEEP);
	
	while (Tone_source_flags & TONE_SOURCE_BEEP);
	
	Tone_next_step  = prev_step;
	Tone_next_chirp = prev_chirp;
	Tone_length     = prev_length;
}

static void Tone_ReadData (
	uint16_t start)
{
	UINT br;
	
	f_read(&Tone_play_file, Tone_play_buf + start, TONE_PLAY_BUF_HALF, &br);

	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_play_pos_read = start;
		Tone_play_status = TONE_PLAY_ACTIVE;
	}

	if (f_eof (&Tone_play_file))
	{
		Tone_play_pos_end = (start + br) % TONE_PLAY_BUF_SIZE;
		f_close(&Tone_play_file);
	}
}

void Tone_Play(
	const char *fname)
{
	FRESULT res;

	while (Tone_source_flags & TONE_SOURCE_FILE);
	
	res = f_open(&Tone_play_file, fname, FA_READ);
	if (res != FR_OK) return;
	
	f_lseek(&Tone_play_file, 44);

	Tone_ReadData (0);
	
	Tone_play_pos     = 0;
	Tone_play_pos_end = TONE_PLAY_BUF_SIZE;

	Tone_EnableSource (TONE_SOURCE_FILE);
}

void Tone_Task(void)
{
	if (Tone_play_status == TONE_PLAY_READ)
	{
		Tone_ReadData ((Tone_play_pos_read + TONE_PLAY_BUF_HALF) % TONE_PLAY_BUF_SIZE);
	}
}
