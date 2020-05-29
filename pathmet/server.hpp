#ifndef SERVER_HPP
#define SERVER_HPP

#include <queue>

#include <boost/asio.hpp>

class Sensors;

using boost::asio::ip::tcp;

class Connection : public std::enable_shared_from_this<Connection> {
public:
    static std::shared_ptr<Connection> create(
        boost::asio::io_service& io,
        std::shared_ptr<Sensors> sensors) {        
        return std::shared_ptr<Connection>(new Connection(io, sensors));
    }
    
    tcp::socket& socket() { return socket_; }

    void start();

private:
    Connection(boost::asio::io_service& io,
               std::shared_ptr<Sensors> sensors);

    boost::asio::io_service& io_;
    tcp::socket socket_;
    std::shared_ptr<Sensors> sensors_;

    boost::asio::streambuf sb_;

    void read();
    void write(const std::string& message);
    void handle_read(boost::system::error_code, size_t);
    void handle_write(boost::system::error_code, size_t);
    void do_write();
    std::queue<std::string> queue_;
};

class Server {
public:
    Server(boost::asio::io_service& io,
           uint16_t port,
           std::shared_ptr<Sensors> sensors);
    
private:
    boost::asio::io_service& io_;

    std::shared_ptr<Sensors> sensors_;

    boost::asio::ip::tcp::acceptor acceptor_;

    void start_accept();
};

#endif
