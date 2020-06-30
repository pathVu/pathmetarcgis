using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PathMet_V2
{
    class Sensors : ISensors
    {
        private string hostname;
        private int port;
        private Thread thread;
        private bool running = true;

        public bool Connected { get; set; }

        private SensorStatus cameraStatus = SensorStatus.Init;
        private SensorStatus encoderStatus = SensorStatus.Init;
        private SensorStatus imuStatus = SensorStatus.Init;
        private SensorStatus laserStatus = SensorStatus.Init;
        private bool sampling = false;

        public SensorStatus CameraStatus { get { return cameraStatus; } }
        public SensorStatus EncoderStatus { get { return encoderStatus; } }
        public SensorStatus IMUStatus {  get { return imuStatus;  } }
        public SensorStatus LaserStatus {  get { return laserStatus; } }
        public bool Sampling { get { return sampling; } }

        public string directory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double EncoderFinishDist { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Sensors(string hostname, int port)
        {
            this.hostname = hostname;
            this.port = port;
            Connected = false;
            
            thread = new Thread(Run);
            thread.Start();
            
        }

        private TcpClient tcpClient;
        private StreamWriter writer;
        private StreamReader reader;
        Object writerLock = new object();

        private void Run()
        {
            while (running)
            {
                try
                {
                    tcpClient = new TcpClient();
                    tcpClient.Connect(hostname, port);

                    try
                    {
                        tcpClient.ReceiveTimeout = 1000;

                        Connected = true;
                        OnUpdate();

                        writer = new StreamWriter(tcpClient.GetStream());
                        reader = new StreamReader(tcpClient.GetStream());

                        while (running)
                        {
                            string line;
                            lock (writerLock)
                            {
                                writer.WriteLine("status");
                                writer.Flush();
                            }
                            while ((line = reader.ReadLine()) != null && !line.StartsWith("status"))
                            {
                                if (line == "exists")
                                {
                                    OnExists();
                                }
                                else if (line.StartsWith("summary"))
                                {
                                    ParseSummary(line);
                                }
                            }

                            ProcessStatus(line);

                            Thread.Sleep(100);
                        }
                    }
                    finally
                    {
                        Connected = false;
                        tcpClient = null;

                        reader = null;
                        writer = null;
                    }
                }
                catch (Exception)
                {

                }

                OnUpdate();

                Thread.Sleep(5000);
            }
        }

        public void Dispose()
        {
            // TBD
        }

        public void Start(string name)
        {
            lock (writerLock)
            {
                writer.WriteLine("start {0}", name);
                writer.Flush();
            }
        }

        public void Stop()
        {
            lock (writerLock)
            {
                writer.WriteLine("stop");
                writer.Flush();
            }
        }

        public void Flag(string flag)
        {
            lock (writerLock)
            {
                writer.WriteLine("flag {0}", flag);
                writer.Flush();
            }
        }

        public void Restart()
        {
            lock (writerLock)
            {
                writer.WriteLine("exit");
                writer.Flush();
            }
        }

        public event UpdateHandler UpdateEvent;
        private void OnUpdate()
        {
            UpdateEvent?.Invoke();
        }

        public event ExistsHandler ExistsEvent;
        private void OnExists()
        {
            ExistsEvent?.Invoke();
        }

        public event SummaryHandler SummaryEvent;
        private void OnSummary(double laser, double encoder)
        {
            SummaryEvent?.Invoke(laser, encoder);
        }

        void ProcessStatus(string line)
        {
            try
            {
                string[] kvps = line.Split(' ')[1].Split(',');
                foreach (string kvp in kvps)
                {
                    string[] split = kvp.Split('=');
                    string key = split[0];
                    string value = split[1];

                    switch (key)
                    {
                        case "run":
                            int i;
                            Int32.TryParse(value, out i);
                            sampling = i != 0;
                            break;
                        case "camera":
                            Enum.TryParse<SensorStatus>(value, true, out cameraStatus);
                            break;
                        case "encoder":
                            Enum.TryParse<SensorStatus>(value, true, out encoderStatus);
                            break;
                        case "imu":
                            Enum.TryParse<SensorStatus>(value, true, out imuStatus);
                            break;
                        case "laser":
                            Enum.TryParse<SensorStatus>(value, true, out laserStatus);
                            break;
                    }
                }

                OnUpdate();
            }
            catch (Exception)
            {

            }
        }

        void ParseSummary(string line)
        {
            double laser = 0.0;
            double encoder = 0.0;
            
            try
            {
                string[] kvps = line.Split(' ')[1].Split(',');
                foreach (string kvp in kvps)
                {
                    string[] split = kvp.Split('=');
                    string key = split[0];
                    string value = split[1];

                    switch (key)
                    {
                    case "laser":
                        Double.TryParse(value, out laser);
                        break;
                    case "encoder":
                        Double.TryParse(value, out encoder);
                        break;
                    }
                }

                OnSummary(laser, encoder);
            }
            catch (Exception)
            {
                
            }
        }
    }
}
