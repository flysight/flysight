# ****************************************************************************
# **                                                                        **
# **  FlySight firmware                                                     **
# **  Copyright 2018 Michael Cooper, Tom van Dijck, Will Glynn              **
# **  Original makefile copyright 2013 Dean Camera                          **
# **                                                                        **
# **  This program is free software: you can redistribute it and/or modify  **
# **  it under the terms of the GNU General Public License as published by  **
# **  the Free Software Foundation, either version 3 of the License, or     **
# **  (at your option) any later version.                                   **
# **                                                                        **
# **  This program is distributed in the hope that it will be useful,       **
# **  but WITHOUT ANY WARRANTY; without even the implied warranty of        **
# **  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         **
# **  GNU General Public License for more details.                          **
# **                                                                        **
# **  You should have received a copy of the GNU General Public License     **
# **  along with this program.  If not, see <http://www.gnu.org/licenses/>. **
# **                                                                        **
# ****************************************************************************
# **  Contact: Michael Cooper                                               **
# **  Website: http://flysight.ca/                                          **
# ****************************************************************************

# Define FLYSIGHT_VERSION automatically from source control
VERSION_OPT = -DFLYSIGHT_VERSION='"$(shell git describe --abbrev=6 --dirty --always)"'

# Run "make help" for target help.

MCU          = at90usb646
ARCH         = AVR8
BOARD        = USER
F_CPU        = 8000000
F_USB        = $(F_CPU)
OPTIMIZATION = s
TARGET       = flysight
SRC          = src/Main.c                                                  \
	           src/Config.c                                                \
	           src/Debug.c                                                 \
	           src/Descriptors.c                                           \
	           src/Log.c                                                   \
	           src/Power.c                                                 \
	           src/Signature.c                                             \
	           src/Stack.c                                                 \
	           src/Time.c                                                  \
	           src/Timer.c                                                 \
	           src/Tone.c                                                  \
	           src/uart.c                                                  \
	           src/UBX.c                                                   \
	           src/UsbInterface.c                                          \
	           src/Lib/MMC.c                                               \
	           src/Lib/SCSI.c                                              \
	           vendor/FatFS/ff.c                                           \
	           vendor/FatFS/mmc.c                                          \
	           $(LUFA_SRC_USB)                                             \
	           $(LUFA_SRC_USBCLASS) 
LUFA_PATH    = vendor/lufa/LUFA
CC_FLAGS     = -DUSE_LUFA_CONFIG_HEADER -Isrc -Isrc/Config/ -Ivendor -fdata-sections $(VERSION_OPT)
LD_FLAGS     =

# Default target
all:

# Include LUFA build script makefiles
include $(LUFA_PATH)/Build/lufa_core.mk
include $(LUFA_PATH)/Build/lufa_sources.mk
include $(LUFA_PATH)/Build/lufa_build.mk
include $(LUFA_PATH)/Build/lufa_cppcheck.mk
include $(LUFA_PATH)/Build/lufa_doxygen.mk
include $(LUFA_PATH)/Build/lufa_dfu.mk
include $(LUFA_PATH)/Build/lufa_hid.mk
include $(LUFA_PATH)/Build/lufa_avrdude.mk
include $(LUFA_PATH)/Build/lufa_atprogram.mk

# Target to pack the build results into a single .zip
build.zip: version.txt $(TARGET).bin $(TARGET).eep $(TARGET).elf $(TARGET).hex $(TARGET).lss $(TARGET).map $(TARGET).sym $(TARGET).size.txt
	zip -9q $@ $^

version.txt:
	(git describe --always --dirty; git rev-parse HEAD) > $@

$(TARGET).size.txt: $(TARGET).elf
	$(CROSS)-size --mcu=$(MCU) --format=avr $< > $@
