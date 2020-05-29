#include "laser.hpp"

#include <functional>
#include <iostream>
#include <thread>

#include <boost/format.hpp>

using namespace std::placeholders;

namespace laser {
enum {
    baud_rate = 0x04,
    low_byte_sample_rate = 0x08,
    high_byte_sample_rate = 0x09,
    low_byte_integration_limit = 0x0A,
    high_byte_integration_limit = 0x0B,
};
}

// laser's maximum range is 500 mm; it's normalized to 16384 counts at max range
const double Laser::INCHES_PER_COUNT = 19.685 / 16384;

// laser reports distance minus its base range
const double Laser::BASE_INCHES = 4.92126;

// need a hundredth of an inch per second variance to count as changing
const double Laser::CHANGE_THRESHOLD = 0.01;

Laser::Laser(boost::asio::io_service& io, const std::string& port) :
    serial_port_(io),
    timer_(io),
    initialized_(false),
    capture_(false)
{
    try
    {
        using boost::asio::serial_port_base;

        serial_port_.open(port);
        serial_port_.set_option(serial_port_base::baud_rate(9600));
        serial_port_.set_option(serial_port_base::character_size(8));
        serial_port_.set_option(serial_port_base::parity(serial_port_base::parity::even));
        serial_port_.set_option(serial_port_base::stop_bits(serial_port_base::stop_bits::one));
    } catch (const boost::system::system_error& e) {
        error_ = e.code();
    }
}

Laser::~Laser()
{
    serial_port_.cancel();
    serial_port_.close();
}

void Laser::start()
{
    auto self(shared_from_this());

    if (initialized_) {
        readings_.clear();
        
        capture_ = true;

        min_reading_ = DBL_MAX;
        max_reading_ = -DBL_MAX;

        summary_min_reading_ = DBL_MAX;
        summary_max_reading_ = -DBL_MAX;

        changing_ = true;

        timer_.expires_from_now(boost::posix_time::seconds(1));
        timer_.async_wait(std::bind(&Laser::verify_changing, this, _1));
    }
}

void Laser::stop()
{
    timer_.cancel();
    capture_ = false;
}

void Laser::init()
{
    initialized_ = false;
    if (!error()) {
        init_set_laser_baudrate();
    }
}

SensorStatus Laser::status() const
{
    if (error_) {
        return SensorStatus::error;
    } else if (!initialized_) {
        return SensorStatus::init;
    } else if (std::chrono::steady_clock::now() - timestamp_ > std::chrono::milliseconds(500)) {
        return SensorStatus::timeout;
    } else if (capture_ && !changing_) {
        return SensorStatus::error; // ?
    } else {
        return SensorStatus::ok;
    }
}

std::vector<Laser::reading_t> Laser::reset()
{
    std::vector<Laser::reading_t> v;
    std::swap(v, readings_);
    return v;
}

static std::vector<uint8_t> build_register_write(uint8_t address, uint8_t value)
{
    std::vector<uint8_t> v;
    v.push_back(0x00);
    v.push_back(0x83);

    v.push_back(0x80 | (address & 0x0f));
    v.push_back(0x80 | (address >> 4));

    v.push_back(0x80 | (value & 0x04));
    v.push_back(0x80 | (value >> 4));

    return v;
}

void Laser::init_set_laser_baudrate()
{
    auto self(shared_from_this());

    command_ = build_register_write(laser::baud_rate, 48);
    boost::asio::async_write(serial_port_, boost::asio::buffer(command_),
        [this, self](boost::system::error_code ec, std::size_t) {
            if (!ec) {
                timer_.expires_from_now(boost::posix_time::milliseconds(100));
                timer_.async_wait([this, self](boost::system::error_code ec) {
                        init_set_self_baudrate();
                    });
            } else {
                error_ = ec;
            }
        });
}

void Laser::init_set_self_baudrate()
{
    auto self(shared_from_this());
    
    serial_port_.set_option(boost::asio::serial_port_base::baud_rate(115200));
    timer_.expires_from_now(boost::posix_time::milliseconds(100));
    timer_.async_wait([this, self](boost::system::error_code ec) {
            if (!ec) {
                init_set_high_byte_sample_rate();
            } else {
                error_ = ec;
            }
        });
}

void Laser::init_set_high_byte_sample_rate()
{
    auto self(shared_from_this());

    command_ = build_register_write(laser::high_byte_sample_rate, 0);
    boost::asio::async_write(serial_port_, boost::asio::buffer(command_),
        [this, self](boost::system::error_code ec, std::size_t) {
            if (!ec) {
                init_set_low_byte_sample_rate();
            } else {
                error_ = ec;
            }
        });
}

void Laser::init_set_low_byte_sample_rate()
{
    auto self(shared_from_this());

    command_ = build_register_write(laser::low_byte_sample_rate, 100);
    boost::asio::async_write(serial_port_, boost::asio::buffer(command_),
        [this, self](boost::system::error_code ec, std::size_t) {
            if (!ec) {
                init_set_high_byte_integration_limit();
            } else {
                error_ = ec;
            }
        });
}

void Laser::init_set_high_byte_integration_limit()
{
    auto self(shared_from_this());

    command_ = build_register_write(laser::high_byte_integration_limit, 0x02);
    boost::asio::async_write(serial_port_, boost::asio::buffer(command_),
        [this, self](boost::system::error_code ec, std::size_t) {
            if (!ec) {
                init_set_low_byte_integration_limit();
            } else {
                error_ = ec;
            }
        });
}

void Laser::init_set_low_byte_integration_limit()
{
    auto self(shared_from_this());

    command_ = build_register_write(laser::low_byte_integration_limit, 0x80);
    boost::asio::async_write(serial_port_, boost::asio::buffer(command_),
        [this, self](boost::system::error_code ec, std::size_t) {
            if (!ec) {
                init_end_initialization();
            } else {
                error_ = ec;
            }
        });
}

void Laser::init_end_initialization()
{
    auto self(shared_from_this());
    
    timer_.expires_from_now(boost::posix_time::milliseconds(100));
    timer_.async_wait([this, self](boost::system::error_code ec) {
            if (!ec) {
                initialized_ = true;
                send_enable_stream_command();
            } else {
                error_ = ec;
            }
        });
}

void Laser::send_enable_stream_command()
{
    auto self(shared_from_this());

    command_ = { 0x00, 0x87 };
    boost::asio::async_write(serial_port_, boost::asio::buffer(command_),
        [this, self](boost::system::error_code ec, std::size_t) {
            if (!ec) {
                read_sample();
            } else {
                error_ = ec;
            }
        });
}

static uint8_t get_counter(uint8_t byte)
{
    // the second two bits in each reading byte are a counter that counts up
    return (byte & 0x30) >> 4;
}

int Laser::sync_offset()
{
    int counter = get_counter(read_buffer_[0]);
    int offset = 1;
    while (offset < 4 && get_counter(read_buffer_[offset]) == counter) {
        ++offset;
    }

    if (offset == 4) {
        offset = 0;
    }

    return offset;
}

void Laser::skip_bytes(int bytes)
{
    auto self(shared_from_this());

    boost::asio::async_read(serial_port_, boost::asio::buffer(read_buffer_, bytes),
        [this, self](boost::system::error_code ec, std::size_t) {
            if (!ec) {
                read_sample();
            } else {
                error_ = ec;
            }
        });
}

void Laser::read_sample()
{
    auto self(shared_from_this());

    boost::asio::async_read(serial_port_, boost::asio::buffer(read_buffer_, 4),
        [this, self](boost::system::error_code ec, std::size_t) {
            if (!ec) {
                int offset = sync_offset();
                if (offset) {
                    // out of sync; discard bytes to get back in sync
                    skip_bytes(offset);
                } else {
                    if (capture_) {
                        reading_t reading;
                        uint16_t counts =
                            (read_buffer_[0] & 0x0f) |
                            ((read_buffer_[1] & 0x0f) << 4) |
                            ((read_buffer_[2] & 0x0f) << 8) |
                            ((read_buffer_[3] & 0x0f) << 12);
                        if (counts) {
                            reading.value = BASE_INCHES + counts * INCHES_PER_COUNT;
                        } else {
                            reading.value = 0.0;
                        }
                        reading.counter = get_counter(read_buffer_[0]);
                        reading.timestamp = std::chrono::steady_clock::now();
                        
                        readings_.push_back(reading);

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
                    
                    read_sample();
                }
            } else {
                error_ = ec;
            }
        });
}

void Laser::verify_changing(const boost::system::error_code& ec)
{
    if (capture_ && !ec) {
        changing_ = (max_reading_ - min_reading_ > CHANGE_THRESHOLD);
        timer_.expires_at(timer_.expires_at() + boost::posix_time::seconds(1));
        timer_.async_wait(std::bind(&Laser::verify_changing, this, _1));

        min_reading_ = DBL_MAX;
        max_reading_ = -DBL_MAX;
    }
}
