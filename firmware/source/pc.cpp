#include "pc.hpp"

#include "sensors.hpp"

extern "C" void APPInit(void);
extern "C" void APPTask(void);

PC pc;

extern "C" void pc_on_rx(const uint8_t* data, uint32_t size)
{
    pc.on_rx(data, size);
}

extern "C" uint32_t pc_tx_pending()
{
    return pc.tx_pending();
}

extern "C" void pc_get_tx(uint8_t* data, uint32_t size)
{
    pc.get_tx(data, size);
}

PC::PC() :
    rx_state_(RxState::wait_for_start_delimiter)
{

}

void PC::init()
{
    APPInit();
}

void PC::update()
{
    APPTask();
    while (rx_buffer.size()) {
        uint8_t c = rx_buffer.front();
        rx_buffer.pop_front();

        switch (rx_state_) {
        case RxState::wait_for_start_delimiter:
            if (c == start_delimiter) {
                rx_state_ = RxState::read_length;
                message_index_ = 0;
            }
            break;
        case RxState::read_length:
            if (c >= 2) {
                message_length_ = c - 2;
                rx_state_ = RxState::read_command;
            } else {
                rx_state_ = RxState::wait_for_start_delimiter;
            }
            break;
        case RxState::read_command:
            message_command_ = c;
            if (message_length_) {
                rx_state_ = RxState::read_data;
            } else {
                rx_state_ = RxState::read_end_delimiter;
            }
            break;
        case RxState::read_data:
            // we don't read any data with commands yet, just discard
            ++message_index_;
            if (message_index_ == message_length_) {
                rx_state_ = RxState::read_end_delimiter;
            }
            break;
        case RxState::read_end_delimiter:
            if (c == end_delimiter) {
                switch(message_command_) {
                case static_cast<uint8_t>(Command::start):
                    sensors.start();
                    break;
                default:
                    break;
                }
            }
            rx_state_ = RxState::wait_for_start_delimiter;
            break;
        }
    }
}

void PC::write(const uint8_t* data, size_t size)
{
    while (size--) {
        tx_buffer.push_back(*data++);
    }

    if (tx_buffer.full()) {
//        printf("PCTXF\n");
    }
}

void PC::on_rx(const uint8_t* data, uint32_t size)
{
    while (size--) {
        rx_buffer.push_back(*data);
        ++data;
    }
}

uint32_t PC::tx_pending()
{
    return tx_buffer.size();
}

void PC::get_tx(uint8_t* data, uint32_t size)
{
    while (size--) {
        *data = tx_buffer.front();
        tx_buffer.pop_front();
        ++data;
    }
}

/*void PC::puts(const char* s)
{
    while (*s) {
        tx_buffer.push_back(*s++);
    }
}
*/
