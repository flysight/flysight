#ifndef TIME_H
#define TIME_H

#include <stdint.h>

uint32_t mk_gmtime(uint16_t year, uint8_t mon, uint8_t mday, uint8_t hour, uint8_t min, uint8_t sec);
void gmtime_r(const uint32_t timer, uint16_t *year, uint8_t *mon, uint8_t *mday, uint8_t *hour, uint8_t *min, uint8_t *sec);

#endif // TIME_H
