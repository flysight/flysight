/***************************************************************************
**                                                                        **
**  FlySight firmware                                                     **
**  Copyright 2018 Michael Cooper, Will Glynn                             **
**                                                                        **
**  This program is free software: you can redistribute it and/or modify  **
**  it under the terms of the GNU General Public License as published by  **
**  the Free Software Foundation, either version 3 of the License, or     **
**  (at your option) any later version.                                   **
**                                                                        **
**  This program is distributed in the hope that it will be useful,       **
**  but WITHOUT ANY WARRANTY; without even the implied warranty of        **
**  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         **
**  GNU General Public License for more details.                          **
**                                                                        **
**  You should have received a copy of the GNU General Public License     **
**  along with this program.  If not, see <http://www.gnu.org/licenses/>. **
**                                                                        **
****************************************************************************
**  Contact: Michael Cooper                                               **
**  Website: http://flysight.ca/                                          **
****************************************************************************/

#include <avr/eeprom.h>
#include <avr/io.h>
#include <avr/wdt.h>
#include <util/delay.h>

#include "Board/LEDs.h"
#include "Lib/MMC.h"
#include "Config.h"
#include "Log.h"
#include "Main.h"
#include "Power.h"
#include "Signature.h"
#include "Timer.h"
#include "Tone.h"
#include "uart.h"
#include "UBX.h"
#include "UsbInterface.h"

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

	USB_Init();
	LEDs_Init();
	
	f_mount(0, &Main_fs);
	Main_mmcInitialized = MMC_Init();
	
	Tone_Init();
}

static void ReadSingleConfigName(
	char *fname)
{
	FRESULT res;

	size_t  len;
	char    *name;
	char    *result;
	
	res = f_chdir("\\config");
	res = f_open(&Main_file, fname, FA_READ);
	if (res != FR_OK) return;

	while (!f_eof(&Main_file))
	{
		f_gets(UBX_buffer.buffer, sizeof(UBX_buffer.buffer), &Main_file);

		len = strcspn(UBX_buffer.buffer, ";");
		UBX_buffer.buffer[len] = 0;
		
		name = strtok(UBX_buffer.buffer, " \t:");
		if (name == 0) continue ;
		
		result = strtok(0, " \t:");
		if (result == 0) continue ;
		
		if (!strcmp_P(name, Config_Init_File))
		{
			eeprom_write_block(fname, CONFIG_FNAME_ADDR, CONFIG_FNAME_LEN);
			
			strcpy(UBX_buffer.filename, result);
			strcat(UBX_buffer.filename, ".wav");

			Power_Hold();
			Tone_Hold();
			
			Tone_Play(UBX_buffer.filename);
			Tone_Wait();
			
			Tone_Release();
			Power_Release();
			
			delay_ms(500);
		}
	}	

	f_close(&Main_file);
}

static void ReadConfigNames(void)
{
	FRESULT res;
	DIR dir;
    FILINFO fno;

	res = f_opendir(&dir, "\\config");
	if (res == FR_OK)
	{
		for (;;)
		{
			res = f_readdir(&dir, &fno);
			
			if (res != FR_OK || fno.fname[0] == 0) break;
			if (fno.fname[0] == '.') continue;
			if (fno.fattrib & AM_DIR) continue;

			ReadSingleConfigName(fno.fname);
		}
	}

	eeprom_write_block("", CONFIG_FNAME_ADDR, CONFIG_FNAME_LEN);
}

static void ReadInitFile(void)
{
	uint8_t i;

	Power_Hold();
	Tone_Hold();
	
	if (UBX_init_mode == 1)			// Speech test
	{
		for (i = 0; i < 10; ++i)
		{
			UBX_buffer.filename[0] = i + '0';
			UBX_buffer.filename[1] = 0;
			strcat(UBX_buffer.filename, ".wav");

			Tone_Play(UBX_buffer.filename);
			Tone_Wait();
		}

		Tone_Play("dot.wav");
		Tone_Wait();

		Tone_Play("minus.wav");
		Tone_Wait();
	}
	else if (UBX_init_mode == 2)	// Play a file
	{
		strcpy(UBX_buffer.filename, UBX_init_filename);
		strcat(UBX_buffer.filename, ".wav");

		Tone_Play(UBX_buffer.filename);
		Tone_Wait();
	}
	
	Tone_Release();
	Power_Release();
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
		
		uart_init(12);
		
		for (;;)
		{
			CHARGE_STATUS_PORT |= CHARGE_STATUS_MASK ;
			
			if (Main_mmcInitialized)
			{
				USBInterfaceTask();
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

		if (count == 1)
		{
			ReadConfigNames();
		}
		
		Power_Hold();
		Signature_Write();
		Config_Read();
		Power_Release();
				
		ReadInitFile();
		
		Timer_Init();
		UBX_Init();

		for (;;)
		{
			UBX_Task();
			Tone_Task();
		}
	}
}
