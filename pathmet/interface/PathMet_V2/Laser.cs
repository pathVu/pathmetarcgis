namespace PathMet_V2
{
    public class Laser
    {
        public double Distance { get { return distance; } }
        public bool Ready { get; set; }
        
        public void Update(ushort counts)
        {
            distance = BaseInches + counts * InchesPerCount;
        }

        // laser reports distance minus a base range
        private static readonly double BaseInches = 4.92126;

        // laser's maximum range is 500mm
        // it's normalized to 16384 counts at max range
        private static readonly double InchesPerCount = 19.685 / 16384;

        private double distance;
    }
}
