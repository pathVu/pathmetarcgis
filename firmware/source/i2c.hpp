#pragma once

#include <cstddef>
#include <cstdint>

class I2C {
public:
    void init();

    bool read(uint8_t device, uint8_t address, uint8_t* data, size_t size);
    bool write(uint8_t device, uint8_t address, const uint8_t* data, size_t size);

private:
    void bus_reset();
};

extern I2C i2c1;
