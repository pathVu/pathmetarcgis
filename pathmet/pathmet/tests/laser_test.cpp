#include "../laser.hpp"

#include <boost/asio/high_resolution_timer.hpp>
#include <iostream>
#include <thread>

boost::asio::io_service io;
boost::asio::high_resolution_timer timer(io);
std::shared_ptr<Laser> laser;

void print()
{
    timer.expires_at(timer.expires_at() + std::chrono::seconds(1));
    timer.async_wait([](boost::system::error_code ec) {
            if (!ec) {
                if (laser->error()) {
                    std::cerr << "Laser: " << laser->error().message() << std::endl;
                } else {
                    std::vector<Laser::reading_t> readings = laser->reset();
                    std::cerr << "Laser: " << readings.size() << " readings per second" << std::endl;
                    std::cerr << "\t(last)" << readings.back().value << " @ " << (unsigned int) readings.back().counter << " (" << std::chrono::duration_cast<std::chrono::seconds>(readings.back().timestamp.time_since_epoch()).count() << ")" << std::endl;
                }
                print();
            }
        });
}

int main(int argc, char* argv[])
{
    if (argc != 2) {
        std::cerr << "Usage: laser <port>" << std::endl;
        exit(1);
    }

    laser = std::make_shared<Laser>(io, argv[1]);
    laser->init();

    while (!laser->initialized()) {
        io.run_one();
    }

    std::cerr << "Laser initialized; starting" << std::endl;

    io.reset();
    laser->start();

    timer.expires_at(std::chrono::high_resolution_clock::now());

    print();

    io.run();

    return 0;
}
