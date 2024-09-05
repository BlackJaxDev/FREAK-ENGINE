namespace XREngine.Data
{
    /// <summary>
    /// Represents a normalized float value between 0.0 and 1.0, stored in 8 bits.
    /// </summary>
    public readonly struct UNormFloat8
    {
        private readonly byte _value;

        // Constructor to initialize from a float between 0.0 and 1.0
        public UNormFloat8(float floatValue)
        {
            if (floatValue < 0.0f || floatValue > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(floatValue), "Value must be between 0.0 and 1.0");

            _value = (byte)(floatValue * 255.0f);
        }

        public static implicit operator UNormFloat8(float floatValue) => new(floatValue);
        public static implicit operator float(UNormFloat8 fraction) => fraction._value / 255.0f;

        public override string ToString() => ((float)this).ToString("0.###");
    }
}
