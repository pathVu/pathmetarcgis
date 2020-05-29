#include "sensors.hpp"

#include <chrono>
#include <direct.h>
#include <iostream>
#include <sys/stat.h>
#include <sys/types.h>

#include <boost/bind.hpp>

#include "camera.hpp"

Sensors::Sensors(boost::asio::io_service& io,
                 std::shared_ptr<Camera> camera,
                 std::shared_ptr<Encoder> encoder,
                 std::shared_ptr<IMU> imu,
                 std::shared_ptr<Laser> laser) :
    io_(io),
    work_(work_service_),
    work_thread_(boost::bind(&boost::asio::io_service::run, &work_service_)),
    camera_(camera),
    encoder_(encoder),
    imu_(imu),
    laser_(laser),
    timer_(io),
    running_(false)
{
    
}

Sensors::status_t Sensors::status()
{
    status_t s = {
        running_,
        camera_->status(),
        encoder_->status(),
        imu_->status(),
        laser_->status(),
    };

    return s;
}

void Sensors::flag(const std::string& name)
{
    current_flags_.push_back({ name, std::chrono::steady_clock::now() });
}

void Sensors::insert_empty_fields(int fields)
{
    for (int i = 0; i < fields; ++i) {
        csv_ << ",";
    }
}

void Sensors::do_write_encoder_data(std::vector<Encoder::reading_t>::const_iterator& it)
{
    if (it != encoder_readings_.cend()) {
        csv_ << it->value << ","
              << it->counter << ","
              << from_start(it->timestamp) << ",";
        ++it;
    } else {
        csv_ << std::string(3, ',');
    }
}

void Sensors::do_write_imu_data(std::vector<IMU::reading_t>::const_iterator& it, const std::vector<IMU::reading_t>& v)
{
    if (it != v.cend()) {
        csv_ << it->x << ","
              << it->y << ","
              << it->z << ","
              << it->counter << ","
              << from_start(it->timestamp) << ",";
        ++it;
    } else {
        csv_ << std::string(5, ',');
    }
}

void Sensors::do_write_laser_data(std::vector<Laser::reading_t>::const_iterator& it)
{
    if (it != laser_readings_.cend()) {
        csv_ << it->value << ","
              << (unsigned int) it->counter << ","
              << from_start(it->timestamp) << ",";
        ++it;
    } else {
        csv_ << std::string(3, ',');
    }
}

void Sensors::do_write_flags(std::vector<Flag>::const_iterator& it)
{
    if (it != flags_.cend()) {
        csv_ << it->value << ","
             << from_start(it->timestamp);
        ++it;
    } else {
        csv_ << ",";
    }
}

void Sensors::do_write_data()
{
    auto encoder_it = encoder_readings_.cbegin();
    auto accelerometer_it = accelerometer_readings_.cbegin();
    auto gyroscope_it = gyroscope_readings_.cbegin();
    auto magnetometer_it = magnetometer_readings_.cbegin();
    auto angle_it = angle_readings_.cbegin();
    auto laser_it = laser_readings_.cbegin();
    auto flags_it = flags_.cbegin();

    std::lock_guard<std::mutex> guard(csv_mutex_);

    bool mark = false;

    do {
        if (!mark) {
            csv_ << from_start() << ",";
            mark = true;
        } else {
            csv_ << ",";
        }
        
        do_write_encoder_data(encoder_it);
        do_write_imu_data(accelerometer_it, accelerometer_readings_);
        do_write_imu_data(gyroscope_it, gyroscope_readings_);
        do_write_imu_data(magnetometer_it, magnetometer_readings_);
        do_write_imu_data(angle_it, angle_readings_);
        do_write_laser_data(laser_it);
        do_write_flags(flags_it);
        csv_ << std::endl;
        
    } while (encoder_it != encoder_readings_.cend() ||
             accelerometer_it != accelerometer_readings_.cend() ||
             gyroscope_it != gyroscope_readings_.cend() ||
             magnetometer_it != magnetometer_readings_.cend() ||
             angle_it != angle_readings_.cend() ||
             laser_it != laser_readings_.cend() ||
             flags_it != flags_.cend());
}

void Sensors::write_data()
{
    encoder_readings_ = encoder_->reset();
    accelerometer_readings_ = imu_->reset_accelerometer();
    gyroscope_readings_ = imu_->reset_gyroscope();
    magnetometer_readings_ = imu_->reset_magnetometer();
    angle_readings_ = imu_->reset_angle();
    laser_readings_ = laser_->reset();
    flags_.clear();
    std::swap(flags_, current_flags_);

    work_service_.post(boost::bind(&Sensors::do_write_data, this));
}

void Sensors::write_header()
{
    csv_ << "marker timestamp" << ","
         << "encoder value" << ","
         << "encoder counter" << ","
         << "encoder timestamp" << ","
         << "accel x" << ","
         << "accel y" << ","
         << "accel z" << ","
         << "accel counter" << ","
         << "accel timestamp" << ","
         << "gyro x" << ","
         << "gyro y" << ","
         << "gyro z" << ","
         << "gyro counter" << ","
         << "gyro timestamp" << ","
         << "mag x" << ","
         << "mag y" << ","
         << "mag z" << ","
         << "mag counter" << ","
         << "mag timestamp" << ","
         << "angle x" << ","
         << "angle y" << ","
         << "angle z" << ","
         << "angle counter" << ","
         << "angle timestamp" << ","
         << "laser value" << ","
         << "laser counter" << ","
         << "laser timestamp" << ","
         << "flags" << ","
         << "flags timestamp" << std::endl;
}

void Sensors::write_footer()
{
    
}

void Sensors::sample(boost::system::error_code ec)
{
    if (!ec) {
        std::cerr << "Logging one second of data" << std::endl;
        write_data();
        timer_.expires_at(timer_.expires_at() + std::chrono::seconds(1));
        timer_.async_wait(boost::bind(&Sensors::sample, this, _1));
    }
}

void Sensors::capture_image()
{
    std::ostringstream oss;
    oss << name_ << "\\"
        << std::setfill('0') << std::setw(3) << image_count_++ << ".png";
    camera_->capture(oss.str());
}

bool Sensors::start(const std::string& name)
{
    name_ = name;

    std::replace_if(name_.begin(), name_.end(), [](char c) {
            switch (c) {
            case '<':
            case '>':
            case ':':
            case '/':
            case '\\':
            case '|':
            case '?':
            case '*':
                return true;
            default:
                return false;
            }
        }, '_');
    
    std::cerr << "filename: " << name_ << std::endl;
    
    struct stat sb;
    if (stat(name.c_str(), &sb) == 0 && (sb.st_mode & _S_IFDIR)) {
        std::cerr << "exists!" << std::endl;
        return false;
    }
    
    running_ = true;

    std::cerr << _mkdir(name.c_str()) << std::endl;
    std::string csv_name = name_ + "/" + name_ + ".csv";

    {
        std::lock_guard<std::mutex> guard(csv_mutex_);
        csv_.open(csv_name.c_str(), std::ios_base::out | std::ios_base::trunc);
        write_header();
    }

    image_count_ = 1;
    encoder_->set_callback(boost::bind(&Sensors::capture_image, this));

    timer_.expires_from_now(std::chrono::seconds(1));
    start_time_ = std::chrono::steady_clock::now();
    encoder_->start();
    imu_->start();
    laser_->start();

    timer_.async_wait(boost::bind(&Sensors::sample, this, _1));

    return true;
}

void Sensors::stop()
{
    timer_.cancel();

    encoder_->stop();
    imu_->stop();
    laser_->stop();

    {
        std::lock_guard<std::mutex> guard(csv_mutex_);
        write_footer();
        csv_.close();
    }

    running_ = false;
}
