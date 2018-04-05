/***************************************************************************
**                                                                        **
**  FlySight firmware                                                     **
**  Copyright 2018 Michael Cooper                                         **
**  MMC interface copyright 2013 Dean Camera                              **
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
