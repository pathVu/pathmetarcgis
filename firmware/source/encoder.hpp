#pragma once

#include <cstdint>

class Encoder {
public:
    Encoder();
    
    void init();
    void update();
    uint32_t get() { return count_; }
    void set(uint32_t count) { count_ = count; }

private:
    uint16_t last_;
    uint32_t count_;
};

extern Encoder encoder;
