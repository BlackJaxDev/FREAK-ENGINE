﻿using Extensions;
using System.Runtime.InteropServices;

namespace XREngine.Core.Memory
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Bin64
    {
        public ulong _data;
        
        public Bin64(ulong val) { _data = val; }

        public static implicit operator ulong(Bin64 val) { return val._data; }
        public static implicit operator Bin64(ulong val) { return new Bin64(val); }
        //public static implicit operator long(Bin64 val) { return (long)val._data; }
        //public static implicit operator Bin64(long val) { return new Bin64((ulong)val); }

        public override string ToString()
        {
            int i = 0;
            string val = "";
            while (i++ < 64)
            {
                val += (_data >> (64 - i)) & 1;
                if (i % 4 == 0 && i != 64)
                    val += " ";
            }
            return val;
        }

        public bool this[int index]
        {
            get { return (_data >> index & 1) != 0; }
            set
            {
                if (value)
                    _data |= (uint)(1 << index);
                else
                    _data &= ~(uint)(1 << index);
            }
        }

        //public uint this[int shift, int mask]
        //{
        //    get { return (uint)(data >> shift & mask); }
        //    set { data = (uint)((data & ~(mask << shift)) | ((value & mask) << shift)); }
        //}

        public ulong this[int shift, int bitCount]
        {
            get
            {
                ulong mask = 0;
                for (int i = 0; i < bitCount; i++)
                    mask |= 1u << i;
                return (_data >> shift) & mask;
            }
            set
            {
                ulong mask = 0;
                for (int i = 0; i < bitCount; i++)
                    mask |= 1u << i;
                _data = (_data & ~(mask << shift)) | ((value & mask) << shift);
            }
        }

        public static Bin64 FromString(string s)
        {
            char[] delims = new char[] { ',', '(', ')', ' ' };

            uint b = 0;
            string[] arr = s.Split(delims, StringSplitOptions.RemoveEmptyEntries);

            for (int len = 0; len < arr.Length; len++)
            {
                byte bit = 0;
                for (int i = 0; i < arr[len].Length; i++)
                {
                    b <<= 1;
                    byte.TryParse(arr[len][i].ToString(), out bit);
                    bit = bit.Clamp(0, 1);
                    b += bit;
                }
            }

            return new Bin64(b);
        }
    }
}