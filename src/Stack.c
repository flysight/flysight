// Source: https://www.avrfreaks.net/forum/soft-c-avrgcc-monitoring-stack-usage

#include <stdint.h>

#define STACK_CANARY 0xca

extern uint8_t _end;
extern uint8_t __stack;

__attribute__ ((naked,section (".init3")))
void Stack_Paint(void)
{
#if 0
    uint8_t *p = &_end;

    while (p <= &__stack)
    {
        *p = STACK_CANARY;
        p++;
    }
#else
    __asm volatile ("    ldi r30,lo8(_end)\n"
                    "    ldi r31,hi8(_end)\n"
                    "    ldi r24,lo8(0xca)\n" /* STACK_CANARY = 0xca */
                    "    ldi r25,hi8(__stack)\n"
                    "    rjmp .cmp\n"
                    ".loop:\n"
                    "    st Z+,r24\n"
                    ".cmp:\n"
                    "    cpi r30,lo8(__stack)\n"
                    "    cpc r31,r25\n"
                    "    brlo .loop\n"
                    "    breq .loop"::);
#endif
}

uint16_t Stack_Count(void)
{
    const uint8_t *p = &_end;
    uint16_t       c = 0;

    while (*p == STACK_CANARY && p <= &__stack)
    {
        p++;
        c++;
    }

    return c;
}
