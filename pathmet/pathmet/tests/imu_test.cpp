#include "../imu.hpp"

#include <boost/asio/high_resolution_timer.hpp>
#include <iostream>

boost::asio::io_service io;
boost::asio::high_resolution_timer timer(io);
std::shared_ptr<IMU> imu;

void print()
{
    timer.expires_at(timer.expires_at() + std::chrono::seconds(1));
    timer.async_wait([](boost::system::error_code ec) {
            if (!ec) {
                if (imu->error()) {
                    std::cerr << "IMU: " << imu->error().message() << std::endl;
                } else {
                    std::vector<IMU::reading_t> accelerometer_readings = imu->reset_accelerometer();
                    std::vector<IMU::reading_t> gyroscope_readings = imu->reset_gyroscope();
                    std::vector<IMU::reading_t> magnetometer_readings = imu->reset_magnetometer();
                    std::vector<IMU::reading_t> angle_readings = imu->reset_angle();

                    std::cerr << "IMU:"
                              << " A" << accelerometer_readings.size()
                              << " G" << gyroscope_readings.size()
                              << " C" << magnetometer_readings.size()
                              << " E" << angle_readings.size()
                              << " readings per second" << std::endl;
                }
                print();
            }
        });
}

int main(int argc, char* argv[])
{
    if (argc != 2) {
        std::cerr << "Usage: imu <port>" << std::endl;
        exit(1);
    }

    imu = std::make_shared<IMU>(io, argv[1]);
    imu->init();

    while (!imu->initialized()) {
        io.run_one();
    }

    std::cerr << "Initialized; starting" << std::endl;

    io.reset();
    imu->start();

    timer.expires_at(std::chrono::high_resolution_clock::now());

    print();

    io.run();

    return 0;
}
