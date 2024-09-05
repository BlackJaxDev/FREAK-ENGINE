namespace XREngine.Data
{
    /// <summary>
    /// Represents a normalized float value between -1.0 and 1.0, stored in 8 bits.
    /// </summary>
    public readonly struct SNormFloat8
    {
        private readonly sbyte _value;

        // Constructor to initialize from a float between -1.0 and 1.0
        public SNormFloat8(float floatValue)
        {
            if (floatValue < -1.0f || floatValue > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(floatValue), "Value must be between -1.0 and 1.0");

            _value = (sbyte)(floatValue * 127.0f);
        }

        public static implicit operator SNormFloat8(float floatValue) => new(floatValue);
        public static implicit operator float(SNormFloat8 fraction) => fraction._value / 127.0f;

        public override string ToString() => ((float)this).ToString("0.###");
    }
}
