#ifndef MGC_TONE_H
#define MGC_TONE_H

#include <avr/io.h>

#define TONE_RATE_ONE_HZ   65
#define TONE_RATE_FLATLINE UINT16_MAX

#define TONE_LENGTH_125_MS 3906
#define TONE_MAX_PITCH     65280

extern volatile uint16_t Tone_volume;

void Tone_Init(void);
void Tone_Update(void);
void Tone_SetRate(uint16_t rate);
void Tone_SetPitch(uint16_t index);
void Tone_Beep(uint16_t pitch, uint16_t length);

#endif
