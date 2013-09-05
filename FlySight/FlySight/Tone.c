#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/pgmspace.h>
#include <util/atomic.h>

#include "Board/LEDs.h"
#include "FatFS/ff.h"
#include "Log.h"
#include "Main.h"
#include "Power.h"
#include "Tone.h"

#define TONE_BUFFER_LEN  MAIN_BUFFER_SIZE
#define TONE_BUFFER_READ (TONE_BUFFER_LEN / 2)

#define TONE_STATE_IDLE  0
#define TONE_STATE_PLAY  1
#define TONE_STATE_WRITE 2

#define TONE_FLAGS_LOAD  1
#define TONE_FLAGS_STOP  2
#define TONE_FLAGS_BEEP  4

#define TONE_MODE_BEEP   0
#define TONE_MODE_WAV    1

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

static volatile uint16_t Tone_read;
static volatile uint16_t Tone_write;
static volatile uint16_t Tone_load;

static          uint32_t Tone_step;
static          uint32_t Tone_chirp; 
static          uint16_t Tone_len;

static volatile uint8_t  Tone_state = TONE_STATE_IDLE;
static          uint8_t  Tone_mode;

static          FIL      Tone_file;

                uint16_t Tone_volume = 2;

static volatile uint16_t Tone_next_index = 0;
static volatile uint32_t Tone_next_chirp = 0; 
static volatile uint16_t Tone_rate = 0;

static          DWORD    Tone_prevSect;

static volatile uint8_t  Tone_flags = 0;

static          uint8_t  Tone_need_flush = 0;

ISR(TIMER1_OVF_vect)
{	
	const uint16_t i = Tone_read;

	if (i == Tone_write)
	{
		TCCR1A = 0;
		TCCR1B = 0;
		TIMSK1 = 0;

		Tone_flags |= TONE_FLAGS_STOP;
	}
	else 
	{
		if (i == Tone_load)
		{
			Tone_flags |= TONE_FLAGS_LOAD;
		}
		
		OCR1A = OCR1B = Main_buffer[i];
		
		Tone_read = (i + 1) % TONE_BUFFER_LEN;
	}
}

void Tone_Init(void)
{
	DDRB |= (1 << 6) | (1 << 5);
}

void Tone_Update(void)
{
	static uint16_t tone_timer = 0;

	if (0 - tone_timer < Tone_rate)
	{
		Tone_flags |= TONE_FLAGS_BEEP;
	}

	tone_timer += Tone_rate;
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
		Tone_next_index = index;
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

static void Tone_LoadTable(void)
{
	static uint16_t phase = 0;
	       uint16_t size = TONE_BUFFER_READ;
		   uint16_t i = Tone_write;

	while (size && Tone_len)
	{
		const uint8_t val = pgm_read_byte(&Tone_sine_table[phase >> 8]);
		Main_buffer[i++] = 128 - (128 >> Tone_volume) + (val >> Tone_volume);
		phase += Tone_step >> 16;
		Tone_step += Tone_chirp;
		
		--size;
		--Tone_len;
	}
	
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_load = Tone_write;
		Tone_write = i % TONE_BUFFER_LEN;
	}
}

static void Tone_LoadWAV(void)
{
	      UINT     br;
	const uint16_t size = TONE_BUFFER_READ;

	f_read(&Tone_file, (void *) (Main_buffer + Tone_write), size, &br);
	
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_load  = Tone_write;
		Tone_write = (Tone_write + br) % TONE_BUFFER_LEN;
	}
}

static void Tone_Load(void)
{
	switch (Tone_mode)
	{
	case TONE_MODE_BEEP:
		Tone_LoadTable();
		Tone_state = TONE_STATE_WRITE;
		break;
	case TONE_MODE_WAV:
		Tone_LoadWAV();
		if (Tone_file.dsect != Tone_prevSect)
		{
			Tone_state = TONE_STATE_WRITE;
		}
		else
		{
			Tone_state = TONE_STATE_PLAY;
		}
		Tone_prevSect = Tone_file.dsect;
		break;
	}
		
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_flags &= ~TONE_FLAGS_LOAD;
	}
}

static void Tone_Start(
	uint8_t mode)
{
	if (Tone_state == TONE_STATE_IDLE)
	{
		Tone_mode     = mode;
		Tone_prevSect = 0;
		
		ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
		{
			Tone_read  = 0;
			Tone_write = 0;
		}

		Tone_Load();
		
		TCNT1 = 255;
		OCR1A = OCR1B = Main_buffer[0];
		
		TCCR1A = (1 << COM1A1) | (1 << COM1A0) | (1 << COM1B1) | (1 << WGM10);
		TCCR1B = (1 << WGM12) | (1 << CS10);
		TIMSK1 = (1 << TOIE1);
	}
}

void Tone_Stop(void)
{
	if (Tone_state != TONE_STATE_IDLE)
	{
		TCCR1A = 0;
		TCCR1B = 0;
		TIMSK1 = 0;

		switch (Tone_mode)
		{
		case TONE_MODE_BEEP:
			break;
		case TONE_MODE_WAV:
			f_close(&Tone_file);
			break;
		}
		
		if (Tone_need_flush)
		{
			Log_Flush();
			Power_Release();
			Tone_need_flush = 0;
		}

		Tone_state = TONE_STATE_IDLE;
	}

	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_flags &= ~TONE_FLAGS_LOAD;
		Tone_flags &= ~TONE_FLAGS_STOP;
	}
}

void Tone_Task(void)
{
	if (Tone_state == TONE_STATE_WRITE)
	{
		Tone_state = TONE_STATE_PLAY;
	}
	
	if (Tone_flags & TONE_FLAGS_BEEP)
	{
		if (Tone_state == TONE_STATE_IDLE)
		{
			Tone_Beep(Tone_next_index, Tone_next_chirp, TONE_LENGTH_125_MS);
		}

		ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
		{
			Tone_flags &= ~TONE_FLAGS_BEEP;
		}
	}

	if (Tone_flags & TONE_FLAGS_STOP)
	{
		Tone_Stop();
	}
	
	if (Tone_flags & TONE_FLAGS_LOAD)
	{
		Tone_Load();
	}
}

void Tone_Beep(
	uint16_t index,
	uint32_t chirp,
	uint16_t len)
{
	Tone_Stop();
	
	Tone_step  = (int32_t) index * 3242 + 30212096;
	Tone_chirp = chirp;
	Tone_len   = len;
	
	Tone_Start(TONE_MODE_BEEP);
}

void Tone_Play(
	const char *filename)
{
	Tone_Stop();

	f_chdir("\\audio");

	if (f_open(&Tone_file, filename, FA_READ) == FR_OK)
	{
		f_lseek(&Tone_file, 44);

		Tone_Start(TONE_MODE_WAV);
	}
}

uint8_t Tone_CanWrite(void)
{
	return Tone_state == TONE_STATE_IDLE || Tone_state == TONE_STATE_WRITE;
}

uint8_t Tone_IsIdle(void)
{
	return Tone_state == TONE_STATE_IDLE;
}

void Tone_FlushWhenReady(void)
{
	if (Tone_state == TONE_STATE_IDLE)
	{
		Log_Flush();
		Power_Release();
		Tone_need_flush = 0;
	}
	else
	{
		Tone_need_flush = 1;
	}
}
