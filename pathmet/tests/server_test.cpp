#include "../server.hpp"

#include "../imu.hpp"
#include "../encoder.hpp"
#include "../laser.hpp"
#include "../sensors.hpp"

#include <boost/asio.hpp>

int main(int argc, char* argv[])
{
    boost::asio::io_service io;

    std::shared_ptr<Encoder> encoder;
    std::shared_ptr<Laser> laser;
    std::shared_ptr<IMU> imu;
    std::shared_ptr<Sensors> sensors;

    Server server(io, 10101, sensors);

    io.run();
}
