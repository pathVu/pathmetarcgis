#pragma once

#include <cstdint>

#include "clock.hpp"

class Sensors {
public:
    Sensors();
    void start();
    void update();

private:
    static constexpr int laser_threshold = 9;
    static constexpr int imu_threshold = 45;
    static const uint8_t start_delimiter = 0x01;
    static const uint8_t end_delimiter = 0x04;
    
    void send_gps_update();
    void send_update();
    bool should_update_imu();
    bool should_update_laser();
    
    uint32_t last_imu_distance_;
    uint32_t last_laser_distance_;
    clock::time_point last_imu_update_;
    clock::time_point last_laser_update_;
    clock::time_point start_time_;
    
    enum class Flags : uint8_t {
        laser = 0x01,
        imu = 0x02,
        gps = 0x04,
    };
};

extern Sensors sensors;
