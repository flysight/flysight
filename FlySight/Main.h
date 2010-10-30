#ifndef FLYSIGHT_MAIN_H
#define FLYSIGHT_MAIN_H

#include <stdint.h>

#define ABS(a)   ((a) < 0     ? -(a) : (a))
#define MIN(a,b) (((a) < (b)) ?  (a) : (b))
#define MAX(a,b) (((a) > (b)) ?  (a) : (b))

extern uint8_t Main_activeLED ;

#endif
