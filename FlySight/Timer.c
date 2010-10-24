#include <avr/io.h>
#include <avr/interrupt.h>
#include <util/atomic.h>

#include "Tone.h"
#include "UBX.h"

static volatile uint16_t Timer_timer = 0;

ISR(TIMER3_COMPA_vect)
{
	Tone_Update();
	UBX_Update();

	if (Timer_timer > 0)
	{
		--Timer_timer;
	}
}

void Timer_Init(void)
{
    TCCR3B = (1 << WGM32) | (1 << CS31);
	TIMSK3 = (1 << OCIE3A);
	OCR3A = 1000;
}

void Timer_Set(
	uint16_t ms)
{
	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		Timer_timer = ms;
	}
}

uint16_t Timer_Get(void)
{
	uint16_t timer_l;

	ATOMIC_BLOCK(ATOMIC_RESTORESTATE)
	{
		timer_l = Timer_timer;
	}
	
	return timer_l;
}

void Timer_Wait(
	uint16_t ms)
{
	Timer_Set(ms);
	while (Timer_Get());
}
