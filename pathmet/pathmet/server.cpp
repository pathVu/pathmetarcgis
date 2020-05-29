#include "server.hpp"

#include <cassert>
#include <iostream>

#include <boost/bind.hpp>
#include <boost/algorithm/string.hpp>

#include "sensors.hpp"

using boost::asio::ip::tcp;

static std::ostream& operator<<(std::ostream& o, SensorStatus status)
{
    switch (status) {
    case SensorStatus::init:
        o << "init";
        break;
    case SensorStatus::ok:
        o << "ok";
        break;
    case SensorStatus::timeout:
        o << "timeout";
        break;
    case SensorStatus::error:
        o << "error";
        break;
    }

    return o;
}

Connection::Connection(boost::asio::io_service& io,
                       std::shared_ptr<Sensors> sensors) :
    io_(io),
    socket_(io),
    sensors_(sensors)
{

}

void Connection::start()
{
    read();
}

void Connection::read()
{
    boost::asio::async_read_until(
        socket_, sb_, "\r\n",
        boost::bind(&Connection::handle_read, shared_from_this(), _1, _2));
}

void Connection::handle_read(boost::system::error_code ec, size_t)
{
    if (!ec) {
        std::istream is(&sb_);
        std::string line;
        std::getline(is, line);

        try {
            std::istringstream iss(line);
            
            std::string command;
            iss >> command;

            if (command == "flag") {
                std::string flag;
                iss >> flag;
                write("flag\r\n");
                sensors_->flag(flag);
            } else if (command == "start") {
                std::string filename;
                std::getline(iss, filename);
                boost::trim(filename);

                if (sensors_->start(filename)) {
                    write("start\r\n");
                } else {
                    write("exists\r\n");
                }
            } else if (command == "stop") {
                write("stop\r\n");
                sensors_->stop();

                std::ostringstream o;
                o << "summary" << " "
                  << "laser=" << sensors_->laser_summary_distance() << ","
                  << "encoder=" << sensors_->encoder_summary_distance()
                  << "\r\n";
                
                write(o.str());
            } else if (command == "status") {
                Sensors::status_t status = sensors_->status();
                std::ostringstream o;
                o << "status" << " "
                  << "run=" << status.running << ","
                  << "camera=" << status.camera << ","
                  << "encoder=" << status.encoder << ","
                  << "imu=" << status.imu << ","
                  << "laser=" << status.laser << "\r\n";

                write(o.str());
            } else if (command == "exit") {
                // you'll get a confirmation when the socket closes!
                io_.stop();
            }
        } catch (std::exception e) {
            
        }
        
        read();
    }
}

void Connection::write(const std::string& message)
{
    if (queue_.empty()) {
        queue_.push(message);
        do_write();
    } else {
        queue_.push(message);
    }
}

void Connection::handle_write(boost::system::error_code ec, size_t)
{
    queue_.pop();
    if (!ec) {
        if (!queue_.empty()) {
            do_write();
        }
    }
}

void Connection::do_write()
{
    assert(!queue_.empty());

    boost::asio::async_write(
        socket_, boost::asio::buffer(queue_.front()), boost::bind(&Connection::handle_write, shared_from_this(), _1, _2));
}

Server::Server(boost::asio::io_service& io,
               uint16_t port,
               std::shared_ptr<Sensors> sensors) :
    io_(io),
    sensors_(sensors),
    acceptor_(io, tcp::endpoint(tcp::v4(), port))
{
    start_accept();
}

void Server::start_accept()
{
    std::shared_ptr<Connection> connection = Connection::create(io_, sensors_);
    acceptor_.async_accept(connection->socket(),
        [=](boost::system::error_code ec) {
            if (!ec) {
                connection->start();
            }

            start_accept();
        });
}
