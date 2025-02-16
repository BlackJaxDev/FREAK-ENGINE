using System.ComponentModel;
using System.Globalization;
using System.Numerics;

namespace XREngine.Data.Core.TypeConverters
{
    public class Vector2TypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string str)
            {
                string[] parts = str.Split(',');
                if (parts.Length == 2)
                    return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
            }
            return base.ConvertFrom(context, culture, value);
        }
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Vector2 v)
                return $"{v.X}, {v.Y}";
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
