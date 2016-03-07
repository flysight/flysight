#include <avr/io.h>
#include <util/delay.h>

static void Debug_WriteChar(char ch)
{
	uint8_t i;

	// Start bit
	PORTF &= ~(1 << 5);
	_delay_ms(1);

	// Data bits
	for (i = 0; i < 8; ++i)
	{
		if (ch & 0x01)
		{
			PORTF |= (1 << 5);
		}
		else
		{
			PORTF &= ~(1 << 5);
		}
		_delay_ms(1);
		
		ch >>= 1;
	}
	
	// Stop bit
	PORTF |= (1 << 5);
	_delay_ms(1);
}

void Debug_WriteString(const char *str)
{
	char ch;

	while ((ch = *(str++)) != 0)
	{
		Debug_WriteChar(ch);
	}
}

void Debug_Init(void)
{
	// Initialize debugging pins
	MCUCR |= (1 << JTD);
	MCUCR |= (1 << JTD);
	
	DDRF  |= 0x7f;
	PORTF |= 0x7f;
}
