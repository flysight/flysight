#ifndef MGC_LOG_H
#define MGC_LOG_H

extern uint8_t Log_enable_raw;
extern uint8_t Log_enable_csv;
extern int16_t Log_tz_offset;

void Log_Flush(void);
void Log_WriteChar(char ch);
char *Log_WriteInt32ToBuf(char *ptr, int32_t val, int8_t dec, int8_t dot, char delimiter);
void Log_WriteInt32(int32_t val, int8_t dec, int8_t dot, char delimiter);

void Log_Init(uint16_t year, uint8_t month, uint8_t day, 
              uint8_t hour, uint8_t min, uint8_t sec);
uint8_t Log_IsInitialized(void);

#endif
