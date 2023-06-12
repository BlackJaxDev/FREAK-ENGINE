using System;
using System.ComponentModel;
using System.Reflection;

namespace Extensions
{
    public static partial class Ext
    {
        public static string GetDescription(this Enum e)
        {
            Type t = e.GetType();
            MemberInfo[] memberInfo = t.GetMember(e.ToString());
            if ((memberInfo != null && memberInfo.Length > 0))
            {
                var attribs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if ((attribs != null && attribs.Length > 0))
                    return ((DescriptionAttribute)attribs[0]).Description;
            }
            return e.ToString();
        }
        //Converts one enum to the other using names.
        public static Enum ConvertByName(this Enum e, Type otherEnum)
        {
            return Enum.Parse(otherEnum, e.ToString()) as Enum;
        }

        public static bool IsSet(this Enum value, Enum flags)
        {
            Type vuType = Enum.GetUnderlyingType(value.GetType());
            Type fuType = Enum.GetUnderlyingType(flags.GetType());
            
            if (vuType == typeof(Byte))
            {
                Byte vValue = Convert.ToByte(value);
                Byte flagValue = 0;

                if (fuType == typeof(Byte))
                    flagValue = (Byte)Convert.ToByte(value);
                else if (fuType == typeof(SByte))
                    flagValue = (Byte)Convert.ToSByte(value);
                else if (fuType == typeof(Int16))
                    flagValue = (Byte)Convert.ToInt16(value);
                else if (fuType == typeof(UInt16))
                    flagValue = (Byte)Convert.ToUInt16(value);
                else if (fuType == typeof(Int32))
                    flagValue = (Byte)Convert.ToInt32(value);
                else if (fuType == typeof(UInt32))
                    flagValue = (Byte)Convert.ToUInt32(value);
                else if (fuType == typeof(Int64))
                    flagValue = (Byte)Convert.ToInt64(value);
                else if (fuType == typeof(UInt64))
                    flagValue = (Byte)Convert.ToUInt64(value);

                return (vValue & flagValue) != 0;
            }
            else if (vuType == typeof(SByte))
            {
                SByte vValue = Convert.ToSByte(value);
                SByte flagValue = 0;

                if (fuType == typeof(Byte))
                    flagValue = (SByte)Convert.ToByte(value);
                else if (fuType == typeof(SByte))
                    flagValue = (SByte)Convert.ToSByte(value);
                else if (fuType == typeof(Int16))
                    flagValue = (SByte)Convert.ToInt16(value);
                else if (fuType == typeof(UInt16))
                    flagValue = (SByte)Convert.ToUInt16(value);
                else if (fuType == typeof(Int32))
                    flagValue = (SByte)Convert.ToInt32(value);
                else if (fuType == typeof(UInt32))
                    flagValue = (SByte)Convert.ToUInt32(value);
                else if (fuType == typeof(Int64))
                    flagValue = (SByte)Convert.ToInt64(value);
                else if (fuType == typeof(UInt64))
                    flagValue = (SByte)Convert.ToUInt64(value);

                return (vValue & flagValue) != 0;
            }
            else if (vuType == typeof(Int16))
            {
                Int16 vValue = Convert.ToInt16(value);
                Int16 flagValue = 0;

                if (fuType == typeof(Byte))
                    flagValue = (Int16)Convert.ToByte(value);
                else if (fuType == typeof(SByte))
                    flagValue = (Int16)Convert.ToSByte(value);
                else if (fuType == typeof(Int16))
                    flagValue = (Int16)Convert.ToInt16(value);
                else if (fuType == typeof(UInt16))
                    flagValue = (Int16)Convert.ToUInt16(value);
                else if (fuType == typeof(Int32))
                    flagValue = (Int16)Convert.ToInt32(value);
                else if (fuType == typeof(UInt32))
                    flagValue = (Int16)Convert.ToUInt32(value);
                else if (fuType == typeof(Int64))
                    flagValue = (Int16)Convert.ToInt64(value);
                else if (fuType == typeof(UInt64))
                    flagValue = (Int16)Convert.ToUInt64(value);

                return (vValue & flagValue) != 0;
            }
            else if (vuType == typeof(UInt16))
            {
                UInt16 vValue = Convert.ToUInt16(value);
                UInt16 flagValue = 0;

                if (fuType == typeof(Byte))
                    flagValue = (UInt16)Convert.ToByte(value);
                else if (fuType == typeof(SByte))
                    flagValue = (UInt16)Convert.ToSByte(value);
                else if (fuType == typeof(Int16))
                    flagValue = (UInt16)Convert.ToInt16(value);
                else if (fuType == typeof(UInt16))
                    flagValue = (UInt16)Convert.ToUInt16(value);
                else if (fuType == typeof(Int32))
                    flagValue = (UInt16)Convert.ToInt32(value);
                else if (fuType == typeof(UInt32))
                    flagValue = (UInt16)Convert.ToUInt32(value);
                else if (fuType == typeof(Int64))
                    flagValue = (UInt16)Convert.ToInt64(value);
                else if (fuType == typeof(UInt64))
                    flagValue = (UInt16)Convert.ToUInt64(value);

                return (vValue & flagValue) != 0;
            }
            else if (vuType == typeof(Int32))
            {
                Int32 vValue = Convert.ToInt32(value);
                Int32 flagValue = 0;

                if (fuType == typeof(Byte))
                    flagValue = (Int32)Convert.ToByte(value);
                else if (fuType == typeof(SByte))
                    flagValue = (Int32)Convert.ToSByte(value);
                else if (fuType == typeof(Int16))
                    flagValue = (Int32)Convert.ToInt16(value);
                else if (fuType == typeof(UInt16))
                    flagValue = (Int32)Convert.ToUInt16(value);
                else if (fuType == typeof(Int32))
                    flagValue = (Int32)Convert.ToInt32(value);
                else if (fuType == typeof(UInt32))
                    flagValue = (Int32)Convert.ToUInt32(value);
                else if (fuType == typeof(Int64))
                    flagValue = (Int32)Convert.ToInt64(value);
                else if (fuType == typeof(UInt64))
                    flagValue = (Int32)Convert.ToUInt64(value);

                return (vValue & flagValue) != 0;
            }
            else if (vuType == typeof(UInt32))
            {
                UInt32 vValue = Convert.ToUInt32(value);
                UInt32 flagValue = 0;

                if (fuType == typeof(Byte))
                    flagValue = (UInt32)Convert.ToByte(value);
                else if (fuType == typeof(SByte))
                    flagValue = (UInt32)Convert.ToSByte(value);
                else if (fuType == typeof(Int16))
                    flagValue = (UInt32)Convert.ToInt16(value);
                else if (fuType == typeof(UInt16))
                    flagValue = (UInt32)Convert.ToUInt16(value);
                else if (fuType == typeof(Int32))
                    flagValue = (UInt32)Convert.ToInt32(value);
                else if (fuType == typeof(UInt32))
                    flagValue = (UInt32)Convert.ToUInt32(value);
                else if (fuType == typeof(Int64))
                    flagValue = (UInt32)Convert.ToInt64(value);
                else if (fuType == typeof(UInt64))
                    flagValue = (UInt32)Convert.ToUInt64(value);

                return (vValue & flagValue) != 0;
            }
            else if (vuType == typeof(Int64))
            {
                Int64 vValue = Convert.ToInt64(value);
                Int64 flagValue = 0;

                if (fuType == typeof(Byte))
                    flagValue = (Int64)Convert.ToByte(value);
                else if (fuType == typeof(SByte))
                    flagValue = (Int64)Convert.ToSByte(value);
                else if (fuType == typeof(Int16))
                    flagValue = (Int64)Convert.ToInt16(value);
                else if (fuType == typeof(UInt16))
                    flagValue = (Int64)Convert.ToUInt16(value);
                else if (fuType == typeof(Int32))
                    flagValue = (Int64)Convert.ToInt32(value);
                else if (fuType == typeof(UInt32))
                    flagValue = (Int64)Convert.ToUInt32(value);
                else if (fuType == typeof(Int64))
                    flagValue = (Int64)Convert.ToInt64(value);
                else if (fuType == typeof(UInt64))
                    flagValue = (Int64)Convert.ToUInt64(value);

                return (vValue & flagValue) != 0;
            }
            else if (vuType == typeof(UInt64))
            {
                UInt64 vValue = Convert.ToUInt64(value);
                UInt64 flagValue = 0;

                if (fuType == typeof(Byte))
                    flagValue = (UInt64)Convert.ToByte(value);
                else if (fuType == typeof(SByte))
                    flagValue = (UInt64)Convert.ToSByte(value);
                else if (fuType == typeof(Int16))
                    flagValue = (UInt64)Convert.ToInt16(value);
                else if (fuType == typeof(UInt16))
                    flagValue = (UInt64)Convert.ToUInt16(value);
                else if (fuType == typeof(Int32))
                    flagValue = (UInt64)Convert.ToInt32(value);
                else if (fuType == typeof(UInt32))
                    flagValue = (UInt64)Convert.ToUInt32(value);
                else if (fuType == typeof(Int64))
                    flagValue = (UInt64)Convert.ToInt64(value);
                else if (fuType == typeof(UInt64))
                    flagValue = (UInt64)Convert.ToUInt64(value);

                return (vValue & flagValue) != 0;
            }
            
            return false;
        }
    }
}
