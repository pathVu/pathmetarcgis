#include "imu.hpp"

#include <chrono>

#include "clock.hpp"

#include "Invn/Devices/Drivers/Icm20948/Icm20948.h"

#include "fsl_debug_console.h"

#include "i2c.hpp"

extern "C" void inv_icm20948_sleep_us(int us)
{
    clock::sleep_for(std::chrono::microseconds(us));
}

extern "C" uint64_t inv_icm20948_get_time_us(void)
{
    return std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::time_point_cast<std::chrono::microseconds>(clock::now()).time_since_epoch()).count();
}

static const uint8_t dmp3_image[] = {
#include "imu/icm20948_img.dmp3a.h"
};

IMU imu;

IMU::IMU() :
    device_{ new struct inv_icm20948 },
    initialized_{false},
    triggered_{false}
{

}

void IMU::init()
{
    printf("Initializing IMU\n");

    initialized_ = false;
    
    struct inv_icm20948_serif icm20948_serif;

    icm20948_serif.context = this;
    icm20948_serif.read_reg =
        [] (void* context, uint8_t reg, uint8_t* rbuffer, uint32_t rlen) -> int {
            return i2c1.read(reinterpret_cast<IMU*>(context)->address, reg, rbuffer, rlen) ? 0 : -1;
        };
    icm20948_serif.write_reg =
        [] (void* context, uint8_t reg, const uint8_t* wbuffer, uint32_t rlen) -> int {
            return i2c1.write(reinterpret_cast<IMU*>(context)->address, reg, wbuffer, rlen) ? 0 : -1;
        };
    icm20948_serif.max_read = UINT16_MAX;
    icm20948_serif.max_write = UINT16_MAX;
    icm20948_serif.is_spi = false;

    inv_icm20948_reset_states(device_.get(), &icm20948_serif);

    inv_icm20948_init_matrix(device_.get());
    apply_mounting_matrix();

    if (inv_icm20948_initialize(device_.get(), dmp3_image, sizeof(dmp3_image))) {
        printf("Failed to load DMP3\n");
        return;
    }

    inv_icm20948_register_aux_compass(device_.get(), INV_ICM20948_COMPASS_ID_AK09916, magnetometer);
    if (inv_icm20948_initialize_auxiliary(device_.get())) {
        printf("Failed to initialize auxiliary\n");
        return;
    }

    set_fsr();

    inv_icm20948_init_structure(device_.get());

    const inv_icm20948_sensor sensors[] = {
        INV_ICM20948_SENSOR_ACCELEROMETER,
        INV_ICM20948_SENSOR_GYROSCOPE,
        INV_ICM20948_SENSOR_GEOMAGNETIC_FIELD,
        INV_ICM20948_SENSOR_ORIENTATION
    };

    for (auto s : sensors) {
        if (inv_icm20948_enable_sensor(device_.get(), s, true) != 0) {
            printf("Failed to enable sensor %i\n", s);
            return;
        }

        if (inv_icm20948_set_sensor_period(device_.get(), s, 1) != 0) {
            printf("Failed to set sensor %i period\n", s);
            return;
        }
    }

    initialized_ = true;

    printf("IMU initialized\n");
}

void IMU::trigger()
{
    reading_ = temp_reading_;
    triggered_ = true;
}

void IMU::update()
{
    inv_icm20948_poll_sensor(
        device_.get(), this,
        [] (void* context, inv_icm20948_sensor sensortype, uint64_t timestamp, const void* data, const void* arg) {
            auto imu = reinterpret_cast<IMU*>(context);
            auto vec = reinterpret_cast<const float*>(data);
            switch (sensortype) {
            case INV_ICM20948_SENSOR_ACCELEROMETER:
                imu->temp_reading_.ax = vec[0];
                imu->temp_reading_.ay = vec[1];
                imu->temp_reading_.az = vec[2];
                break;
            case INV_ICM20948_SENSOR_GYROSCOPE:
                imu->temp_reading_.gx = vec[0];
                imu->temp_reading_.gy = vec[1];
                imu->temp_reading_.gz = vec[2];
                break;
            case INV_ICM20948_SENSOR_GEOMAGNETIC_FIELD:
                imu->temp_reading_.mx = vec[0];
                imu->temp_reading_.my = vec[1];
                imu->temp_reading_.mz = vec[2];
                break;
            case INV_ICM20948_SENSOR_ORIENTATION:
                imu->last_reading_ = clock::now();
                imu->temp_reading_.ex = vec[0];
                imu->temp_reading_.ey = vec[1];
                imu->temp_reading_.ez = vec[2];
                break;
            default:
                break;
            }
        });
}

void IMU::apply_mounting_matrix()
{
    static const float matrix[9]= {
	1.f, 0, 0,
	0, 1.f, 0,
	0, 0, 1.f
    };
    
    for (int i = 0; i < INV_ICM20948_SENSOR_MAX; ++i) {
        inv_icm20948_set_matrix(device_.get(), matrix, static_cast<inv_icm20948_sensor>(i));
    }
}

void IMU::set_fsr()
{
    const int32_t acc_fsr = 4; // Default = +/- 4g. Valid ranges: 2, 4, 8, 16
    const int32_t gyr_fsr = 2000; // Default = +/- 2000dps. Valid ranges: 250, 500, 1000, 2000
    
    inv_icm20948_set_fsr(device_.get(), INV_ICM20948_SENSOR_RAW_ACCELEROMETER, reinterpret_cast<const void *>(&acc_fsr));
    inv_icm20948_set_fsr(device_.get(), INV_ICM20948_SENSOR_ACCELEROMETER, reinterpret_cast<const void *>(&acc_fsr));
    inv_icm20948_set_fsr(device_.get(), INV_ICM20948_SENSOR_RAW_GYROSCOPE, reinterpret_cast<const void *>(gyr_fsr));
    inv_icm20948_set_fsr(device_.get(), INV_ICM20948_SENSOR_GYROSCOPE, reinterpret_cast<const void *>(&gyr_fsr));
    inv_icm20948_set_fsr(device_.get(), INV_ICM20948_SENSOR_GYROSCOPE_UNCALIBRATED, reinterpret_cast<const void *>(&gyr_fsr));
}
