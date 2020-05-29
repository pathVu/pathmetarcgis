#include "gps.hpp"

#include "fsl_debug_console.h"
#include "fsl_gpio.h"

#include "uart.hpp"

GPS gps;

GPS::GPS() :
    buffer_index_(0)
{

}

void GPS::init()
{
    // out of reset
    CLOCK_EnableClock(kCLOCK_PortB);
    
    gpio_pin_config_t config {
        kGPIO_DigitalOutput,
        0
    };

    GPIO_PinInit(GPIOB, 9, &config);
}

bool GPS::update()
{
    auto update = false;
    
    while (uart3.read_ready()) {
        uint8_t c;
        uart3.read(&c, 1);

//        printf("%c", c);

        if (c == '\r') {
            buffer_[buffer_index_] = '\0';
            if (parse(buffer_, buffer_index_)) {
                update = true;
            }
            buffer_index_ = 0;
        } else if (c != '\n') {
            if (buffer_index_ == buffer_size) {
                buffer_index_ = 0;
            }
            buffer_[buffer_index_++] = c;
        }
    }

    return update;
}

bool GPS::parse(char* p, unsigned int length)
{
    if (length < 4) {
//        printf("GPS packet too small\n");
        return false;
    }

    if (p[0] != '$') {
        char tmp[100];
        strncpy(tmp, p, length);
        tmp[length] = '\0';
//        printf("GPS packet doesn't start with $: %s\n", tmp);
        return false;
    }

    if (p[length - 3] != '*') {
//        printf("GPS packet doesn't end with *\n");
        return false;
    }

    // calculate the checksum
    uint8_t checksum = 0;
    for (unsigned int i = 1; i < length - 3; ++i) {
        checksum ^= p[i];
    }

    unsigned int message_checksum;
    if (sscanf(&p[length - 2], "%X", &message_checksum) != 1) {
        // can't parse checksum
        return false;
    }

    if (checksum != message_checksum) {
        // checksum mismatch
        return false;
    }

    auto c = strtok(&p[1], ",");
    if (strcmp(c, "GPGGA") == 0) {
        return parse_gpgga();
    }

    return true;
}

bool GPS::parse_gpgga()
{
    auto time_p = strtok(nullptr, ",");
    if (!time_p) {
        return false;
    }
    
    auto latitude_p = strtok(nullptr, ",");
    if (!latitude_p) {
        return false;
    }
    
    auto north_south_p = strtok(nullptr, ",");
    if (!north_south_p) {
        return false;
    }
    
    auto longitude_p = strtok(nullptr, ",");
    if (!longitude_p) {
        return false;
    }
    
    auto east_west_p = strtok(nullptr, ",");
    if (!east_west_p) {
        return false;
    }
    
    auto position_fix_indicator_p = strtok(nullptr, ",");
    if (!position_fix_indicator_p) {
        return false;
    }
    
    auto satellites_used_p = strtok(nullptr, ",");
    if (!satellites_used_p) {
        return false;
    }

    // skip HDOP
    if (!strtok(nullptr, ",")) {
        return false;
    }

    auto altitude_p = strtok(nullptr, ",");
    if (!altitude_p) {
        return false;
    }

    int hour;
    int minute;
    float second;
    if (!parse_time(time_p, hour, minute, second)) {
        return false;
    }

    switch (position_fix_indicator_p[0]) {
    case '0':
        has_fix_ = false;
        return true;
    case '1':
    case '2':
        has_fix_ = true;
        break;
    default:
        return false;
    }
    
    float latitude;
    if (!parse_latitude(latitude_p, north_south_p, latitude)) {
        return false;
    }
    
    float longitude;
    if (!parse_longitude(longitude_p, east_west_p, longitude)) {
        return false;
    }

    float altitude;
    if (!parse_altitude(altitude_p, altitude)) {
        return false;
    }

    has_fix_ = true;
    hour_ = hour;
    minute_ = minute;
    second_ = second;
    latitude_ = latitude;
    longitude_ = longitude;
    altitude_ = altitude;

    last_reading_ = clock::now();
    
    return true;
}

bool GPS::parse_time(const char* p, int& hour, int& minute, float& second)
{
    if (sscanf(p, "%2d", &hour) != 1) {
        return false;
    }

    if (sscanf(p + 2, "%2d", &minute) != 1) {
        return false;
    }

    int i;
    int ms;

    if (sscanf(p + 4, "%2d.%3d", &i, &ms) != 2) {
        return false;
    }

    second = i + (ms / 1000.f);

    return true;
}

bool GPS::parse_latitude(const char* latitude_p, const char* north_south_p, float& latitude)
{
    int d;
    int m;
    int frac_m;

    if (sscanf(latitude_p, "%2d%2d.%4d", &d, &m, &frac_m) != 3) {
        return false;
    }

    latitude = d + (m + frac_m / 10000.f) / 60.f;

    if (north_south_p[0] == 'S') {
        latitude *= -1.f;
    } else if (north_south_p[0] != 'N') {
        return false;
    }

    return true;
}

bool GPS::parse_longitude(const char* longitude_p, const char* east_west_p, float& longitude)
{
    int d;
    int m;
    int frac_m;

    if (sscanf(longitude_p, "%3d%2d.%4d", &d, &m, &frac_m) != 3) {
        return false;
    }

    longitude = d + (m + frac_m / 10000.f) / 60.f;

    if (east_west_p[0] == 'W') {
        longitude *= -1.f;
    } else if (east_west_p[0] != 'E') {
        return false;
    }

    return true;
}

bool GPS::parse_altitude(const char* altitude_p, float& altitude)
{
    return sscanf(altitude_p, "%f", &altitude) == 1;
}
