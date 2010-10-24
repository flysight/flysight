#include <avr/io.h>

#define PWR_HOLD_DDR   DDRC
#define PWR_HOLD_PORT  PORTC
#define PWR_HOLD_MASK  (1 << 2)

void Power_Hold(void)
{
	PWR_HOLD_DDR  |= PWR_HOLD_MASK;
	PWR_HOLD_PORT |= PWR_HOLD_MASK;
}

void Power_Release(void)
{
	PWR_HOLD_DDR  |=  PWR_HOLD_MASK;
	PWR_HOLD_PORT &= ~PWR_HOLD_MASK;
}
