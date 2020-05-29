#ifndef IMU_HPP
#define IMU_HPP

#include <chrono>

#include <boost/asio.hpp>

#include "sensor_status.hpp"

namespace imu_status_bits {
enum {
    HEAD  = 0x40,
    EULER = 0x10,
    MAG   = 0x08,
    QUAT  = 0x04,
    GYRO  = 0x02,
    ACC   = 0x01
};
}

class IMU : public std::enable_shared_from_this<IMU> {
public:
    typedef struct {
        uint32_t counter;
        float x;
        float y;
        float z;
        std::chrono::time_point<std::chrono::steady_clock> timestamp;
    } reading_t;

    IMU(boost::asio::io_service& io, const std::string& port);
    ~IMU();

    void init();

    void start();
    void stop();

    boost::system::error_code error() const { return error_; }
    bool initialized() const { return initialized_; }
    SensorStatus status() const;
    
    std::vector<reading_t> reset_accelerometer();
    std::vector<reading_t> reset_gyroscope();
    std::vector<reading_t> reset_magnetometer();
    std::vector<reading_t> reset_angle();

    const std::vector<reading_t>& accelerometer_readings() { return accelerometer_readings_; }
    const std::vector<reading_t>& gyroscope_readings() { return gyroscope_readings_; }
    const std::vector<reading_t>& magnetometer_readings() { return magnetometer_readings_; }
    const std::vector<reading_t>& angle_readings() { return angle_readings_; }

private:
    typedef std::vector<uint8_t> message_t;
    
    void send_command(const std::string& command);
    void send_char();
    
    void request_status();
    void read_delimiter();
    void read_length();
    void read_packet();
    void process_packet();

    void process_status(const message_t& message);
    reading_t process_reading(const message_t& message);
    boost::asio::serial_port serial_port_;
    boost::asio::deadline_timer char_timer_;
    boost::asio::deadline_timer timer_;

    boost::asio::streambuf read_buffer_;
    char tx_;
    int packet_length_;

    boost::system::error_code error_;

    std::vector<reading_t> accelerometer_readings_;
    std::vector<reading_t> gyroscope_readings_;
    std::vector<reading_t> magnetometer_readings_;
    std::vector<reading_t> angle_readings_;

    std::chrono::time_point<std::chrono::steady_clock> timestamp_;

    bool initialized_;
    bool capture_;

    std::string command_;
    int command_index_;
};

#endif
