#include "encoder.hpp"

#include "fsl_ftm.h"
#include "fsl_port.h"

Encoder encoder;

Encoder::Encoder() :
    last_(0),
    count_(0)
{

}

void Encoder::init()
{
    CLOCK_EnableClock(kCLOCK_PortB);
    
    PORT_SetPinMux(PORTB, 18, kPORT_MuxAlt6);
    PORT_SetPinMux(PORTB, 19, kPORT_MuxAlt6);

    ftm_config_t config;
    FTM_GetDefaultConfig(&config);
    config.prescale = kFTM_Prescale_Divide_2;
    FTM_Init(FTM2, &config);

    FTM_SetQuadDecoderModuloValue(FTM2, 0, UINT16_MAX);
    
    ftm_phase_params_t params;
    params.enablePhaseFilter = true;
    params.phaseFilterVal = 16;
    params.phasePolarity = kFTM_QuadPhaseNormal;

    FTM_SetupQuadDecode(FTM2, &params, &params, kFTM_QuadPhaseEncode);
}

void Encoder::update()
{
    uint16_t current = FTM_GetQuadDecoderCounterValue(FTM2);

    if (current > last_) {
        count_ -= current - last_;
        if (current - last_ > INT16_MAX) {
            count_ += UINT16_MAX;
        }
    } else if (current < last_) {
        count_ += (last_ - current);
        if (last_ - current > INT16_MAX) {
            count_ -= UINT16_MAX;
        }
    }

    last_ = current;
}
