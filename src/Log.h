/***************************************************************************
**                                                                        **
**  FlySight firmware                                                     **
**  Copyright 2018 Michael Cooper, Will Glynn                             **
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

#ifndef MGC_LOG_H
#define MGC_LOG_H

extern uint8_t Log_enable_raw;
extern uint8_t Log_enable_csv;
extern int32_t Log_tz_offset;

void Log_Flush(void);
void Log_WriteChar(char ch);
void Log_WriteString(const char *str);
char *Log_WriteInt32ToBuf(char *ptr, int32_t val, int8_t dec, int8_t dot, char delimiter);

void Log_Init(uint16_t year, uint8_t month, uint8_t day, 
              uint8_t hour, uint8_t min, uint8_t sec);
uint8_t Log_IsInitialized(void);

#endif
