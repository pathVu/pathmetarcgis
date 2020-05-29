#ifndef ENCODER_HPP
#define ENCODER_HPP

#include <chrono>

#include <boost/asio.hpp>
#include <boost/function.hpp>

#include "sensor_status.hpp"

class Encoder : public std::enable_shared_from_this<Encoder> {
public:
    typedef struct {
        double value; // inches
        uint32_t counter;
        std::chrono::time_point<std::chrono::steady_clock> timestamp;
    } reading_t;
    
    Encoder(boost::asio::io_service& io, const std::string& port);
    ~Encoder();

    void init();
    void start();
    void stop();

    boost::system::error_code error() const { return error_; }
    bool initialized() const { return command_ == command_sequence_.end(); }
    SensorStatus status() const;

    std::vector<reading_t> reset();
    const std::vector<reading_t>& readings() const { return readings_; }

    double callback_distance() const { return callback_distance_; }
    void callback_distance(double d) { callback_distance_ = d; }

    void set_callback(boost::function<void()> cb) { callback_ = cb; }

    double summary_distance() const { return summary_max_reading_ - summary_min_reading_; }

private:
    void init_next_command();
    void init_read_command_acknowledgement();

    void enable_stream();
    void read();
    void check_callback(double distance);
    void verify_changing(const boost::system::error_code& ec);

    boost::asio::serial_port serial_port_;
    boost::asio::deadline_timer timer_;
    boost::system::error_code error_;
    boost::asio::streambuf read_buffer_;

    static const std::vector<std::string> command_sequence_;
    std::vector<std::string>::const_iterator command_;

    bool capture_;
    bool changing_;
    int32_t start_;
    bool start_valid_;

    double min_reading_;
    double max_reading_;

    double summary_min_reading_;
    double summary_max_reading_;

    static const double INCHES_PER_COUNT;

    std::vector<reading_t> readings_;
    std::chrono::time_point<std::chrono::steady_clock> timestamp_;

    double callback_distance_;
    double next_callback_distance_;
    boost::function<void()> callback_;

    static const double CHANGE_THRESHOLD;
};

#endif
