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

#define MIN(a,b) (((a) < (b)) ?  (a) : (b))
#define MAX(a,b) (((a) > (b)) ?  (a) : (b))

#define TONE_BUFFER_LEN   MAIN_BUFFER_SIZE		 // size of circular buffer
#define TONE_BUFFER_WRITE (TONE_BUFFER_LEN / 8)  // threshold for writing between read operations
#define TONE_BUFFER_CHUNK (TONE_BUFFER_LEN / 8)  // maximum bytes read in one operation

#define TONE_SAMPLE_LEN  4  // number of repeated PWM samples

#define TONE_STATE_IDLE  0
#define TONE_STATE_PLAY  1

#define TONE_FLAGS_LOAD  1
#define TONE_FLAGS_STOP  2
#define TONE_FLAGS_BEEP  4
#define TONE_FLAGS_WRITE 8

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

static volatile uint8_t  Tone_flags = 0;

static          uint8_t  Tone_need_flush = 0;

ISR(TIMER1_OVF_vect)
{
	static uint8_t i = 0;
	static uint16_t s1, s2, step;

	if (i++ % TONE_SAMPLE_LEN)
	{
		s1 += step;
	}
	else if (Tone_read == Tone_write)
	{
#ifdef TONE_DEBUG
		if (Tone_flags & TONE_FLAGS_LOAD)
		{
			PORTF ^= (1 << 5);
		}
#endif
		
		TCCR1A = 0;
		TCCR1B = 0;
		TIMSK1 = 0;

		Tone_flags |= TONE_FLAGS_STOP;
	}
	else 
	{
		s1 = s2;
		s2 = (uint16_t) Main_buffer[Tone_read % TONE_BUFFER_LEN] << 8;
		
		// The contortions below are necessary to ensure that the division by 
		// TONE_SAMPLE_LEN uses shift operations instead of calling a signed 
		// integer division function.
		
		if (s1 <= s2)
		{
			step = (s2 - s1) / TONE_SAMPLE_LEN;
		}
		else
		{
			step = -((s1 - s2) / TONE_SAMPLE_LEN);
		}
		
		++Tone_read;
	}

	OCR1A = OCR1B = s1 >> 8;
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
	       uint8_t  val;
		   uint16_t read;
	       uint16_t size, i;

	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		read = Tone_read;
	}

	size = read + TONE_BUFFER_LEN - Tone_write;
	size = MIN(size, Tone_len);

	for (i = 0; i < size; ++i, --Tone_len)
	{
		val = pgm_read_byte(&Tone_sine_table[phase >> 8]);

		phase += Tone_step >> 16;
		Tone_step += Tone_chirp;

		val = 128 - (128 >> Tone_volume) + (val >> Tone_volume);
		Main_buffer[(Tone_write + i) % TONE_BUFFER_LEN] = val;
	}

	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_write += size;
		if (size >= TONE_BUFFER_WRITE)
		{
			Tone_flags &= ~TONE_FLAGS_WRITE;
		}

		if (!Tone_len)
		{
			Tone_flags &= ~TONE_FLAGS_LOAD;
		}
	}
}

static void Tone_ReadFile(
	uint16_t size)
{
	UINT     br;
	uint16_t i;
	uint8_t  val;

	f_read(&Tone_file, &Main_buffer[Tone_write % TONE_BUFFER_LEN], size, &br);

	for (i = 0; i < br; ++i)
	{
		val = Main_buffer[(Tone_write + i) % TONE_BUFFER_LEN];
		val = 128 - (128 >> Tone_volume) + (val >> Tone_volume);
		Main_buffer[(Tone_write + i) % TONE_BUFFER_LEN] = val;
	}

	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_write += br;
		if (br >= TONE_BUFFER_WRITE)
		{
			Tone_flags &= ~TONE_FLAGS_WRITE;
		}

		if (br != size)
		{
			Tone_flags &= ~TONE_FLAGS_LOAD;
		}
	}
}

static void Tone_LoadWAV(void)
{
	uint16_t read;
	uint16_t size;

	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		read = Tone_read;
	}

	if (Tone_write != read + TONE_BUFFER_LEN)
	{
		size = MIN(TONE_BUFFER_CHUNK, read + TONE_BUFFER_LEN - Tone_write);

		if (Tone_write / TONE_BUFFER_LEN != (Tone_write + size) / TONE_BUFFER_LEN)
		{
			size -= TONE_BUFFER_LEN - (Tone_write % TONE_BUFFER_LEN);
			Tone_ReadFile(TONE_BUFFER_LEN - (Tone_write % TONE_BUFFER_LEN));
		}

		if (Tone_flags & TONE_FLAGS_LOAD)
		{
			Tone_ReadFile(size);
		}
	}
}

static void Tone_Load(void)
{
	switch (Tone_mode)
	{
	case TONE_MODE_BEEP:
		Tone_LoadTable();
		break;
	case TONE_MODE_WAV:
		Tone_LoadWAV();
		break;
	}
}

static void Tone_Start(
	uint8_t mode)
{
	if (Tone_state == TONE_STATE_IDLE)
	{
		Tone_state = TONE_STATE_PLAY;

		Tone_mode = mode;
		
		Tone_flags |= TONE_FLAGS_LOAD;
		
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
		Tone_flags &= ~TONE_FLAGS_STOP;
		Tone_flags &= ~TONE_FLAGS_LOAD;
	}
}

void Tone_Task(void)
{
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Tone_flags |= TONE_FLAGS_WRITE;
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
	
	Tone_step  = ((int32_t) index * 3242 + 30212096) * TONE_SAMPLE_LEN;
	Tone_chirp = chirp;
	Tone_len   = len / TONE_SAMPLE_LEN;
	
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
	return (Tone_state == TONE_STATE_IDLE) || (Tone_flags & TONE_FLAGS_WRITE);
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