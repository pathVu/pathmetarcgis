#include "clock.hpp"

#include "fsl_pit.h"

static uint32_t sec = 0;

clock::time_point clock::now() noexcept {
    uint64_t us = 1000000U - static_cast<uint64_t>(PIT_GetCurrentTimerCount(PIT, kPIT_Chnl_1)) + static_cast<uint64_t>(sec) * 1000000U;
    return time_point { duration { us } };
}

extern "C" void PIT1_IRQHandler(void)
{
    if (PIT_GetStatusFlags(PIT, kPIT_Chnl_1) & kPIT_TimerFlag) {
        PIT_ClearStatusFlags(PIT, kPIT_Chnl_1, kPIT_TimerFlag);
        ++sec;
    }
}
