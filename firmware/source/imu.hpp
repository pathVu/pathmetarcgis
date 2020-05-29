#pragma once

#include <cstdint>
#include <memory>

#include "clock.hpp"

struct inv_icm20948;

class IMU {
public:
    struct Reading {
        static constexpr float a_fsr = 4.f;    // 4G
        static constexpr float g_fsr = 2000.f; // 2000 dps
        static constexpr float m_fsr = 4192.f; // 4192 uT
        static constexpr float e_fsr = 360.f;  // 360 degrees
        float ax;
        float ay;
        float az;

        float gx;
        float gy;
        float gz;

        float mx;
        float my;
        float mz;

        float ex;
        float ey;
        float ez;

        int16_t ax16() const { return s16(ax, a_fsr); }
        int16_t ay16() const { return s16(ay, a_fsr); }
        int16_t az16() const { return s16(az, a_fsr); }

        int16_t gx16() const { return s16(gx, g_fsr); }
        int16_t gy16() const { return s16(gy, g_fsr); }
        int16_t gz16() const { return s16(gz, g_fsr); }

        int16_t mx16() const { return s16(mx, m_fsr); }
        int16_t my16() const { return s16(my, m_fsr); }
        int16_t mz16() const { return s16(mz, m_fsr); }

        uint16_t ex16() const { return u16(ex, e_fsr); }
        uint16_t ey16() const { return u16(ey, e_fsr); }
        uint16_t ez16() const { return u16(ez, e_fsr); }

    private:
        int16_t s16(float f, float scale) const {
            return static_cast<int16_t>(f / scale * INT16_MAX);
        }

        uint16_t u16(float f, float scale) const {
            return static_cast<uint16_t>(f / scale * UINT16_MAX);
        }
    };

    IMU();
    void init();

    void trigger();
    bool triggered() { auto t = triggered_; triggered_ = false; return t; }
    void update();
    const Reading& read() const { return reading_; }

    bool good() { return clock::now() - last_reading_ < std::chrono::seconds(1); }

private:
    const uint8_t address = 0x69; // or 0xD2?
    const uint8_t magnetometer = 0x0C; // or 0x18?

    void apply_mounting_matrix();
    void set_fsr();

    std::unique_ptr<struct inv_icm20948> device_;

    bool initialized_;
    Reading temp_reading_;
    Reading reading_;
    clock::time_point last_reading_;
    bool triggered_;
};

extern IMU imu;
