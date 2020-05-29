#include "uart.hpp"

#include <algorithm>

#include "clock_config.h"
#include "fsl_port.h"
#include "fsl_uart.h"

#include "fsl_debug_console.h"

UART uart0(UART0, 9600U, kUART_ParityEven);
UART uart3(UART3, 9600U, kUART_ParityDisabled);

constexpr int UART::buffer_size;

UART::UART(UART_Type* base, uint32_t baudrate, uart_parity_mode_t parity) :
    tx_busy_(false),
    base_(base),
    baudrate_(baudrate),
    parity_(parity)
{
    
}

void UART::init()
{
    if (base_ == UART0) {
        CLOCK_EnableClock(kCLOCK_PortA);
        PORT_SetPinMux(PORTA, 1, kPORT_MuxAlt2);
        PORT_SetPinMux(PORTA, 2, kPORT_MuxAlt2);
    } else if (base_ == UART3) {
        CLOCK_EnableClock(kCLOCK_PortC);
        PORT_SetPinMux(PORTC, 16, kPORT_MuxAlt3);
        PORT_SetPinMux(PORTC, 17, kPORT_MuxAlt3);
    }

    uart_config_t config;
    UART_GetDefaultConfig(&config);
    config.baudRate_Bps = baudrate_;
    config.parityMode = parity_;
    config.enableTx = true;
    config.enableRx = true;

    if (base_ == UART0 || base_ == UART1) {
        UART_Init(base_, &config, CLOCK_GetCoreSysClkFreq());
    } else {
        UART_Init(base_, &config, CLOCK_GetBusClkFreq());
    }

    UART_TransferCreateHandle(base_, &handle_,
                              [](UART_Type* base, uart_handle_t* handle, status_t status, void* userData) {
                                  reinterpret_cast<UART*>(userData)->callback(base, handle, status);
                              },
                              this);

    UART_TransferStartRingBuffer(base_, &handle_, rx_buffer_, buffer_size);
}

size_t UART::read(uint8_t* data, size_t size)
{
    // don't read more than is available; this should ensure that transfer
    // being stack allocated isn't an issue
    uart_transfer_t transfer;
    transfer.data = data;
    transfer.dataSize = std::min(size, UART_TransferGetRxRingBufferLength(&handle_));

    UART_TransferReceiveNonBlocking(base_, &handle_, &transfer, &size);

    return size;
}

size_t UART::read_ready()
{
    return UART_TransferGetRxRingBufferLength(&handle_);
}

size_t UART::write(const uint8_t* data, size_t size)
{
    if (tx_busy_) {
        return 0;
    }
    
    if (size > buffer_size) {
        size = buffer_size;
    }

    std::copy(data, data + size, tx_buffer_);

    uart_transfer_t transfer;
    transfer.data = tx_buffer_;
    transfer.dataSize = size;
    UART_TransferSendNonBlocking(base_, &handle_, &transfer);

    return size;
}

bool UART::write_complete()
{
    return UART_GetStatusFlags(base_) & kUART_TransmissionCompleteFlag;
}

void UART::set_baud_rate(uint32_t bps)
{
    UART_SetBaudRate(base_, bps, CLOCK_GetCoreSysClkFreq());
}

void UART::callback(UART_Type* base, uart_handle_t* handle, status_t status)
{
    int c = '?';
    if (base == UART0) {
        c = '0';
    } else if (base == UART3) {
        c = '3';
    }
    
    if (status == kStatus_UART_TxIdle) {
        tx_busy_ = false;
    }

    if (status == kStatus_UART_RxRingBufferOverrun) {
        printf("RBO%c\n", c);
    }

    if (status == kStatus_UART_NoiseError) {
        printf("NE%c\n", c);
    }

    if (status == kStatus_UART_FramingError) {
        printf("FE%c\n", c);
    }

    if (status == kStatus_UART_RxHardwareOverrun) {
        printf("HWO%c\n", c);
    }
}
