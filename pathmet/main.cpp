#include <cstdint>
#include <iostream>
#include <fstream>

#define WIN32_LEAN_AND_MEAN
#include <boost/stacktrace.hpp>
#include <boost/exception/diagnostic_information.hpp>
#include <boost/iostreams/tee.hpp>
#include <boost/iostreams/stream.hpp>
#include <boost/program_options.hpp>

#include "camera.hpp"
#include "encoder.hpp"
#include "imu.hpp"
#include "laser.hpp"
#include "sensors.hpp"
#include "server.hpp"

namespace po = boost::program_options;

std::string ts()
{
    std::ostringstream s;
    auto now_t = std::chrono::system_clock::to_time_t(std::chrono::system_clock::now());
    s << std::put_time(std::localtime(&now_t), "%F %T") << ": ";
    return s.str();
}

int main(int argc, char* argv[])
{
    uint16_t server_port;
    std::string encoder_port;
    std::string imu_port;
    std::string laser_port;
    std::string logfile;
    double interval;

    std::ifstream config_file("pathmet.cfg");

    try {
        
        po::options_description config("Configuration");
        config.add_options()
            ("help", "produce help message")
            ("port", po::value<uint16_t>(&server_port)->default_value(10101), "server port")
            ("encoder", po::value<std::string>(&encoder_port)->required(), "encoder serial port")
            ("imu", po::value<std::string>(&imu_port)->required(), "IMU serial port")
            ("laser", po::value<std::string>(&laser_port)->required(), "laser serial port")
            ("interval", po::value<double>(&interval)->default_value(120.0), "distance between camera images (in inches)")
            ("logfile", po::value<std::string>(&logfile)->default_value("pathmet.log"));
        
        po::variables_map vm;
        po::store(parse_command_line(argc, argv, config), vm);
        po::store(parse_config_file(config_file, config), vm);

        if (vm.count("help")) {
            std::cout << config << std::endl;
            return 1;
        }
        
        po::notify(vm);
        
    } catch (po::required_option e) {
        std::cerr << "Error: " << e.what() << std::endl;
    } catch (po::unknown_option e) {
        std::cerr << "Error: " << e.what() << std::endl;
    } catch (std::exception e) {
        std::cerr << "Error: " << e.what() << std::endl;
    }

    std::ofstream logstream(logfile, std::ios::out | std::ios::app);
    boost::iostreams::tee_device<std::ostream, std::ostream> tee(std::cerr, logstream);
    boost::iostreams::stream<boost::iostreams::tee_device<std::ostream, std::ostream> > log(tee);

    try {
        log << ts() << "Starting service" << std::endl;
        boost::asio::io_service io;
        boost::asio::deadline_timer timer(io);
        
        std::shared_ptr<Encoder> encoder = std::make_shared<Encoder>(io, encoder_port);
        if (encoder->error()) {
            log << ts() << "Encoder error: " << encoder->error().message() << std::endl;
        }
        
        std::shared_ptr<IMU> imu = std::make_shared<IMU>(io, imu_port);
        if (imu->error()) {
            log << ts() << "IMU error: " << imu->error().message() << std::endl;
        }
        
        std::shared_ptr<Laser> laser = std::make_shared<Laser>(io, laser_port);
        if (laser->error()) {
            log << ts() << "Laser error: " << laser->error().message() << std::endl;
        }
        
        std::shared_ptr<Camera> camera = std::make_shared<Camera>();
        std::shared_ptr<Sensors> sensors = std::make_shared<Sensors>(io, camera, encoder, imu, laser);
        Server server(io, server_port, sensors);

        encoder->callback_distance(interval);
        
        camera->init();
        imu->init();
        laser->init();
        encoder->init();
        
        io.run();
    } catch (std::exception e) {
        log << ts() << "Exception: " << boost::diagnostic_information(e) << std::endl;
        log << boost::stacktrace::stacktrace();
        log << ts() << "Stopping service" << std::endl;
        return -1;
    }
    
    log << ts() << "Stopping service" << std::endl;
    return 0;
}
