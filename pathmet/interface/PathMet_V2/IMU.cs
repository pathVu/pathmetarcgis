namespace PathMet_V2
{
    public struct Vector
    {
        public double X;
        public double Y;
        public double Z;
    }
    
    public class IMU
    {
        public bool Good { get; set; }
        public Vector Accelerometer { get { return accelerometer; } }
        public Vector Gyroscope { get { return gyroscope; } }
        public Vector Magnetometer { get { return magnetometer; } }
        public Vector Angle { get { return angle; } }
        
        public void Update(short[] a, short[] g, short[] m, ushort[] e)
        {
            accelerometer = Convert(a, 4.0);
            gyroscope = Convert(g, 2000.0);
            magnetometer = Convert(m, 4192.0);
            angle = Convert(e, 360.0);
        }

        private double Convert(short raw, double scale)
        {
            return raw / 32767.0 * scale;
        }

        private double Convert(ushort raw, double scale)
        {
            return raw / 65535.0 * scale;
        }

        private Vector Convert(short[] raw, double scale)
        {
            Vector v;
            v.X = Convert(raw[0], scale);
            v.Y = Convert(raw[1], scale);
            v.Z = Convert(raw[2], scale);

            return v;
        }

        private Vector Convert(ushort[] raw, double scale)
        {
            Vector v;
            v.X = Convert(raw[0], scale);
            v.Y = Convert(raw[1], scale);
            v.Z = Convert(raw[2], scale);

            return v;
        }

        private Vector accelerometer;
        private Vector gyroscope;
        private Vector magnetometer;
        private Vector angle;
    }
}
