#ifndef SENSORS_HPP
#define SENSORS_HPP

#include <fstream>
#include <memory>
#include <mutex>
#include <thread>

#include <boost/asio.hpp>
#include <boost/asio/high_resolution_timer.hpp>

#include "sensor_status.hpp"
#include "encoder.hpp"
#include "imu.hpp"
#include "laser.hpp"

class Camera;

class Sensors {
public:
    struct status_t {
        bool running;
        SensorStatus camera;
        SensorStatus encoder;
        SensorStatus imu;
        SensorStatus laser;
    };

    Sensors(boost::asio::io_service& io,
            std::shared_ptr<Camera> camera_,
            std::shared_ptr<Encoder> encoder_,
            std::shared_ptr<IMU> imu,
            std::shared_ptr<Laser> laser);
    
    bool start(const std::string& filename); // if false, already exists
    void stop();
    status_t status();

    double laser_summary_distance() const { return laser_->summary_distance(); }
    double encoder_summary_distance() const { return encoder_->summary_distance(); }

    void flag(const std::string& flag);

private:
    struct Flag {
        std::string value;
        std::chrono::time_point<std::chrono::steady_clock> timestamp;
    };
    
    boost::asio::io_service& io_;

    boost::asio::io_service work_service_;
    boost::asio::io_service::work work_;
    std::thread work_thread_;

    std::shared_ptr<Camera> camera_;
    std::shared_ptr<Encoder> encoder_;
    std::shared_ptr<IMU> imu_;
    std::shared_ptr<Laser> laser_;

    std::vector<Encoder::reading_t> encoder_readings_;
    std::vector<IMU::reading_t> accelerometer_readings_;
    std::vector<IMU::reading_t> gyroscope_readings_;
    std::vector<IMU::reading_t> magnetometer_readings_;
    std::vector<IMU::reading_t> angle_readings_;
    std::vector<Laser::reading_t> laser_readings_;
    std::vector<Flag> flags_;

    std::vector<Flag> current_flags_;

    boost::asio::high_resolution_timer timer_;

    std::string name_;
    std::ofstream csv_;
    std::mutex csv_mutex_;
    std::chrono::time_point<std::chrono::steady_clock> start_time_;

    bool running_;

    int image_count_;

    void write_header();
    void write_data();
    void write_footer();
    void sample(boost::system::error_code ec);
    void capture_image();

    void do_write_data();
    void insert_empty_fields(int);
    void do_write_encoder_data(std::vector<Encoder::reading_t>::const_iterator& it);
    void do_write_imu_data(std::vector<IMU::reading_t>::const_iterator& it, const std::vector<IMU::reading_t>& v);
    void do_write_laser_data(std::vector<Laser::reading_t>::const_iterator& it);
    void do_write_flags(std::vector<Flag>::const_iterator& it);

    double from_start(std::chrono::time_point<std::chrono::steady_clock> t = std::chrono::steady_clock::now()) { return std::chrono::duration<double>(t - start_time_).count(); }
};

#endif
