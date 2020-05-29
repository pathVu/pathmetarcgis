#include "../encoder.hpp"

#include <iostream>
#include <thread>

#include <boost/asio/high_resolution_timer.hpp>

boost::asio::io_service io;
boost::asio::high_resolution_timer timer(io);
std::shared_ptr<Encoder> encoder;

void print()
{
    timer.expires_at(timer.expires_at() + std::chrono::seconds(1));
    timer.async_wait([](boost::system::error_code ec) {
            if (!ec) {
                if (encoder->error()) {
                    std::cerr << "Encoder: " << encoder->error().message() << std::endl;
                } else {
                    std::vector<Encoder::reading_t> readings = encoder->reset();
                    std::cerr << "Encoder: " << readings.size() << " readings per second" << std::endl;
                    std::cerr << "\t(last)" << readings.back().value << " @ " << (unsigned int) readings.back().counter << " (" << std::chrono::duration_cast<std::chrono::seconds>(readings.back().timestamp.time_since_epoch()).count() << ")" << std::endl;
                }
                print();
            }
        });
}

int main(int argc, char* argv[])
{
    if (argc != 2) {
        std::cerr << "Usage: encoder <port>" << std::endl;
        exit(1);
    }

    encoder = std::make_shared<Encoder>(io, argv[1]);
    encoder->init();

    while (!encoder->initialized()) {
        io.run_one();
    }

    std::cerr << "Encoder initialized; starting" << std::endl;

    io.reset();
    encoder->start();

    timer.expires_at(std::chrono::high_resolution_clock::now());
    print();

    io.run();

    std::string line;
    std::getline(std::cin, line);

    return 0;
}
