#include "laser.hpp"

#include "clock_config.h"
#include "fsl_gpio.h"
#include "fsl_port.h"
#include "fsl_uart.h"

#include "uart.hpp"

#include "fsl_debug_console.h"

Laser laser;

Laser::Laser() :
    ready_(false)
{

}

void Laser::init()
{
    CLOCK_EnableClock(kCLOCK_PortC);
    
    gpio_pin_config_t config = {
        .pinDirection = kGPIO_DigitalOutput,
        .outputLogic = 0U
    };
    
    GPIO_PinInit(GPIOC, 5, &config);

    PORT_SetPinMux(PORTC, 5, kPORT_MuxAsGpio);

    config.outputLogic = 1U;
    GPIO_PinInit(GPIOB, 23, &config);
    PORT_SetPinMux(PORTB, 23, kPORT_MuxAsGpio);

    config.outputLogic = 0U;
    GPIO_PinInit(GPIOC, 8, &config);
    PORT_SetPinMux(PORTC, 8, kPORT_MuxAsGpio);

    config.outputLogic = 1U;
    GPIO_PinInit(GPIOC, 9, &config);
    PORT_SetPinMux(PORTC, 9, kPORT_MuxAsGpio);

    reset();
}

void Laser::update()
{
    switch (state_) {
    case State::set_laser_baudrate:
        if (clock::now() > wait_until_) {
            uart0.set_baud_rate(115200U);
            wait_until_ = clock::now() + std::chrono::milliseconds(100);
            state_ = State::set_self_baudrate;
        }
        break;
    case State::set_self_baudrate:
        if (clock::now() > wait_until_) {
            write_register(Register::low_byte_integration_limit, 0x80);
            state_ = State::set_low_byte_sampling_period;
        }
        break;
    case State::set_low_byte_sampling_period:
        if (uart0.write_complete()) {
            write_register(Register::low_byte_sampling_period, 0x01);
            state_ = State::set_high_byte_sampling_period;
        }
        break;
    case State::set_high_byte_sampling_period:
        if (uart0.write_complete()) {
            write_register(Register::high_byte_sampling_period, 0x00);
            state_ = State::set_low_byte_integration_limit;
        }
        break;
    case State::set_low_byte_integration_limit:
        if (uart0.write_complete()) {
            write_register(Register::high_byte_integration_limit, 0x02);
            state_ = State::set_high_byte_integration_limit;
        }
        break;
    case State::set_high_byte_integration_limit:
        if (uart0.write_complete()) {
            write_register(Register::sampling, 0x01);
            state_ = State::set_time_sampling;
        }
        break;
    case State::set_time_sampling:
        if (uart0.write_complete()) {
            static const uint8_t command[2] = { 0x00, 0x87 };
            uart0.write(command, 2);
            state_ = State::enable_stream;
        }
        break;
    case State::enable_stream:
        if (uart0.write_complete()) {
            // throw away any data in the receive buffer
            while (uart0.read_ready()) {
                uint8_t tmp;
                uart0.read(&tmp, 1);
            }
            
            state_ = State::initialized;
        }
        break;
    case State::initialized:
        if (uart0.read_ready() >= 4) {
            uint8_t buffer[4];
            uart0.read(buffer, 4);

            if ((buffer[0] & 0x30) == (buffer[1] & 0x30) &&
                (buffer[0] & 0x30) == (buffer[2] & 0x30) &&
                (buffer[0] & 0x30) == (buffer[3] & 0x30))
            {
                last_reading_ =
                    (buffer[0] & 0x0f) |
                    ((buffer[1] & 0x0f) << 4) |
                    ((buffer[2] & 0x0f) << 8) |
                    ((buffer[3] & 0x0f) << 12);
                
                last_counter_ = (buffer[0] & 0x30) >> 4;
                ready_ = true;
            }
        }
        break;
    default:
        break;
    }
}

void Laser::reset()
{
    enable_power(false);
    clock::sleep_for(std::chrono::milliseconds(100));
    enable_power(true);
    clock::sleep_for(std::chrono::milliseconds(500));

    state_ = State::set_laser_baudrate;
    write_register(Register::baud_rate, 48);

    wait_until_ = clock::now() + std::chrono::milliseconds(10);
}

void Laser::trigger()
{
    ready_ = false;

    while (uart0.read_ready()) {
        uint8_t tmp;
        uart0.read(&tmp, 1);
    }
    
    GPIO_WritePinOutput(GPIOB, 23, 0);
    GPIO_WritePinOutput(GPIOB, 23, 1);
}

void Laser::write_register(Register address, uint8_t value)
{
    uint8_t buffer[6];
    buffer[0] = 0x01;
    buffer[1] = 0x83;
    buffer[2] = 0x80 | (static_cast<uint8_t>(address) & 0x0f);
    buffer[3] = 0x80 | (static_cast<uint8_t>(address) >> 4);
    buffer[4] = 0x80 | (value & 0x0f);
    buffer[5] = 0x80 | (value >> 4);

    uart0.write(buffer, 6);
}

void Laser::enable_power(bool enable)
{
    GPIO_WritePinOutput(GPIOC, 5, enable ? 0 : 1);
}
