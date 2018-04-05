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

#ifndef FLYSIGHT_LEDS_H
#define FLYSIGHT_LEDS_H

#include <avr/io.h>

/** LED mask for the first LED on the board. */
#define LEDS_GREEN    (1 << 5)

/** LED mask for the second LED on the board. */
#define LEDS_RED      (1 << 6)

/** LED mask for all the LEDs on the board. */
#define LEDS_ALL_LEDS (LEDS_GREEN | LEDS_RED)

/** LED mask for the none of the board LEDs */
#define LEDS_NO_LEDS  0

/* Inline Functions: */
static inline void LEDs_Init(void)
{
	DDRC  |=  LEDS_ALL_LEDS;
	PORTC &= ~LEDS_ALL_LEDS;
}

static inline void LEDs_TurnOnLEDs(
	const uint8_t LEDMask)
{
	PORTC |= LEDMask;
}

static inline void LEDs_TurnOffLEDs(
	const uint8_t LEDMask)
{
	PORTC &= ~LEDMask;
}

static inline void LEDs_SetAllLEDs(
	const uint8_t LEDMask)
{
	PORTC &= ~LEDS_ALL_LEDS;
	PORTC |= LEDMask;
}

static inline void LEDs_ChangeLEDs(
	const uint8_t LEDMask, 
	const uint8_t ActiveMask)
{
	PORTC &= ~LEDMask;
	PORTC |= ActiveMask;
}

static inline void LEDs_ToggleLEDs(
	const uint8_t LEDMask)
{
	PORTC ^= LEDMask;
}

static inline uint8_t LEDs_GetLEDs(void)
{
	return PORTC & LEDS_ALL_LEDS;
}

#endif
