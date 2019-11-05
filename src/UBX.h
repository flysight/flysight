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

#ifndef MGC_UBX_H
#define MGC_UBX_H

#include <avr/io.h>

#define UBX_MAX_ALARMS   10
#define UBX_MAX_WINDOWS  2
#define UBX_MAX_SPEECH   3

#define UBX_BUFFER_LEN   150
#define UBX_FILENAME_LEN 13

#define UBX_UNITS_KMH       0
#define UBX_UNITS_MPH       1

#define UBX_UNITS_METERS    0
#define UBX_UNITS_FEET      1

typedef struct
{
	int32_t elev;
	uint8_t type;
	char    filename[9];
}
UBX_alarm_t;

typedef struct
{
	int32_t top;
	int32_t bottom;
}
UBX_window_t;

typedef struct
{
	uint8_t mode;
	uint8_t units;
	int32_t decimals;
}
UBX_speech_t;

typedef struct
{
	char buffer[UBX_BUFFER_LEN - UBX_FILENAME_LEN];
	char filename[UBX_FILENAME_LEN];
}
UBX_buffer_t;

extern uint8_t   UBX_model;
extern uint16_t  UBX_rate;
extern uint8_t   UBX_mode;
extern int32_t   UBX_min;
extern int32_t   UBX_max;

extern uint8_t   UBX_mode_2;
extern int32_t   UBX_min_2;
extern int32_t   UBX_max_2;
extern int32_t   UBX_min_rate;
extern int32_t   UBX_max_rate;
extern uint8_t   UBX_flatline;
extern uint8_t   UBX_limits;
extern uint8_t   UBX_use_sas;

extern int32_t   UBX_threshold;
extern int32_t   UBX_hThreshold;

extern UBX_alarm_t UBX_alarms[UBX_MAX_ALARMS];
extern uint8_t   UBX_num_alarms;
extern int32_t   UBX_alarm_window_above;
extern int32_t   UBX_alarm_window_below;

extern UBX_speech_t UBX_speech[UBX_MAX_SPEECH];
extern uint8_t      UBX_num_speech;
extern uint16_t     UBX_sp_rate;

extern uint8_t   UBX_alt_units;
extern int32_t   UBX_alt_step;

extern uint8_t   UBX_init_mode;
extern char      UBX_init_filename[9];

extern UBX_buffer_t UBX_buffer;

extern UBX_window_t UBX_windows[UBX_MAX_WINDOWS];
extern uint8_t    UBX_num_windows;

extern int32_t    UBX_dz_elev;

void UBX_Init(void);
void UBX_Task(void);
void UBX_Update(void);

#endif
