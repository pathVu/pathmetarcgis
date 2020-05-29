#include "camera.hpp"

#include <boost/bind.hpp>

enum class CameraErrorCode {
    NotOpen = 1,
    FrameWidth,
    FrameHeight,
    NoGrab,
    NoRead,
};

static CameraErrorCategory camera_error_category;

template<>
struct boost::system::is_error_code_enum<CameraErrorCode> { static const bool value = true; };

boost::system::error_code make_error_code(CameraErrorCode ec) {
    return { static_cast<int>(ec), camera_error_category };
}

std::string CameraErrorCategory::message(int ev) const
{
    switch (static_cast<CameraErrorCode>(ev)) {
    case CameraErrorCode::NotOpen:
        return "Camera not open";
    case CameraErrorCode::FrameWidth:
        return "Failed to set frame width";
    case CameraErrorCode::FrameHeight:
        return "Failed to set frame height";
    case CameraErrorCode::NoGrab:
        return "Failed to grab frame";
    case CameraErrorCode::NoRead:
        return "Failed to read frame";
    }
}

Camera::Camera()
    : video_capture_(0), // default capture
      running_(true)
{
}

Camera::~Camera()
{
    running_ = false;
    thread_.join();
}

void Camera::init()
{
    if (!video_capture_.isOpened()) {
        error_ = CameraErrorCode::NotOpen;
        return;
    }

    if (!video_capture_.set(CV_CAP_PROP_FRAME_WIDTH, 1280)) {
        error_ = CameraErrorCode::FrameWidth;
        return;
    }

    if (!video_capture_.set(CV_CAP_PROP_FRAME_HEIGHT, 720)) {
        error_ = CameraErrorCode::FrameHeight;
        return;
    }

    initialized_ = true;
    
    thread_ = std::thread(boost::bind(&Camera::run, this));
}

SensorStatus Camera::status() const
{
    if (error_) {
        return SensorStatus::error;
    } else if (!initialized_) {
        return SensorStatus::init;
    } else {
        return SensorStatus::ok;
    }
}

void Camera::capture(const std::string& filename)
{
    std::lock_guard<std::mutex> guard(capture_requests_mutex_);
    capture_requests_.push(filename);
}

std::string Camera::check_queue()
{
    std::string result;
    std::lock_guard<std::mutex> guard(capture_requests_mutex_);
    if (capture_requests_.size()) {
        result = capture_requests_.front();
        capture_requests_.pop();
    }

    return result;
}

void Camera::run()
{
    while (running_) {
        if (!error_) {
            std::string filename = check_queue();

            cv::Mat image;
            video_capture_ >> image;
            if (image.empty()) {
                error_ = CameraErrorCode::NoRead;
            } else if (filename.size()) {
                cv::imwrite(filename, image);
            }

            cv::waitKey(33);
        }
    }
}
