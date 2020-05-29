#include "flush.hpp"

boost::system::error_code flush(boost::asio::serial_port& serial_port)
{
    boost::system::error_code ec;

#if !defined(BOOST_WINDOWS) && !defined(__CYGWIN__)
    if (!!::tcflush(serial_port.native(), TCIOFLUSH)) {
        ec = boost::system::error_code(errno, boost::asio::error::get_system_category());
    }
#else
    if (!::PurgeComm(serial_port.native(), PURGE_RXABORT | PURGE_RXCLEAR | PURGE_TXABORT | PURGE_TXCLEAR)) {
        ec = boost::system::error_code(::GetLastError(), boost::asio::error::get_system_category());
    }
#endif
    return ec;
}
