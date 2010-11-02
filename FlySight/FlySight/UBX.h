#ifndef MGC_UBX_H
#define MGC_UBX_H

#include <avr/io.h>

extern uint8_t  UBX_model;
extern uint16_t UBX_rate;
extern uint8_t  UBX_mode;
extern uint32_t UBX_min;
extern uint32_t UBX_max;
extern uint32_t UBX_threshold;

extern uint8_t  UBX_mode_2;
extern uint32_t UBX_min_2;
extern uint32_t UBX_max_2;
extern uint32_t UBX_min_rate;
extern uint32_t UBX_max_rate;
extern uint8_t  UBX_flatline;

extern uint32_t UBX_sAccThreshold;

void UBX_Init(void);
void UBX_Task(void);
void UBX_Update(void);

#endif
