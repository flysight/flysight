#ifndef FLYSIGHT_DATAFLASH_MANAGER
#define FLYSIGHT_DATAFLASH_MANAGER

#include <LUFA/Drivers/USB/Class/MassStorageClass.h>

#define VIRTUAL_MEMORY_BLOCK_SIZE 512

uint8_t MMC_Init(void);

void MMC_WriteBlocks(
	USB_ClassInfo_MS_Device_t * const MSInterfaceInfo, 
	const uint32_t                    BlockAddress,
	uint16_t                          TotalBlocks);
	
void MMC_ReadBlocks(
	USB_ClassInfo_MS_Device_t * const MSInterfaceInfo, 
	const uint32_t                    BlockAddress,
	uint16_t                          TotalBlocks);
	
bool MMC_CheckDataflashOperation(void);

uint32_t MMC_MediaBlocks(void);

#endif
