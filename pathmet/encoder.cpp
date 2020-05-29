#include "encoder.hpp"

#include <iostream>
#include <functional>
#include <string>
#include <vector>

#include <boost/math/constants/constants.hpp>

using namespace std::placeholders;

static const double pi = boost::math::constants::pi<double>();

// according to Eric, the diameter of the wheel is "approximately" 15.5
// inches. There are 1000 encoder counts per revolution. There is also a 2:1
// ratio on the pulley. Lastly, this all turned out to be off by 5.805x when tested
const double Encoder::INCHES_PER_COUNT = pi * 15.5 / 1000.0 / 2.0 / 5.805;

const std::vector<std::string> Encoder::command_sequence_ = {
    "R0E\r\n",                  // shut off streaming (if on)
    "W15F\r\n",                 // space separated, crlf, timestamp
    "W0000\r\n",                // enter quadrature mode
    "W04000\r\n",               // count up
    "W0343\r\n",                // set counter
    "W0B00\r\n",                // set threshold (0 counts)
    "W0C01\r\n",                // set interval (1.9 ms)
    "S0E\r\n"                   // stream encoder
};

// need a hundredth of an inch per second variance to count as changing
const double Encoder::CHANGE_THRESHOLD = 0.01;

Encoder::Encoder(boost::asio::io_service& io, const std::string& port) :
    serial_port_(io),
    timer_(io),
    command_(command_sequence_.begin()),
    capture_(false)
{
    try {
        using boost::asio::serial_port_base;

        serial_port_.open(port);
        serial_port_.set_option(serial_port_base::baud_rate(230400));
        serial_port_.set_option(serial_port_base::character_size(8));
        serial_port_.set_option(serial_port_base::parity(serial_port_base::parity::none));
        serial_port_.set_option(serial_port_base::stop_bits(serial_port_base::stop_bits::one));
        serial_port_.set_option(serial_port_base::flow_control(serial_port_base::flow_control::none));
    } catch (const boost::system::system_error& e) {
        error_ = e.code();
    }
}

Encoder::~Encoder()
{
    serial_port_.cancel();
}

void Encoder::init()
{
    if (!error()) {
        command_ = command_sequence_.begin();
        init_next_command();
    }
}

void Encoder::start()
{
    readings_.clear();
    start_valid_ = false;
    next_callback_distance_ = 0;
    capture_ = true;
    
    min_reading_ = DBL_MAX;
    max_reading_ = -DBL_MAX;

    summary_min_reading_ = DBL_MAX;
    summary_max_reading_ = -DBL_MAX;
    
    changing_ = true;
    
    timer_.expires_from_now(boost::posix_time::seconds(1));
    timer_.async_wait(std::bind(&Encoder::verify_changing, this, _1));
}

void Encoder::stop()
{
    capture_ = false;
}

SensorStatus Encoder::status() const
{
    if (error()) {
        return SensorStatus::error;
    } else if (command_ != command_sequence_.end()) {
        return SensorStatus::init;
    } else if (std::chrono::steady_clock::now() - timestamp_ > std::chrono::milliseconds(500)) {
        return SensorStatus::timeout;
    } else if (capture_ && !changing_) {
        return SensorStatus::error;
    } else {
        return SensorStatus::ok;
    }
}

std::vector<Encoder::reading_t> Encoder::reset()
{
    std::vector<reading_t> v;
    std::swap(v, readings_);
    return v;
}

void Encoder::init_next_command()
{
    auto self(shared_from_this());

    boost::asio::async_write(serial_port_, boost::asio::buffer(*command_),
        [this, self](boost::system::error_code ec, std::size_t) {
            if (!ec) {
                init_read_command_acknowledgement();
            } else {
                error_ = ec;
            }
        });
}

void Encoder::init_read_command_acknowledgement()
{
    auto self(shared_from_this());

    boost::asio::async_read_until(serial_port_, read_buffer_, "\r\n",
        [this, self](boost::system::error_code ec, std::size_t) {
            if (!ec) {
                std::istream is(&read_buffer_);
                std::string line;
                std::getline(is, line);

                // TBD: this won't actually match if streaming was already
                // running; should wait until the actual command sent is
                // acknowledged

                if (++command_ != command_sequence_.end()) {
                    init_next_command();
                } else {
                    read();
                }
            } else {
                error_ = ec;
            }
        });
}

void Encoder::check_callback(double distance)
{
    if (distance > next_callback_distance_) {
        std::cerr << "Check callback " << distance << std::endl;
        next_callback_distance_ += callback_distance_;
        if (callback_) {
            callback_();
        }
    }
}

void Encoder::read()
{
    auto self(shared_from_this());

    boost::asio::async_read_until(serial_port_, read_buffer_, "\r\n",
        [this, self](boost::system::error_code ec, std::size_t) {
            if (!ec) {
                try {
                    std::istream is(&read_buffer_);
                    std::string command;
                    int reg;
                    uint32_t value;
                    uint32_t counter;
                    std::string exclamation;

                    is >> command
                       >> std::hex >> reg
                       >> std::hex >> value
                       >> std::hex >> counter
                       >> exclamation;

                    is.ignore(std::numeric_limits<std::streamsize>::max(), '\n');
                    if (command == "s" && reg == 14) {
                        if (!start_valid_) {
                            start_ = static_cast<int32_t>(value);
                            start_valid_ = true;
                        }
                        
                        if (capture_) {
                            reading_t reading;
                            reading.value = (start_ - static_cast<int32_t>(value)) * INCHES_PER_COUNT;
                            reading.counter = counter;
                            reading.timestamp = std::chrono::steady_clock::now();
                            readings_.push_back(reading);

                            check_callback(reading.value);
                            if (reading.value < min_reading_) {
                                min_reading_ = reading.value;
                            }

                            if (reading.value > max_reading_) {
                                max_reading_ = reading.value;
                            }

                            if (reading.value < summary_min_reading_) {
                                summary_min_reading_ = reading.value;
                            }

                            if (reading.value > summary_max_reading_) {
                                summary_max_reading_ = reading.value;
                            }
                        }

                        timestamp_ = std::chrono::steady_clock::now();
                    }
                } catch (std::exception& e) {

                }

                read();
            } else {
                error_ = ec;
            }
        });
}

void Encoder::verify_changing(const boost::system::error_code& ec)
{
    if (capture_ && !ec) {
        changing_ = (max_reading_ - min_reading_ > CHANGE_THRESHOLD);
        timer_.expires_at(timer_.expires_at() + boost::posix_time::seconds(1));
        timer_.async_wait(std::bind(&Encoder::verify_changing, this, _1));
        
        min_reading_ = DBL_MAX;
        max_reading_ = -DBL_MAX;
    }
}
