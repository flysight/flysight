#ifndef MGC_LOG_H
#define MGC_LOG_H

extern uint8_t Log_enable_raw;
extern uint8_t Log_enable_csv;

void Log_WriteRaw(unsigned char ch);
void Log_Flush(void);
void Log_WriteCSV(char *format, ...);

void Log_WriteInt32(int32_t val, int8_t dec);

void Log_Init(uint16_t year, uint8_t month, uint8_t day, 
              uint8_t hour, uint8_t min, uint8_t sec);
uint8_t Log_IsInitialized(void);

#endif
