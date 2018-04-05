/***************************************************************************
**                                                                        **
**  FlySight firmware                                                     **
**  Copyright 2018 Michael Cooper, Tom van Dijck                          **
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

#ifndef MGC_TONE_H
#define MGC_TONE_H

#include <avr/io.h>

#define TONE_RATE_ONE_HZ   65
#define TONE_RATE_FLATLINE UINT16_MAX

#define TONE_LENGTH_125_MS 3906
#define TONE_MAX_PITCH     65280

#define TONE_CHIRP_MAX     (((uint32_t) 3242 << 16) / TONE_LENGTH_125_MS)

extern uint16_t Tone_volume;
extern uint16_t Tone_sp_volume;

void Tone_Init(void);
void Tone_Update(void);

void Tone_SetRate(uint16_t rate);
void Tone_SetPitch(uint16_t index);
void Tone_SetChirp(uint32_t chirp);

void Tone_Task(void);

void Tone_Beep(uint16_t index, uint32_t chirp, uint16_t len);
void Tone_Play(const char *filename);
void Tone_Wait(void);
void Tone_Stop(void);

uint8_t Tone_CanWrite(void);
uint8_t Tone_IsIdle(void);

void Tone_Hold(void);
void Tone_Release(void);

#endif
