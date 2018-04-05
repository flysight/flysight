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

#define PWR_HOLD_DDR   DDRC
#define PWR_HOLD_PORT  PORTC
#define PWR_HOLD_MASK  (1 << 2)

void Power_Hold(void)
{
	PWR_HOLD_DDR  |= PWR_HOLD_MASK;
	PWR_HOLD_PORT |= PWR_HOLD_MASK;
}

void Power_Release(void)
{
	PWR_HOLD_DDR  |=  PWR_HOLD_MASK;
	PWR_HOLD_PORT &= ~PWR_HOLD_MASK;
}
