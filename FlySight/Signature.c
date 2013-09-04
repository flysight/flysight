#include <avr/boot.h>
#include <avr/pgmspace.h>

#include <stdio.h>

#include "FatFS/ff.h"
#include "Main.h"
#include "Config.h"
#include "Signature.h"

void Config_WriteString_P(const char *str, FIL *file);

static const char SignatureHeader[] PROGMEM = "\
FlySight - http://flysight.ca/\r\n\
Processor serial number: ";

static const char SignatureFooter[] PROGMEM = "\
\r\n";

void Signature_WriteString(const char * string)
{
    char c;
    while ((c = pgm_read_byte(string++)))
        f_putc(c, &Main_file);
}

void Signature_WriteHexNibble(char nibble)
{
    if (nibble >= 10)
        f_putc(nibble + 'a' - 10, &Main_file);
    else
        f_putc(nibble + '0', &Main_file);
}

void Signature_Write(void)
{
    FRESULT res;
    
    res = f_open(&Main_file, "flysight.txt", FA_WRITE | FA_CREATE_ALWAYS);
    if (res != FR_OK)
        return;     // ignore failures
    
    Signature_WriteString(SignatureHeader);
    
    uint8_t offset;
    for (offset = 0x0e; offset <= 0x18; offset++) {
        uint8_t byte = boot_signature_byte_get(offset);
        Signature_WriteHexNibble(byte >> 4);
        Signature_WriteHexNibble(byte & 0x0f);
    }

    Signature_WriteString(SignatureFooter);
    
    f_close(&Main_file);
}
