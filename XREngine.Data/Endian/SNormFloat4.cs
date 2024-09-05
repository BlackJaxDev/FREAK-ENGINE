namespace XREngine.Data
{
    /// <summary>
    /// Represents a normalized float value between -1.0 and 1.0, stored in 4 bits.
    /// </summary>
    public struct SNormFloat4
    {
        private byte _value;

        // Constructor to initialize from a float between -1.0 and 1.0
        public SNormFloat4(float floatValue)
        {
            if (floatValue < -1.0f || floatValue > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(floatValue), "Value must be between -1.0 and 1.0");

            _value = (byte)((floatValue + 1.0f) * 7.5f);
            _value &= 0x0F; // Ensure only lower 4 bits are used
        }

        public static implicit operator SNormFloat4(float floatValue) => new(floatValue);
        public static implicit operator float(SNormFloat4 fraction) => (fraction._value / 7.5f) - 1.0f;

        /// <summary>
        /// 0001
        /// </summary>
        public bool Bit0
        {
            readonly get => (_value & 0x01) != 0;
            set
            {
                if (value)
                    this._value |= 0x01;
                else
                    this._value &= 0xFE;
            }
        }

        /// <summary>
        /// 0010
        /// </summary>
        public bool Bit1
        {
            readonly get => (_value & 0x02) != 0;
            set
            {
                if (value)
                    this._value |= 0x02;
                else
                    this._value &= 0xFD;
            }
        }

        /// <summary>
        /// 0100
        /// </summary>
        public bool Bit2
        {
            readonly get => (_value & 0x04) != 0;
            set
            {
                if (value)
                    this._value |= 0x04;
                else
                    this._value &= 0xFB;
            }
        }

        /// <summary>
        /// 1000
        /// </summary>
        public bool Bit3
        {
            readonly get => (_value & 0x08) != 0;
            set
            {
                if (value)
                    this._value |= 0x08;
                else
                    this._value &= 0xF7;
            }
        }

        public override readonly string ToString() => ((float)this).ToString("0.###");
    }
}
