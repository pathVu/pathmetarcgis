#pragma once

#include <chrono>
#include <cstdint>

#include "clock.hpp"

class GPS {
public:
    GPS();

    void init();
    bool update();

    bool has_fix() { return has_fix_; }

    int get_hour() { return hour_; }
    int get_minute() { return minute_; }
    float get_second() { return second_; }
    
    float get_latitude() { return latitude_; }
    float get_longitude() { return longitude_; }
    float get_altitude() { return altitude_; }

    bool good() { return clock::now() - last_reading_ < std::chrono::seconds(1); }

private:
    static constexpr int buffer_size = 83;

    bool parse(char* p, unsigned int length);
    bool parse_gpgga();

    bool parse_time(const char* p, int& hour, int& minute, float& second);
    bool parse_latitude(const char* latitude_p, const char* north_south_p, float& latitude);
    bool parse_longitude(const char* longitude_p, const char* east_west_p, float& longitude);
    bool parse_altitude(const char* altitude_p, float& altitude);
    
    char buffer_[buffer_size];
    int buffer_index_;

    bool has_fix_;

    int hour_;
    int minute_;
    float second_;
    float latitude_;
    float longitude_;
    float altitude_;

    clock::time_point last_reading_;
};

extern GPS gps;
