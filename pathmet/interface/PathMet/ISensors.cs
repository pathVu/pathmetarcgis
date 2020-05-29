using System;

namespace PathMet
{
    public enum SensorStatus
    {
        Init,
        OK,
        Timeout,
        Error
    }

    public delegate void UpdateHandler();
    public delegate void ExistsHandler();
    public delegate void SummaryHandler(double laser, double encoder);
    
    public interface ISensors : IDisposable
    {
        bool Connected { get; }
        SensorStatus CameraStatus { get; }
        SensorStatus EncoderStatus { get; }
        SensorStatus IMUStatus { get; }
        SensorStatus LaserStatus { get; }
        bool Sampling { get; }

        event UpdateHandler UpdateEvent;
        event ExistsHandler ExistsEvent;
        event SummaryHandler SummaryEvent;

        void Start(string name);
        void Stop();
        void Flag(string flag);
        void Restart();
    }
}
