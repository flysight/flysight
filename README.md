![FlySight logo](http://flysight.ca/images/flysight.jpg)

# FlySight Firmware [![Build Status](https://travis-ci.org/flysight/flysight.png)](https://travis-ci.org/flysight/flysight)

[FlySight](http://flysight.ca/) is an audible GPS intended to give wingsuit pilots real-time feedback on statistics like glide ratio. The FlySight firmware is open-source and lives in this repository.

## Hardware

![](http://flysight.ca/images/support.jpg)

FlySight uses an Atmel [AT90USB646](http://www.atmel.com/devices/at90usb646.aspx) microcontroller clocked at 8 MHZ. This chip has 64 KB of flash memory, 4 KB of RAM, an onboard USB PHY, and enough I/O to connect everything else. Other components of interest include:

* the GPS module (a [u-blox NEO-6Q](http://www.u-blox.com/en/gps-modules/pvt-modules/previous-generations/neo-6-family.html) or [NEO-7N](https://www.u-blox.com/en/gps-modules/pvt-modules/neo-7.html)),
* a micro-SD card connected via SPI (512 MB as shipped),
* a red and a green LED,
* a lithium polymer battery and charging circuit,
* an override-able power switch (the MCU can keep itself on even as the switch turns off), and
* an audio output system driven by the MCU's PWM support.

There is a [schematic](http://flysight.ca/wiki/images/1/1b/Schematic.png) if you care to see how everything is connected.

## Structure

The FlySight code lives in `FlySight/`.

The firmware uses LUFA as a submodule in `vendor/lufa/` for USB access. It also uses FatFS as a copy in `FlySight/FatFS/` for FAT16.

## Building

The microcontroller is in the Atmel AVR family. As such, building the firmware requires `avr-gcc`.

On UNIXy systems, install an AVR toolchain and run `make`. Some suggestions for getting a suitable toolchain:

* Ubuntu Linux: `apt-get install build-essential gcc-avr binutils-avr avr-libc`
* Mac OS X: check out [CrossPack](http://www.obdev.at/products/crosspack/), use `homebrew` to tap e.g. [homebrew-avr](https://github.com/larsimmisch/homebrew-avr)/[homebrew-embedded](https://github.com/darconeous/homebrew-embedded), or use the binaries embedded in the [Arduino IDE](http://arduino.cc/en/Main/Software#toc1)

On Windows, the WinAVR toolchain is recommended:

* [WinAVR](http://winavr.sourceforge.net/index.html)

## Contributing

1. [Fork the project](https://help.github.com/articles/fork-a-repo)
2. Update submodules (`git submodule init` and `git submodule update`)
3. Create a feature branch (`git checkout -b shiny_new_feature`)
4. Develop
5. Commit your changes (`git commit -a`, being sure to give a useful message)
6. Push to your branch (`git push origin shiny_new_feature`)
7. [Create a pull request](https://help.github.com/articles/creating-a-pull-request)
