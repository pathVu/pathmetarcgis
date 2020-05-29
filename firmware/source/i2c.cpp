#include "i2c.hpp"

#include <chrono>

#include "clock_config.h"
#include "fsl_gpio.h"
#include "fsl_i2c.h"
#include "fsl_port.h"

#include "clock.hpp"

I2C i2c1;

void I2C::init()
{
    CLOCK_EnableClock(kCLOCK_PortC);

    bus_reset();

    PORT_SetPinMux(PORTC, 10, kPORT_MuxAlt2);
    PORT_SetPinMux(PORTC, 11, kPORT_MuxAlt2);

    i2c_master_config_t config;
    I2C_MasterGetDefaultConfig(&config);
    config.baudRate_Bps = 400000;

    I2C_MasterInit(I2C1, &config, CLOCK_GetBusClkFreq());
}

bool I2C::write(uint8_t device, uint8_t address, const uint8_t* data, size_t size)
{
    i2c_master_transfer_t transfer;

    transfer.slaveAddress = device;
    transfer.direction = kI2C_Write;
    transfer.subaddress = address;
    transfer.subaddressSize = 1;
    transfer.data = const_cast<uint8_t*>(data);
    transfer.dataSize = size;
    transfer.flags = kI2C_TransferDefaultFlag;

    auto ret = I2C_MasterTransferBlocking(I2C1, &transfer);

    if (ret != kStatus_Success) {
        return false;
    }

    return true;
}

bool I2C::read(uint8_t device, uint8_t address, uint8_t* data, size_t size)
{
    i2c_master_transfer_t transfer;

    transfer.slaveAddress = device;
    transfer.direction = kI2C_Read;
    transfer.subaddress = address;
    transfer.subaddressSize = 1;
    transfer.data = data;
    transfer.dataSize = size;
    transfer.flags = kI2C_TransferDefaultFlag;

    auto ret = I2C_MasterTransferBlocking(I2C1, &transfer);

    if (ret != kStatus_Success) {
        return false;
    }

    return true;
}

void I2C::bus_reset()
{
    PORT_SetPinMux(PORTC, 10, kPORT_MuxAsGpio);
    PORT_SetPinMux(PORTC, 11, kPORT_MuxAsGpio);

    gpio_pin_config_t config = {
        .pinDirection = kGPIO_DigitalInput,
        .outputLogic = 0U
    };

    GPIO_PinInit(GPIOC, 11, &config);

    for (int i = 0; i < 9; ++i) {
        config.pinDirection = kGPIO_DigitalOutput;
        GPIO_PinInit(GPIOC, 10, &config);
        clock::sleep_for(std::chrono::microseconds(10));

        config.pinDirection = kGPIO_DigitalInput;
        GPIO_PinInit(GPIOC, 10, &config);
        clock::sleep_for(std::chrono::microseconds(10));

        if (GPIO_PinRead(GPIOC, 11)) {
            break;
        }
    }

    // issue a stop
    config.pinDirection = kGPIO_DigitalOutput;
    GPIO_PinInit(GPIOC, 11, &config);

    clock::sleep_for(std::chrono::microseconds(10));
    config.pinDirection = kGPIO_DigitalInput;
    GPIO_PinInit(GPIOC, 11, &config);

    clock::sleep_for(std::chrono::microseconds(10));
}
