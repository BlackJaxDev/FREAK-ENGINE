using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Extensions
{
    public static partial class Ext
    {
        public static string SerializeAsXML<T>(this T value, XmlWriterSettings settings = null)
        {
            if (value == null)
                return string.Empty;

            XmlSerializer xmlSerializer = new(typeof(T));
            using (StringWriter stringWriter = new())
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings ?? new XmlWriterSettings { Indent = true }))
                {
                    xmlSerializer.Serialize(xmlWriter, value);
                    return stringWriter.ToString();
                }
        }
    }
}
