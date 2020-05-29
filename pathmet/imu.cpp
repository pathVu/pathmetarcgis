#include "imu.hpp"

#include <iostream>
#include <thread>

IMU::IMU(boost::asio::io_service& io, const std::string& port) :
    serial_port_(io),
    char_timer_(io),
    timer_(io)
{
    try {
        using boost::asio::serial_port_base;
        serial_port_.open(port);
        serial_port_.set_option(serial_port_base::baud_rate(230400));
        serial_port_.set_option(serial_port_base::character_size(8));
        serial_port_.set_option(serial_port_base::parity(serial_port_base::parity::none));
        serial_port_.set_option(serial_port_base::stop_bits(serial_port_base::stop_bits::one));
    } catch (const boost::system::system_error& e) {
        error_ = e.code();
    }
}

IMU::~IMU()
{
    serial_port_.cancel();
}

void IMU::init()
{
    initialized_ = false;
    if (!error()) {
        read_delimiter();
        request_status();
    }
}

void IMU::start()
{
    accelerometer_readings_.clear();
    gyroscope_readings_.clear();
    magnetometer_readings_.clear();
    angle_readings_.clear();
    
    capture_ = true;
}

void IMU::stop()
{
    capture_ = false;
}

SensorStatus IMU::status() const
{
    if (error_) {
        return SensorStatus::error;
    } else if (!initialized_) {
        return SensorStatus::init;
    } else if (std::chrono::steady_clock::now() - timestamp_ > std::chrono::milliseconds(500)) {
        return SensorStatus::timeout;
    } else {
        return SensorStatus::ok;
    }
}

std::vector<IMU::reading_t> IMU::reset_accelerometer()
{
    std::vector<IMU::reading_t> v;
    std::swap(v, accelerometer_readings_);
    return v;
}

std::vector<IMU::reading_t> IMU::reset_gyroscope()
{
    std::vector<IMU::reading_t> v;
    std::swap(v, gyroscope_readings_);
    return v;
}

std::vector<IMU::reading_t> IMU::reset_magnetometer()
{
    std::vector<IMU::reading_t> v;
    std::swap(v, magnetometer_readings_);
    return v;
}

std::vector<IMU::reading_t> IMU::reset_angle()
{
    std::vector<IMU::reading_t> v;
    std::swap(v, angle_readings_);
    return v;
}

void IMU::send_command(const std::string& command)
{
    if (command_.empty()) {
        command_ = command;
        command_index_ = 0;

        if (command_index_ < command_.size()) {
            send_char();
        }
    } else {
        command_.append(command);
    }
}

void IMU::send_char()
{
    auto self(shared_from_this());

    tx_ = command_[command_index_];
    boost::asio::async_write(serial_port_, boost::asio::buffer(&tx_, 1),
        [this, self](boost::system::error_code ec, std::size_t length) {
            if (!ec) {
                if (++command_index_ < command_.size()) {
                    char_timer_.expires_from_now(boost::posix_time::milliseconds(10));
                    char_timer_.async_wait([this, self](boost::system::error_code ec) {
                            if (!ec) {
                                send_char();
                            } else {
                                error_ = ec;
                            }
                        });
                } else {
                    command_.clear();
                    command_index_ = 0;
                }
            } else {
                error_ = ec;
            }
        });
}

void IMU::request_status()
{
    send_command("vars");
}

void IMU::read_delimiter()
{
    auto self(shared_from_this());

    boost::asio::async_read_until(serial_port_, read_buffer_, 0x01,
        [this, self](boost::system::error_code ec, std::size_t length) {
            if (!ec) {
                read_buffer_.consume(length);
                if (read_buffer_.in_avail()) {
                    packet_length_ = read_buffer_.sbumpc();
                    if (packet_length_ > 4) {
                        read_packet();
                    } else {
                        read_delimiter();
                    }
                } else {
                    read_length();
                }
            } else {
                std::cerr << ec.message() << std::endl;
                error_ = ec;
            }
        });
}

void IMU::read_length()
{
    auto self(shared_from_this());

    boost::asio::async_read(serial_port_, read_buffer_, boost::asio::transfer_at_least(1),
        [this, self](boost::system::error_code ec, std::size_t length) {
            if (!ec) {
                packet_length_ = read_buffer_.sbumpc();
                if (packet_length_ > 4) {
                    read_packet();
                } else {
                    read_delimiter();
                }
            } else {
                std::cerr << ec.message() << std::endl;
                error_ = ec;
            }
        });
}

void IMU::read_packet()
{
    auto self(shared_from_this());

    int needed = packet_length_ - read_buffer_.in_avail() - 2;
    if (needed > 0) {
        boost::asio::async_read(serial_port_, read_buffer_, boost::asio::transfer_at_least(needed),
            [this, self](boost::system::error_code ec, std::size_t length) {
                if (!ec) {
                    process_packet();
                    read_delimiter();
                } else {
                    std::cerr << ec.message() << std::endl;
                    error_ = ec;
                }
            });
    } else {
        process_packet();
        read_delimiter();
    }
}

static uint32_t get_uint32(std::vector<uint8_t>::const_iterator& it)
{
    uint32_t result = *it++ << 24;
    result |= *it++ << 16;
    result |= *it++ << 8;
    result |= *it++;

    return result;
}

static float get_float(std::vector<uint8_t>::const_iterator& it)
{
    uint32_t result = get_uint32(it);
    return *reinterpret_cast<float*>(&result);
}

IMU::reading_t IMU::process_reading(const message_t& message)
{
    auto it = message.begin();
    reading_t reading;
    reading.counter = get_uint32(it);
    reading.x = get_float(it);
    reading.y = get_float(it);
    reading.z = get_float(it);
    reading.timestamp = std::chrono::steady_clock::now();

    return reading;
}

void IMU::process_status(const message_t& message)
{
    auto self(shared_from_this());
    auto it = message.begin();
    
    uint8_t sensors_status = *it++;
    uint8_t sensors_resolution = *it++;
    uint8_t low_output_rate_status = *it++;
    it += 3;
    uint8_t data_currently_streaming = *it++;
    
    std::cerr << "Received status packet: " << std::endl
              << "\tStatus: " << std::hex << (int) sensors_status << std::endl
              << "\tResolution: " << std::hex << (int) sensors_resolution << std::endl
              << "\tLow Output Rate: " << std::hex << (int) low_output_rate_status << std::endl
              << "\tData Streaming: " << std::hex << (int) data_currently_streaming << std::endl;
    std::cerr << std::dec;
    
    if (!initialized_) {
        // turn off any data streams we don't want; enable any we do
        if (data_currently_streaming & imu_status_bits::HEAD) {
            send_command("varh");
        }
        
        if (!(data_currently_streaming & imu_status_bits::EULER)) {
            send_command("vare");
        }
        
        if (!(data_currently_streaming & imu_status_bits::MAG)) {
            send_command("varc");
        }
        
        if (data_currently_streaming & imu_status_bits::QUAT) {
            send_command("varq");
        }
        
        if (!(data_currently_streaming & imu_status_bits::GYRO)) {
            send_command("varg");
        }
        
        if (!(data_currently_streaming & imu_status_bits::ACC)) {
            send_command("vara");
        }

        // we don't receive an acknowledgement so just assume the commands made
        // it through
        timer_.expires_from_now(boost::posix_time::milliseconds(500));
        timer_.async_wait([this, self](boost::system::error_code ec) {
                if (!ec) {
                    initialized_ = true;
                }
            });
    }
}

void IMU::process_packet()
{
    char type = read_buffer_.sbumpc();
    std::vector<uint8_t> message;
    message.resize(packet_length_ - 4);
    read_buffer_.sgetn(reinterpret_cast<char*>(&message[0]), packet_length_ - 4);
    char end_delimiter = read_buffer_.sbumpc();

    if (end_delimiter == 0x04) {
        switch (type) {
        case 'a':
            if (capture_) {
                accelerometer_readings_.push_back(process_reading(message));
            }
            timestamp_ = std::chrono::steady_clock::now();
            break;
        case 'g':
            if (capture_) {
                gyroscope_readings_.push_back(process_reading(message));
            }
            timestamp_ = std::chrono::steady_clock::now();
            break;
        case 'c':
            if (capture_) {
                magnetometer_readings_.push_back(process_reading(message));
            }
            timestamp_ = std::chrono::steady_clock::now();
            break;
        case 'e':
            if (capture_) {
                angle_readings_.push_back(process_reading(message));
            }
            timestamp_ = std::chrono::steady_clock::now();
            break;
        case 's':
            process_status(message);
            break;
        default:
            // ignore
            break;
        }
    }
}
