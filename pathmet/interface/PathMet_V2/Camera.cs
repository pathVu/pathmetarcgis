using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace PathMet_V2
{
    public class Camera : IDisposable
    {
        public SensorStatus Status
        {
            get { return status; }
        }

        public Camera()
        {
            capture = new VideoCapture(Properties.Settings.Default.CameraIndex);
            capture.Set(CaptureProperty.FrameWidth, 1280);
            capture.Set(CaptureProperty.FrameHeight, 720);
            
            thread = new Thread(Run);
            thread.Start();
        }

        public void Dispose()
        {
            running = false;

            if (thread != null)
            {
                thread.Join();
                thread = null;
            }
        }

        public void Capture(string filename)
        {
            captureQueue.Enqueue(filename);
        }

        private void Run()
        {
            using (Mat image = new Mat())
            {
                while (running)
                {
                    capture.Read(image);

                    if (image.Empty())
                    {
                        status = SensorStatus.Error;
                    }
                    else
                    {
                        status = SensorStatus.OK;

                        string filename;
                        if (captureQueue.TryDequeue(out filename))
                        {
                            Cv2.ImWrite(filename, image);
                        }

                        Cv2.WaitKey(33);
                    }
                }
            }
        }

        private SensorStatus status = SensorStatus.Init;
        private Thread thread;
        private bool running = true;
        private VideoCapture capture;

        private ConcurrentQueue<string> captureQueue = new ConcurrentQueue<string>();
    }
}
