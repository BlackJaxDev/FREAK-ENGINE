using XREngine.Core.Files;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace XREngine
{
    public class XRAssetYamlTypeConverter : IYamlTypeConverter
    {
        public const string ID = "ID";

        public bool Accepts(Type type)
            => typeof(XRAsset).IsAssignableFrom(type);

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            if (!parser.Accept<Scalar>(out var scalar) || scalar is null || !string.Equals(scalar.Value, ID, StringComparison.OrdinalIgnoreCase))
                return null;

            parser.MoveNext();
            if (!parser.Accept<Scalar>(out var id) || id is null)
                return null;
            
            parser.MoveNext();
            return Engine.Assets.GetAssetByID(Guid.Parse(id.Value));
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            if (value is not XRAsset source)
                return;

            emitter.Emit(new Scalar(ID));
            emitter.Emit(new Scalar(source.ID.ToString()));
        }
    }
}
