/***************************************************************************
**                                                                        **
**  FlySight firmware                                                     **
**  Copyright 2018 Will Glynn                                             **
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

#ifndef TIME_H
#define TIME_H

#include <stdint.h>

uint32_t mk_gmtime(uint16_t year, uint8_t mon, uint8_t mday, uint8_t hour, uint8_t min, uint8_t sec);
void gmtime_r(const uint32_t timer, uint16_t *year, uint8_t *mon, uint8_t *mday, uint8_t *hour, uint8_t *min, uint8_t *sec);

#endif // TIME_H
