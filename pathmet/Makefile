all: main

main: main.cpp encoder.cpp encoder.hpp flush.hpp flush.cpp imu.hpp imu.cpp laser.hpp laser.cpp logger.hpp logger.cpp
	g++ -o $@ -std=c++11 main.cpp encoder.cpp flush.cpp imu.cpp laser.cpp logger.cpp -lboost_program_options -lpthread -lboost_system
