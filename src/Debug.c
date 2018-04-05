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
#include <util/delay.h>

static void Debug_WriteChar(char ch)
{
	uint8_t i;

	// Start bit
	PORTF &= ~(1 << 5);
	_delay_ms(1);

	// Data bits
	for (i = 0; i < 8; ++i)
	{
		if (ch & 0x01)
		{
			PORTF |= (1 << 5);
		}
		else
		{
			PORTF &= ~(1 << 5);
		}
		_delay_ms(1);
		
		ch >>= 1;
	}
	
	// Stop bit
	PORTF |= (1 << 5);
	_delay_ms(1);
}

void Debug_WriteString(const char *str)
{
	char ch;

	while ((ch = *(str++)) != 0)
	{
		Debug_WriteChar(ch);
	}
}

void Debug_Init(void)
{
	// Initialize debugging pins
	MCUCR |= (1 << JTD);
	MCUCR |= (1 << JTD);
	
	DDRF  |= 0x7f;
	PORTF |= 0x7f;
}
