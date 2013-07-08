#ifndef MGC_UBX_H
#define MGC_UBX_H

#include <avr/io.h>

#define UBX_MAX_ALARMS 10

typedef struct
{
	int32_t elev;
	uint8_t type;
}
UBX_alarm;

extern uint8_t   UBX_model;
extern uint16_t  UBX_rate;
extern uint8_t   UBX_mode;
extern int32_t   UBX_min;
extern int32_t   UBX_max;

extern uint8_t   UBX_mode_2;
extern int32_t   UBX_min_2;
extern int32_t   UBX_max_2;
extern uint32_t  UBX_min_rate;
extern uint32_t  UBX_max_rate;
extern uint8_t   UBX_flatline;
extern uint8_t   UBX_limits;
extern uint8_t   UBX_use_sas;

extern uint32_t  UBX_threshold;
extern uint32_t  UBX_hThreshold;

extern UBX_alarm UBX_alarms[UBX_MAX_ALARMS];
extern uint8_t   UBX_num_alarms;
extern uint32_t  UBX_alarm_window;

extern uint8_t   UBX_sp_mode;
extern uint8_t   UBX_sp_units;
extern uint16_t  UBX_sp_rate;
extern uint8_t   UBX_sp_decimals;

void UBX_Init(void);
void UBX_Task(void);
void UBX_Update(void);

#endif
