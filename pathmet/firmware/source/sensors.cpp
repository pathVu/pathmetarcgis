#include "sensors.hpp"

#include <algorithm>

#include "encoder.hpp"
#include "gps.hpp"
#include "imu.hpp"
#include "laser.hpp"
#include "pc.hpp"
#include "stream.hpp"

Sensors sensors;

Sensors::Sensors() :
    last_imu_distance_(0),
    last_laser_distance_(0),
    last_imu_update_{ clock::time_point { clock::duration { 0 } } },
    last_laser_update_{ clock::time_point { clock::duration { 0 } } }
{

}

void Sensors::update()
{
    using namespace std::chrono;
    auto now = clock::now();
    
    auto update_gps = gps.update();
    
    auto distance = encoder.get();

    auto update_laser = (now >= last_laser_update_ + microseconds(500) &&
                         distance > last_laser_distance_ &&
                         distance - last_laser_distance_ >= laser_threshold) ||
        now > last_laser_update_ + milliseconds(100);
    
    auto update_imu = (now >= last_imu_update_ + microseconds(2500) &&
                       distance > last_imu_distance_ &&
                       distance - last_imu_distance_ >= imu_threshold) ||
        now > last_imu_update_ + milliseconds(100);
    
    if (update_laser) {
        last_laser_update_ = now;

        if (update_imu) {
            last_imu_update_ = now;
        }

        // send the last reading
        send_update();
        
        last_laser_distance_ = distance;
        laser.trigger();

        if (update_imu) {
            last_imu_distance_ = distance;
            imu.trigger();
        }
    }

    if (update_gps) {
        send_gps_update();
    }
}

void Sensors::start()
{
    last_laser_distance_ = 0;
    last_imu_distance_ = 0;
    encoder.set(0);

    start_time_ = clock::now();
}

void Sensors::send_gps_update()
{
    static constexpr uint8_t length = 21; // not including delimiters
    static constexpr uint8_t command = 0x47; // version 1 GPS message

    uint8_t buffer[length + 2];

    std::fill_n(buffer, length + 2, '\0');

    Stream s(buffer, length + 2);

    s << start_delimiter
      << length
      << command

      << static_cast<uint8_t>(gps.has_fix() ? 1 : 0)

      << gps.get_latitude()
      << gps.get_longitude()
      << gps.get_altitude()

      << static_cast<uint8_t>(gps.get_hour())
      << static_cast<uint8_t>(gps.get_minute())
      << gps.get_second()

      << end_delimiter;

    pc.write(buffer, length + 2);
}

void Sensors::send_update()
{
    static constexpr uint8_t base_length = 13; // not including delimiters
    static constexpr uint8_t imu_data_length = 24;
    static constexpr uint8_t command = 0x55; // version 1 update message

    uint8_t buffer[base_length + imu_data_length + 2];

    auto include_imu = imu.triggered();

    auto length = base_length;
    if (include_imu) {
        length += imu_data_length;
    }

    std::fill_n(buffer, length + 2, '\0');
    
    Stream s(buffer, length + 2);

    const IMU::Reading& imu_reading = imu.read();

    uint32_t timestamp = std::chrono::duration_cast<std::chrono::milliseconds>(clock::now() - start_time_).count();

    uint8_t flags = 0;
    if (laser.ready()) {
        flags |= static_cast<uint8_t>(Flags::laser);
    }

    if (imu.good()) {
        flags |= static_cast<uint8_t>(Flags::imu);
    }

    if (gps.good()) {
        flags |= static_cast<uint8_t>(Flags::gps);
    }

    s << start_delimiter
      << length
      << command

      << flags

      << timestamp
        
      << last_laser_distance_
        
      << laser.read();

    if (include_imu) {
        s << imu_reading.ax16()
          << imu_reading.ay16()
          << imu_reading.az16()
            
          << imu_reading.gx16()
          << imu_reading.gy16()
          << imu_reading.gz16()
            
          << imu_reading.mx16()
          << imu_reading.my16()
          << imu_reading.mz16()
            
          << imu_reading.ex16()
          << imu_reading.ey16()
          << imu_reading.ez16();
    }
    
    s << end_delimiter;

    pc.write(buffer, length + 2);
}


