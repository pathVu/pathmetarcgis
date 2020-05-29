namespace PathMet
{
    public class GPS
    {
        public bool Good { get; set; }

        public void Update(bool hasFix, float latitude, float longitude, float altitude)
        {
            HasFix = hasFix;
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }

        public bool HasFix { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Altitude { get; set; }
    }
}
