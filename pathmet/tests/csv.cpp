#include "../laser.hpp"
#include "../encoder.hpp"

#include <boost/asio/high_resolution_timer.hpp>
#include <fstream>
#include <iostream>
#include <thread>

boost::asio::io_service io;
boost::asio::high_resolution_timer timer(io);
std::shared_ptr<Laser> laser;
std::shared_ptr<Encoder> encoder;
std::chrono::time_point<std::chrono::steady_clock> start;

std::ofstream csv;

static std::ostream& operator<<(std::ostream& s, std::chrono::time_point<std::chrono::steady_clock> t)
{
    return s << std::chrono::duration_cast<std::chrono::microseconds>(t - start).count();
}

void dump(const std::vector<Laser::reading_t>& laser_readings, const std::vector<Encoder::reading_t>& encoder_readings)
{
    auto laser_it = laser_readings.begin();
    auto encoder_it = encoder_readings.begin();

    bool mark = false;

    do {
        if (!mark) {
            csv << std::chrono::steady_clock::now() << ",";
            mark = true;
        } else {
            csv << ",";
        }
        
        if (laser_it != laser_readings.end()) {
            csv << laser_it->value << "," << (unsigned int) laser_it->counter << "," << laser_it->timestamp << ",";
            ++laser_it;
        } else {
            csv << ",,,";
        }

        if (encoder_it != encoder_readings.end()) {
            csv << encoder_it->value << "," << encoder_it->counter << "," << encoder_it->timestamp;
            ++encoder_it;
        } else {
            csv << ",,,";
        }

        csv << std::endl;
    } while (laser_it != laser_readings.end() || encoder_it != encoder_readings.end());
}

void print()
{
    timer.expires_at(timer.expires_at() + std::chrono::seconds(1));
    timer.async_wait([](boost::system::error_code ec) {
            if (!ec) {
                std::vector<Laser::reading_t> laser_readings = laser->reset();
                std::vector<Encoder::reading_t> encoder_readings = encoder->reset();
                std::cerr << "Laser: " << laser_readings.size() << " Encoder: " << encoder_readings.size() << std::endl;
                dump(laser_readings, encoder_readings);

                
            }

            print();
        });
}

int main(int argc, char* argv[])
{
    if (argc != 4) {
        std::cerr << "Usage: csv <encoder_port> <laser_port> <output>" << std::endl;
        exit(1);
    }

    csv.open(argv[3], std::ios_base::out | std::ios_base::trunc);

    csv << "marker us,laser value,laser counter,laser us,encoder value,encoder counter,encoder us" << std::endl;
        
    encoder = std::make_shared<Encoder>(io, argv[1]);
    laser = std::make_shared<Laser>(io, argv[2]);

    encoder->init();
    laser->init();

    while (!laser->initialized() || !encoder->initialized()) {
        io.run_one();
    }

    std::cerr << "Starting" << std::endl;

    io.reset();
    start = std::chrono::steady_clock::now();
    laser->start();
    encoder->start();

    timer.expires_at(std::chrono::high_resolution_clock::now());

    print();

    io.run();

    return 0;
}
