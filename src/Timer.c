/***************************************************************************
**                                                                        **
**  FlySight firmware                                                     **
**  Copyright 2018 Michael Cooper                                         **
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
