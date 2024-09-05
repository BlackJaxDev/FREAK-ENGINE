using Extensions;

namespace System
{
    public class FeetInches
    {
        private float _inches = 0.0f;

        public int Feet { get; set; }
        public float Inches
        {
            get => _inches;
            set
            {
                _inches = value;
                float ft = _inches / 12.0f;
                if (ft > 1.0f)
                {
                    int ift = (int)Math.Floor(ft);
                    Feet += ift;
                    _inches -= ift * 12;
                }
                else if (ft < 1.0f)
                {

                }
            }
        }
        
        public FeetInches(int feet, float inches)
        {
            Feet = feet;
            Inches = inches;
        }
        public static FeetInches FromInches(float inches)
            => FromFeet(inches / 12.0f);
        public static FeetInches FromFeet(float feet)
        {
            int ift = (int)Math.Floor(feet);
            return new FeetInches(ift, (feet - ift) * 12.0f);
        }
        public float ToFeet()
            => Feet + Inches / 12.0f;
        public float ToInches()
            => Feet * 12.0f + Inches;
        public float ToMeters()
            => ToFeet().FeetToMeters();
    }
}
