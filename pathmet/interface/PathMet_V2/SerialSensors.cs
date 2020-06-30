using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace PathMet_V2
{
    public class SerialSensors : ISensors
    {
        public bool Connected { get; set; }

        public string directory { get; set; }
        public double EncoderFinishDist { get; set; }

        public SensorStatus CameraStatus { get { return camera.Status; } }
        public SensorStatus EncoderStatus { get { return encoder.Status; } }
        public SensorStatus IMUStatus
        {
            get { return imu.Good ? SensorStatus.OK : SensorStatus.Error; }
        }

        public SensorStatus LaserStatus {
            get { return laser.Ready ? SensorStatus.OK : SensorStatus.Error; }
        }

        public bool Sampling { get { return sampling; } }

        public void Start(string name)
        {
            this.name = name;
            this.directory = Path.Combine(Properties.Settings.Default.LogPath, name);
            imageCount = 0;
            
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            if (File.Exists(directory) || Directory.Exists(directory))
            {
                OnExists();
                return;
            }

            Directory.CreateDirectory(directory);

            lock (csvLock)
            {
                if (csv != null)
                {
                    csv.Dispose();
                    csv = null;

                    gpsFile.Dispose();
                    gpsFile = null;
                }
            }

            serialPort.BaseStream.BeginWrite(RestartCommand, 0, RestartCommand.Length, delegate (IAsyncResult ar) {
                    serialPort.BaseStream.EndWrite(ar);
                    
                    
                }, null);

            lock (csvLock)
            {
                csv = File.CreateText(Path.Combine(directory, name + ".csv"));
                gpsFile = File.CreateText(Path.Combine(directory, name + "_gps.txt"));
                WriteHeader();
            }

            // wait a few milliseconds for the old readings to clear out of the
            // serial port buffer
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += delegate (object source, ElapsedEventArgs e) {
                encoder.Start();
                sampling = true;
            };
            timer.AutoReset = false;
            timer.Interval = 50;
            timer.Enabled = true;
        }

        public void Stop()
        {
            sampling = false;

            //added
            EncoderFinishDist = encoder.Distance;
            
            lock (csvLock)
            {
                if (csv != null)
                {
                    csv.Dispose();
                    csv = null;

                    gpsFile.Dispose();
                    gpsFile = null;
                }
            }

            OnUpdate();
        }
        
        public void Flag(string flag)
        {
            flagsQueue.Enqueue(flag);
        }

        public void Restart()
        {

        }

        public event UpdateHandler UpdateEvent;
        private void OnUpdate()
        {
            UpdateEvent?.Invoke();
        }
        
        public event ExistsHandler ExistsEvent;
        public event SummaryHandler SummaryEvent;
        
        public SerialSensors(string port)
        { 

            encoder.CallbackDistance = 120.0; // inches
            encoder.Callback += CaptureImage;

            sampling = false;
            

            serialPort = new SerialPort(port);

            serialPort.BaudRate = 115200;
            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Handshake = Handshake.None;

            serialPort.DtrEnable = true;
            serialPort.RtsEnable = true;

            serialPort.ReadTimeout = 1000;

            serialPort.Handshake = Handshake.None;

            try
            {
                serialPort.Open();
                serialPort.DiscardInBuffer();

                Connected = true;
                OnUpdate();

                thread = new Thread(Run);
                thread.Start();
            }
            catch (Exception e)
            {
                Connected = false; 
            }
        }

        public void Dispose()
        {
            Console.WriteLine("Dispoing SerialSensors");
            camera.Dispose();
            Console.WriteLine("Camera");
            encoder.Dispose();
            Console.WriteLine("Encoder");
            
            running = false;
            if (thread != null)
            {
                thread.Join();
                thread = null;
            }

            serialPort.Close();
            serialPort.Dispose();

            if (csv != null)
            {
                csv.Dispose();
                csv = null;

                gpsFile.Dispose();
                gpsFile = null;
            }
        }

        enum State {
            WaitForStartDelimiter,
            ReadLength,
            ReadCommand,
            ReadData,
            ReadEndDelimiter
        }

        enum Command {
            Version1GPSMessage = 0x47,
            Version1UpdateMessage = 0x55
        }

        private State state = State.WaitForStartDelimiter;

        private void Run()
        {
            byte command = 0;
            int length = 0;
            List<byte> data = new List<byte>();

            Console.WriteLine("SerialSensors thread running");

            while (running)
            {
                try
                {
                    byte b = (byte) serialPort.BaseStream.ReadByte();

                    switch (state)
                    {
                    case State.WaitForStartDelimiter:
                        if (b == StartDelimiter)
                        {
                            state = State.ReadLength;
                        }
                        break;
                    case State.ReadLength:
                        if (b >= 2) {
                            length = b - 2;
                            state = State.ReadCommand;
                        } else {
                            state = State.WaitForStartDelimiter;
                        }                       
                        break;
                    case State.ReadCommand:
                        command = b;
                        state = State.ReadData;
                        break;
                    case State.ReadData:
                        data.Add(b);
                        if (data.Count == length)
                        {
                            state = State.ReadEndDelimiter;
                        }
                        break;
                    case State.ReadEndDelimiter:
                        if (b == EndDelimiter)
                        {
                            ProcessMessage(command, data.ToArray());
                        }
                        data.Clear();
                        state = State.WaitForStartDelimiter;
                        break;
                    }
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Timeout");
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("Exception");
                    running = false;
                }
            }

            Console.WriteLine("SerialSensors thread stopping");
        }

        private void ProcessMessage(byte command, byte[] data)
        {
            if (command == (byte) Command.Version1UpdateMessage)
            {
                using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
                {
                    ProcessUpdate(reader);
                }
            }
            else if (command == (byte) Command.Version1GPSMessage)
            {
                using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
                {
                    ProcessGPS(reader);
                }
            }
        }

        private void ProcessUpdate(BinaryReader reader)
        {
            byte flags = reader.ReadByte();

            laser.Ready = (flags & (int) Flags.Laser) != 0;
            imu.Good = (flags & (int) Flags.IMU) != 0;
            gps.Good = (flags & (int) Flags.GPS) != 0;

            timestamp = reader.ReadUInt32() / 1000.0;
            encoder.Update(reader.ReadInt32());
            laser.Update(reader.ReadUInt16());
            bool includeImu = false;
            if (reader.BaseStream.Position + 24 <= reader.BaseStream.Length)
            {
                includeImu = true;
                imu.Update(new short[3] {
                        reader.ReadInt16(),
                        reader.ReadInt16(),
                        reader.ReadInt16()
                    },
                    new short[3] {
                        reader.ReadInt16(),
                        reader.ReadInt16(),
                        reader.ReadInt16()
                    },
                    new short[3] {
                        reader.ReadInt16(),
                        reader.ReadInt16(),
                        reader.ReadInt16()
                    },
                    new ushort[3] {
                        reader.ReadUInt16(),
                        reader.ReadUInt16(),
                        reader.ReadUInt16()
                    });
            }

           

            lock (csvLock)
            {
                if (sampling && csv != null)
                {
                    WriteReading(includeImu);
                }
            }

            OnUpdate();
        }

        private void ProcessGPS(BinaryReader reader)
        {
            byte hasFix = reader.ReadByte();
            float latitude = reader.ReadSingle();
            float longitude = reader.ReadSingle();
            float altitude = reader.ReadSingle();

            gps.Update(hasFix == 1, latitude, longitude, altitude);
        }

        private void WriteHeader()
        {
            string[] fields = {
                "timestamp",
                "encoder",
                "laser",
                "accel x",
                "accel y",
                "accel z",
                "gyro x",
                "gyro y",
                "gyro z",
                "mag x",
                "mag y",
                "mag z",
                "angle x",
                "angle y",
                "angle z",
                "flag"
            };

            csv.WriteLine(String.Join(",", fields));
        }

        private void WriteReading(bool includeImu)
        {
            string flag;
            if (!flagsQueue.TryDequeue(out flag))
            {
                flag = String.Empty;
            }

            List<string> fields = new List<string>();

            fields.Add(String.Format("{0}", timestamp));
            fields.Add(String.Format("{0}", encoder.Distance));
            fields.Add(String.Format("{0}", laser.Distance));

            if (includeImu)
            {
                fields.Add(String.Format("{0}", imu.Accelerometer.X));
                fields.Add(String.Format("{0}", imu.Accelerometer.Y));
                fields.Add(String.Format("{0}", imu.Accelerometer.Z));
                
                fields.Add(String.Format("{0}", imu.Gyroscope.X));
                fields.Add(String.Format("{0}", imu.Gyroscope.Y));
                fields.Add(String.Format("{0}", imu.Gyroscope.Z));
                
                fields.Add(String.Format("{0}", imu.Magnetometer.X));
                fields.Add(String.Format("{0}", imu.Magnetometer.Y));
                fields.Add(String.Format("{0}", imu.Magnetometer.Z));
                
                fields.Add(String.Format("{0}", imu.Angle.X));
                fields.Add(String.Format("{0}", imu.Angle.Y));
                fields.Add(String.Format("{0}", imu.Angle.Z));

            }
            else
            {
                for (int i = 0; i < 12; ++i)
                {
                    fields.Add(String.Empty);
                }
            }

            fields.Add(flag);

            lock (csvLock)
            {
                csv.WriteLine(String.Join(",", fields.ToArray()));
            }
        }

        private void OnExists()
        {
            ExistsEvent?.Invoke();
        }

        private void CaptureImage()
        {
            if (sampling)
            {
                string filename = Path.Combine(directory, String.Format("{0:000}.png", imageCount++));
                camera.Capture(filename);

                if (gps.HasFix)
                {
                    gpsFile.WriteLine("{0},{1},{2},{3}m", timestamp, gps.Latitude, gps.Longitude, gps.Altitude);
                }
                else
                {
                    gpsFile.WriteLine("{0},,,", timestamp);
                }
            }
        }

        private int imageCount = 0;
        private string name;
        //private string directory;
        private Thread thread;
        private bool running = true;
        private bool sampling = false;
        
        private static readonly byte StartDelimiter = 0x01;
        private static readonly byte EndDelimiter = 0x04;

        private byte[] RestartCommand = { 0x01, 0x02, 0x53, 0x04 };

        private SerialPort serialPort;

        private object csvLock = new Object();
        private StreamWriter csv;
        private StreamWriter gpsFile;

        private ConcurrentQueue<string> flagsQueue = new ConcurrentQueue<string>();


        enum Flags {
            Laser = 0x01,
            IMU = 0x02,
            GPS = 0x04,
        };

        // the current reading
        private Laser laser = new Laser();
        private Encoder encoder = new Encoder();
        private Camera camera = new Camera();
        private IMU imu = new IMU();
        private GPS gps = new GPS();
        private double timestamp;
    }
}
