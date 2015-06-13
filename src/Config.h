#ifndef MGC_CONFIG_H
#define MGC_CONFIG_H

#define CONFIG_FNAME_ADDR ((void *) 0x02)
#define CONFIG_FNAME_LEN  (13)

extern const char Config_Init_File[];

extern char Config_buf[80];

void Config_Read(void);

#endif
