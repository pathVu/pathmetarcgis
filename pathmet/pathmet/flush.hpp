#ifndef FLUSH_HPP
#define FLUSH_HPP

#include <boost/asio.hpp>

boost::system::error_code flush(boost::asio::serial_port& serial_port);

#endif
