#ifndef MGC_TIMER_H
#define MGC_TIMER_H

void     Timer_Init(void);
void     Timer_Set(uint16_t ms);
uint16_t Timer_Get(void);
void     Timer_Wait(uint16_t ms);

#endif
