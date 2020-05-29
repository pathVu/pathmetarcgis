#pragma once

#include "fsl_uart.h"

class UART {
public:
    UART(UART_Type* base, uint32_t baudrate, uart_parity_mode_t parity);
    void init();

    size_t read(uint8_t* data, size_t size);
    size_t read_ready();
    
    size_t write(const uint8_t* data, size_t size);
    bool write_complete();

    void set_baud_rate(uint32_t bps);

private:
    void callback(UART_Type* base, uart_handle_t* handle, status_t status);

    static constexpr int buffer_size = 256;

    uint8_t rx_buffer_[buffer_size];
    uint8_t tx_buffer_[buffer_size];
    bool tx_busy_;

    UART_Type* base_;
    uart_handle_t handle_;

    uint32_t baudrate_;
    uart_parity_mode_t parity_;
};

extern UART uart0;
extern UART uart3;
