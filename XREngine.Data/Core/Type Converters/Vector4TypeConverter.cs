using System.ComponentModel;
using System.Globalization;
using System.Numerics;

namespace XREngine.Data.Core.TypeConverters
{
    public class Vector4TypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string str)
            {
                string[] parts = str.Split(',');
                if (parts.Length == 4)
                    return new Vector4(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
            }
            return base.ConvertFrom(context, culture, value);
        }
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Vector4 v)
                return $"{v.X}, {v.Y}, {v.Z}, {v.W}";
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
