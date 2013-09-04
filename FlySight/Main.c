#include <avr/eeprom.h>
#include <avr/io.h>
#include <avr/wdt.h>
#include <util/delay.h>

#include "Board/LEDs.h"
#include "Lib/MMC.h"
#include "Config.h"
#include "Log.h"
#include "Main.h"
#include "MassStorage.h"
#include "Power.h"
#include "Timer.h"
#include "Tone.h"
#include "UBX.h"
#include "Signature.h"

#define CHARGE_STATUS_DDR  DDRC
#define CHARGE_STATUS_PORT PORTC
#define CHARGE_STATUS_PIN  PINC
#define CHARGE_STATUS_MASK (1 << 3)

#if defined(__AVR_AT90USB1286__) || defined(__AVR_AT90USB1287__)
	#define BOOTLOADER_START_ADDR (0xF000)
#elif defined(__AVR_AT90USB646__) || defined(__AVR_AT90USB647__)
	#define BOOTLOADER_START_ADDR (0x7800)
#endif
#define BOOTLOADER_COUNT_ADDR ((uint8_t *) 0x01)

uint8_t Main_activeLED;

static FATFS    Main_fs;
       FIL      Main_file;
       uint8_t  Main_buffer[MAIN_BUFFER_SIZE];

static uint8_t Main_mmcInitialized;

static void delay_ms(
	uint16_t ms)
{
	while (ms)
	{
		_delay_ms(1);
		--ms;
	}
}

void SetupHardware(void)
{
	MCUSR &= ~(1 << WDRF);
	wdt_disable();

	CLKPR = (1 << CLKPCE);
	CLKPR = 0;

#ifdef TONE_DEBUG
	MCUCR |= (1 << JTD); 
	MCUCR |= (1 << JTD); 
   
	DDRF  = 0xff;
	PORTF = 0x00;
#endif

	USB_Init();
	LEDs_Init();
	
	f_mount(0, &Main_fs);
	Main_mmcInitialized = MMC_Init();
	
	Tone_Init();
}

int main(void)
{
	typedef void (*AppPtr_t) (void);
	AppPtr_t Bootloader = (AppPtr_t) BOOTLOADER_START_ADDR; 

	const uint8_t count = eeprom_read_byte(BOOTLOADER_COUNT_ADDR);

	DDRB |= (1 << 6) | (1 << 5);	// pull audio pins down
	
	if (count == 3)
	{
		eeprom_write_byte(BOOTLOADER_COUNT_ADDR, 0);
		Bootloader();
	}

	SetupHardware();

	eeprom_write_byte(BOOTLOADER_COUNT_ADDR, count + 1);
	delay_ms(500);
	eeprom_write_byte(BOOTLOADER_COUNT_ADDR, 0);

	if (USB_VBUS_GetStatus())
	{
		if (!Main_mmcInitialized)
		{
			USB_Disable();
		}
	
		for (;;)
		{
			CHARGE_STATUS_PORT |= CHARGE_STATUS_MASK ;
			
			if (Main_mmcInitialized)
			{
				MS_Device_USBTask(&Disk_MS_Interface);
				USB_USBTask();
			}
			
			if (CHARGE_STATUS_PIN & CHARGE_STATUS_MASK)
			{
				Main_activeLED = LEDS_GREEN;
			}
			else
			{
				Main_activeLED = LEDS_RED;
			}

			LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);
		}
	}
	else
	{
		USB_Disable();
		
		if (Main_mmcInitialized)
		{
			Main_activeLED = LEDS_GREEN;
		}
		else
		{
			Main_activeLED = LEDS_RED;
		}
		LEDs_ChangeLEDs(LEDS_ALL_LEDS, Main_activeLED);

		Power_Hold();
		Signature_Write();
		Config_Read();
		Power_Release();
		
		Timer_Init();
		UBX_Init();

		for (;;)
		{
			UBX_Task();
			Tone_Task();
		}
	}
}
