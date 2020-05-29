#pragma once

#include <cstdint>

#include "circular_buffer.hpp"

extern "C" void pc_on_rx(const uint8_t*, uint32_t);
extern "C" uint32_t pc_tx_pending(void);
extern "C" void pc_get_tx(uint8_t* data, uint32_t size);

class PC {
public:
    PC();
    
    void init();
    void update();

    void write(const uint8_t* data, size_t size);

private:
    friend void pc_on_rx(const uint8_t*, uint32_t);
    friend uint32_t pc_tx_pending(void);
    friend void pc_get_tx(uint8_t*, uint32_t);
    
    /* interface to USB VCOM */
    void on_rx(const uint8_t* data, uint32_t size);

    uint32_t tx_pending();
    void get_tx(uint8_t* data, uint32_t size);

    pa::circular_buffer<uint8_t, 256> tx_buffer;
    pa::circular_buffer<uint8_t, 256> rx_buffer;

    enum class RxState {
        wait_for_start_delimiter,
        read_length,
        read_command,
        read_data,
        read_end_delimiter
    };

    enum class Command : uint8_t {
        start = 0x53
    };

    static const uint8_t start_delimiter = 0x01;
    static const uint8_t end_delimiter = 0x04;

    RxState rx_state_;
    uint8_t message_length_;
    uint8_t message_command_;
    uint8_t message_index_;
};

extern PC pc;
