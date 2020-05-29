all: laser encoder csv imu server

laser: laser_test.cpp ../laser.cpp ../laser.hpp ../flush.hpp ../flush.cpp
	g++ -o $@ -std=c++11 laser_test.cpp ../laser.cpp ../flush.cpp -lboost_system

encoder: encoder_test.cpp ../encoder.cpp ../encoder.hpp ../flush.cpp ../flush.hpp
	g++ -o $@ -std=c++11 encoder_test.cpp ../encoder.cpp ../flush.cpp -lboost_system

csv: csv.cpp ../encoder.cpp ../encoder.hpp ../laser.cpp ../laser.hpp ../flush.cpp ../flush.hpp
	g++ -o $@ -std=c++11 csv.cpp ../laser.cpp ../encoder.cpp ../flush.cpp -lboost_system

imu: imu_test.cpp ../imu.cpp ../imu.hpp ../flush.cpp ../flush.hpp
	g++ -o $@ -std=c++11 imu_test.cpp ../imu.cpp ../flush.cpp -lboost_system

server: server_test.cpp ../server.cpp ../server.hpp ../encoder.cpp ../encoder.hpp ../laser.hpp ../laser.hpp ../imu.hpp ../imu.cpp
	g++ -o $@ -std=c++11 server_test.cpp ../server.cpp ../encoder.cpp ../imu.cpp ../laser.cpp -lboost_system
