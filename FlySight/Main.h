#ifndef FLYSIGHT_MAIN_H
#define FLYSIGHT_MAIN_H

#include <stdint.h>

#include "FatFS/ff.h"

#define MAIN_BUFFER_SIZE 512

extern uint8_t Main_activeLED;
extern FIL     Main_file;
extern uint8_t Main_buffer[MAIN_BUFFER_SIZE];

#endif
