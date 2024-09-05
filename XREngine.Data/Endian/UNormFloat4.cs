namespace XREngine.Data
{
    /// <summary>
    /// Represents a normalized float value between 0.0 and 1.0, stored in 4 bits.
    /// </summary>
    public struct UNormFloat4
    {
        private byte value;

        // Constructor to initialize from a float between 0.0 and 1.0
        public UNormFloat4(float floatValue)
        {
            if (floatValue < 0.0f || floatValue > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(floatValue), "Value must be between 0.0 and 1.0");

            value = (byte)(floatValue * 15.0f);
            value &= 0x0F; // Ensure only lower 4 bits are used
        }

        public static implicit operator UNormFloat4(float floatValue) => new(floatValue);
        public static implicit operator float(UNormFloat4 fraction) => fraction.value / 15.0f;

        /// <summary>
        /// 0001
        /// </summary>
        public bool Bit0
        {
            readonly get => (value & 0x01) != 0;
            set
            {
                if (value)
                    this.value |= 0x01;
                else
                    this.value &= 0xFE;
            }
        }

        /// <summary>
        /// 0010
        /// </summary>
        public bool Bit1
        {
            readonly get => (value & 0x02) != 0;
            set
            {
                if (value)
                    this.value |= 0x02;
                else
                    this.value &= 0xFD;
            }
        }

        /// <summary>
        /// 0100
        /// </summary>
        public bool Bit2
        {
            readonly get => (value & 0x04) != 0;
            set
            {
                if (value)
                    this.value |= 0x04;
                else
                    this.value &= 0xFB;
            }
        }

        /// <summary>
        /// 1000
        /// </summary>
        public bool Bit3
        {
            readonly get => (value & 0x08) != 0;
            set
            {
                if (value)
                    this.value |= 0x08;
                else
                    this.value &= 0xF7;
            }
        }

        public override readonly string ToString() => ((float)this).ToString("0.###");
    }
}
