using System;
using System.Timers;

namespace PathMet_V2
{
    public class Encoder : IDisposable
    {
        public SensorStatus Status { get { return status; } }
        public double Distance { get { return distance; } }

        public Encoder() {
            timer = new Timer();
            timer.Elapsed += CheckForMotion;
            timer.Interval = 1000;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public void Dispose()
        {
            timer.Enabled = false;
        }
        
        public void Update(int counts)
        {
            distance = counts / Properties.Settings.Default.EncoderCountsPerInch;

            if (distance > nextCallbackDistance)
            {
                OnCallback();
                nextCallbackDistance += CallbackDistance;
            }

            if (distance > maxDistance)
            {
                maxDistance = distance;
            }

            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        public void Start()
        {
            nextCallbackDistance = 0;
        }

        private SensorStatus status = SensorStatus.Init;
        public delegate void CallbackHandler();
        public event CallbackHandler Callback;
        public double CallbackDistance { get; set; }

        private void OnCallback()
        {
            Callback?.Invoke();
        }

        private void CheckForMotion(object source, ElapsedEventArgs e)
        {
            double distance = this.Distance;

            if (maxDistance - minDistance > 0.01)
            {
                status = SensorStatus.OK;
            }
            else
            {
                status = SensorStatus.Error;
            }

            maxDistance = distance;
            minDistance = distance;
        }

        private Timer timer;

        private double nextCallbackDistance;

        private double distance;
        private double minDistance;
        private double maxDistance;
    }
}
