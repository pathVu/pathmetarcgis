#pragma once

class Stream {
public:
    Stream(uint8_t* data, size_t size) :
        end_(data + size),
        p_(data) {
        
    }

    Stream& operator<<(uint8_t i) {
        if (p_ + 1 <= end_) {
            *p_++ = i;
        }

        return *this;
    }
    
    Stream& operator<<(uint16_t i) {
        if (p_ + 2 <= end_) {
            *p_++ = i & 0xff;
            *p_++ = i >> 8;
        }

        return *this;
    }

    Stream& operator<<(int16_t i) {
        return operator<<(static_cast<uint16_t>(i));
    }

    Stream& operator<<(uint32_t i) {
        if (p_ + 4 <= end_) {
            *p_++ = i & 0xff;
            *p_++ = i >> 8;
            *p_++ = i >> 16;
            *p_++ = i >> 24;
        }

        return *this;
    }

    Stream& operator<<(float f) {
        return operator<<(*reinterpret_cast<uint32_t*>(&f));
    }

private:
    uint8_t* end_;
    uint8_t* p_;
};
