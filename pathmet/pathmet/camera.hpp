#ifndef CAMERA_HPP
#define CAMERA_HPP

#include <mutex>
#include <string>
#include <thread>

#include <boost/system/error_code.hpp>

#include "opencv2/opencv.hpp"

#include "sensor_status.hpp"

class CameraErrorCategory : public boost::system::error_category {
public:
    const char* name() const noexcept { return "Camera"; }
    std::string message(int ev) const;
};

class Camera {
public:
    Camera();
    ~Camera();

    void init();
    void capture(const std::string& filename);

    boost::system::error_code error() const { return error_; }
    bool initialized() const { return initialized_; }
    SensorStatus status() const;

private:
    cv::VideoCapture video_capture_;
    std::mutex capture_requests_mutex_;
    std::queue<std::string> capture_requests_;
    std::thread thread_;

    bool initialized_;
    boost::system::error_code error_;

    bool running_;
    std::string check_queue();
    void run();
};

#endif
