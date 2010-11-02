#include <avr/interrupt.h>
#include <avr/io.h>
#include <util/delay.h>

#include "../FatFS/diskio.h"
#include "../FatFS/ff.h"
#include "MMC.h"

#define GREEN_LED_DDR  DDRC
#define GREEN_LED_PORT PORTC
#define GREEN_LED_MASK (1 << 5)

#define RED_LED_DDR    DDRC
#define RED_LED_PORT   PORTC
#define RED_LED_MASK   (1 << 6)

static uint32_t MMC_mediaBlocks;
static uint8_t  MMC_buffer[VIRTUAL_MEMORY_BLOCK_SIZE];

ISR(TIMER0_COMPA_vect)
{
	disk_timerproc();
}

uint8_t MMC_Init(void)
{
	static FATFS fs;
	DSTATUS stat;

	TCCR0A |= (1 << WGM01);
	TCCR0B |= (1 << CS02) | (1 << CS00);
	OCR0A   = 78;
	TIMSK0 |= (1 << OCIE0A);

	sei();

	DDRB  |= (1 << 2) | (1 << 1) | (1 << 0);
	PORTB |= (1 << 3) | (1 << 2) | (1 << 0);

	SPCR = (1 << SPE) | (1 << MSTR) | (1 << SPR1);
	SPSR = (1 << SPI2X);
	
	f_mount(0, &fs);
	
	stat = disk_initialize(0);
	if (stat & STA_NOINIT)
	{
		return 0;
	}
	else
	{
		disk_ioctl(0, GET_SECTOR_COUNT, &MMC_mediaBlocks);
		return 1;
	}
}

void MMC_WriteBlocks(
	USB_ClassInfo_MS_Device_t *MSInterfaceInfo, 
	uint32_t                  BlockAddress, 
	uint16_t                  TotalBlocks)
{
	uint8_t  CurrDFPageByteDiv16 = 0;

	/* Wait until endpoint is ready before continuing */
	if (Endpoint_WaitUntilReady()) return;

	while (TotalBlocks)
	{
		uint8_t BytesInBlockDiv16 = 0;
		uint8_t *buf = MMC_buffer;
		
		/* Write an endpoint packet sized data block to the dataflash */
		while (BytesInBlockDiv16 < (VIRTUAL_MEMORY_BLOCK_SIZE >> 4))
		{
			/* Check if the endpoint is currently empty */
			if (!(Endpoint_IsReadWriteAllowed()))
			{
				/* Clear the current endpoint bank */
				Endpoint_ClearOUT();
				
				/* Wait until the host has sent another packet */
				if (Endpoint_WaitUntilReady()) return;
			}

			/* Write one 16-byte chunk of data to the dataflash */
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			*(buf++) = Endpoint_Read_Byte();
			
			/* Increment the dataflash page 16 byte block counter */
			CurrDFPageByteDiv16++;

			/* Increment the block 16 byte block counter */
			BytesInBlockDiv16++;

			/* Check if the current command is being aborted by the host */
			if (MSInterfaceInfo->State.IsMassStoreReset) return;			
		}

		/* Write a single sector */
		disk_write(0, MMC_buffer, BlockAddress, 1);
			
		/* Increment the block address */
		BlockAddress++;
			
		/* Decrement the blocks remaining counter and reset the sub block counter */
		TotalBlocks--;
	}

	/* If the endpoint is empty, clear it ready for the next packet from the host */
	if (!(Endpoint_IsReadWriteAllowed()))
	{
		Endpoint_ClearOUT();
	}
}

void MMC_ReadBlocks(
	USB_ClassInfo_MS_Device_t *MSInterfaceInfo, 
	uint32_t                  BlockAddress, 
	uint16_t                  TotalBlocks)
{
	uint8_t  CurrDFPageByteDiv16 = 0;

	/* Wait until endpoint is ready before continuing */
	if (Endpoint_WaitUntilReady()) return;
	
	while (TotalBlocks)
	{
		uint8_t BytesInBlockDiv16 = 0;
		uint8_t *buf = MMC_buffer;
		
		/* read a single sector */
		disk_read(0, MMC_buffer, BlockAddress, 1);
			
		/* Increment the block address */
		BlockAddress++;
			
		/* Write an endpoint packet sized data block to the dataflash */
		while (BytesInBlockDiv16 < (VIRTUAL_MEMORY_BLOCK_SIZE >> 4))
		{
			/* Check if the endpoint is currently full */
			if (!(Endpoint_IsReadWriteAllowed()))
			{
				/* Clear the endpoint bank to send its contents to the host */
				Endpoint_ClearIN();
				
				/* Wait until the endpoint is ready for more data */
				if (Endpoint_WaitUntilReady()) return;
			}

			/* Read one 16-byte chunk of data from the dataflash */
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			Endpoint_Write_Byte(*(buf++));
			
			/* Increment the dataflash page 16 byte block counter */
			CurrDFPageByteDiv16++;
			
			/* Increment the block 16 byte block counter */
			BytesInBlockDiv16++;

			/* Check if the current command is being aborted by the host */
			if (MSInterfaceInfo->State.IsMassStoreReset) return;
		}
		
		/* Decrement the blocks remaining counter */
		TotalBlocks--;
	}
	
	/* If the endpoint is full, send its contents to the host */
	if (!(Endpoint_IsReadWriteAllowed()))
	{
		Endpoint_ClearIN();
	}
}

bool MMC_CheckDataflashOperation(void)
{
	return true;
}

uint32_t MMC_MediaBlocks(void)
{
	return MMC_mediaBlocks;
}
