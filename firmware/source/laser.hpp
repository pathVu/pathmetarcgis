#pragma once

#include <cstdint>

#include "clock.hpp"

class Laser {
public:
    Laser();
    
    void init();
    void update();

    void reset();
    void trigger();

    uint16_t read() const { return last_reading_; }
    uint8_t count() const { return last_counter_; }
    bool ready() const { return ready_; }

    bool initialized() const { return state_ == State::initialized; }
private:
    enum class Register : uint8_t {
        sampling = 0x02,
        baud_rate = 0x04,
        low_byte_sampling_period = 0x08,
        high_byte_sampling_period = 0x09,
        low_byte_integration_limit = 0x0A,
        high_byte_integration_limit = 0x0B
    };

    void enable_power(bool enable);
    void write_register(Register address, uint8_t value);
    
    enum class State {
        set_laser_baudrate,
        set_self_baudrate,
        set_low_byte_integration_limit,
        set_high_byte_integration_limit,
        set_low_byte_sampling_period,
        set_high_byte_sampling_period,
        set_time_sampling,
        enable_stream,
        initialized
    } state_;

    clock::time_point wait_until_;
    uint16_t last_reading_;
    uint8_t last_counter_;
    bool ready_;
};

extern Laser laser;
