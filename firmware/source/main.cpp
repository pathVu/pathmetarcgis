/*
 * Copyright 2016-2018 NXP Semiconductor, Inc.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 * o Redistributions of source code must retain the above copyright notice, this list
 *   of conditions and the following disclaimer.
 *
 * o Redistributions in binary form must reproduce the above copyright notice, this
 *   list of conditions and the following disclaimer in the documentation and/or
 *   other materials provided with the distribution.
 *
 * o Neither the name of NXP Semiconductor, Inc. nor the names of its
 *   contributors may be used to endorse or promote products derived from this
 *   software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
 * ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
 * ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
 
/**
 * @file    firmware.cpp
 * @brief   Application entry point.
 */
#include <stdio.h>
#include "board.h"
#include "peripherals.h"
#include "pin_mux.h"
#include "clock_config.h"
#include "MK64F12.h"
#include "fsl_debug_console.h"
/* TODO: insert other include files here. */

#include "fsl_gpio.h"
#include "fsl_port.h"

#include "encoder.hpp"
#include "i2c.hpp"
#include "imu.hpp"
#include "gps.hpp"
#include "laser.hpp"
#include "pc.hpp"
#include "sensors.hpp"
#include "uart.hpp"

/* TODO: insert other definitions and declarations here. */

void enable_level_shifter()
{
    CLOCK_EnableClock(kCLOCK_PortE);

    gpio_pin_config_t config {
        kGPIO_DigitalOutput,
        0
    };
    
    GPIO_PinInit(GPIOE, 25, &config);

    PORT_SetPinMux(PORTE, 25, kPORT_MuxAsGpio);
}

/*
 * @brief   Application entry point.
 */
int main(void) {

  	/* Init board hardware. */
    BOARD_InitBootPins();
    BOARD_InitBootClocks();
    BOARD_InitBootPeripherals();

    // do NOT initialize the debug console, we need it for laser

    PRINTF("Hello World\n");

    enable_level_shifter();
    
    uart0.init();
    i2c1.init();
    
    encoder.init();
    laser.init();
    pc.init();

    clock::sleep_for(std::chrono::milliseconds(10));
    imu.init();

    uart3.init();
    gps.init();

    while(1) {
        imu.update();
        encoder.update();
        laser.update();
        sensors.update();
        pc.update();
    }
}
