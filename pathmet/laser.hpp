#ifndef LASER_HPP
#define LASER_HPP

#include <chrono>
#include <cstdint>

#include <boost/asio.hpp>

#include "sensor_status.hpp"

class Laser : public std::enable_shared_from_this<Laser> {
public:
    typedef struct {
        double value; // in inches
        uint8_t counter;
        std::chrono::time_point<std::chrono::steady_clock> timestamp;
    } reading_t;
    
    Laser(boost::asio::io_service& io, const std::string& port);
    ~Laser();

    void init();
    void start();
    void stop();
    
    boost::system::error_code error() const { return error_; }
    bool initialized() const { return initialized_; }
    SensorStatus status() const;

    std::vector<reading_t> reset();
    const std::vector<reading_t>& readings() const { return readings_; }

    double summary_distance() const { return summary_max_reading_ - summary_min_reading_; }

private:
    void init_set_laser_baudrate();
    void init_set_self_baudrate();
    void init_set_high_byte_sample_rate();
    void init_set_low_byte_sample_rate();
    void init_set_high_byte_integration_limit();
    void init_set_low_byte_integration_limit();
    void init_end_initialization();

    void send_baudrate_command();
    void increase_baudrate();
    void send_enable_stream_command();
    void skip_bytes(int);
    void read_sample();
    int sync_offset();

    void verify_changing(const boost::system::error_code& error);

    std::vector<reading_t> readings_;
    double min_reading_;
    double max_reading_;

    double summary_min_reading_;
    double summary_max_reading_;

    boost::asio::serial_port serial_port_;
    boost::asio::deadline_timer timer_;

    std::chrono::time_point<std::chrono::steady_clock> timestamp_;

    bool initialized_;
    bool capture_;
    boost::system::error_code error_;
    bool changing_;

    uint8_t read_buffer_[4];
    std::vector<uint8_t> command_;

    static const double BASE_INCHES;
    static const double INCHES_PER_COUNT;
    static const double CHANGE_THRESHOLD;
};

#endif
