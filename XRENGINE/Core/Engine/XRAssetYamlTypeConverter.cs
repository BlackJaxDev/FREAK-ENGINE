using SevenZip.CommandLineParser;
using System.Diagnostics.CodeAnalysis;
using XREngine.Core.Files;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace XREngine
{
    //public class XRAssetYamlTypeConverter : IYamlTypeConverter
    //{
    //    public const string ID = "ID";

    //    public bool Accepts(Type type)
    //        => typeof(XRAsset).IsAssignableFrom(type);

    //    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    //    {
    //        parser.Consume<MappingStart>();
    //        parser.Consume<Scalar>();
    //        var id = parser.Consume<Scalar>();
    //        parser.Consume<MappingEnd>();

    //        return Engine.Assets.GetAssetByID(Guid.Parse(id.Value));
    //    }

    //    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    //    {
    //        if (value is not XRAsset source)
    //            return;

    //        emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));
    //        {
    //            emitter.Emit(new Scalar(ID));
    //            emitter.Emit(new Scalar(source.ID.ToString()));
    //        }
    //        emitter.Emit(new MappingEnd());
    //    }
    //}

    public class DepthTrackingNodeDeserializer(INodeDeserializer innerDeserializer) : INodeDeserializer
    {
        // Thread-static variable to track the depth
        [ThreadStatic]
        private static int _depth;

        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            _depth++;
            try
            {
                return innerDeserializer.Deserialize(reader, expectedType, nestedObjectDeserializer, out value, rootDeserializer);
            }
            finally
            {
                _depth--;
            }
        }

        // Property to access the current depth
        public static int CurrentDepth => _depth;
    }

    public class XRAssetDeserializer : INodeDeserializer
    {
        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            // Check if the expected type is the one we want to customize
            if (expectedType != typeof(XRAsset))
            {
                value = null;
                return false;
            }

            if (DepthTrackingNodeDeserializer.CurrentDepth == 1)
            {
                // At root, use default deserialization
                value = nestedObjectDeserializer(reader, expectedType);
            }
            else
            {
                // Not at root, apply custom deserialization
                value = GetAsset(reader);
            }
            return true;
        }

        private static XRAsset? GetAsset(IParser parser)
        {
            parser.Consume<MappingStart>();
            parser.Consume<Scalar>();
            var id = parser.Consume<Scalar>();
            parser.Consume<MappingEnd>();

            return Guid.TryParse(id.Value, out Guid guid) 
                ? Engine.Assets.GetAssetByID(guid) 
                : null;
        }
    }

    public class DepthTrackingEventEmitter(IEventEmitter nextEmitter) : ChainedEventEmitter(nextEmitter)
    {
        // Thread-static variable to track the depth
        [ThreadStatic]
        private static int _depth;

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
        {
            _depth++;
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(MappingEndEventInfo eventInfo, IEmitter emitter)
        {
            base.Emit(eventInfo, emitter);
            _depth--;
        }

        public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
        {
            _depth++;
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(SequenceEndEventInfo eventInfo, IEmitter emitter)
        {
            base.Emit(eventInfo, emitter);
            _depth--;
        }

        // Property to access the current depth
        public static int CurrentDepth => _depth;
    }

    public class XRAssetYamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) =>
            type.IsSubclassOf(typeof(XRAsset));

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
            => throw new NotImplementedException("Deserialization is handled by the custom deserializer.");

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            switch (DepthTrackingEventEmitter.CurrentDepth)
            {
                case 0:
                    // At root, use default serialization
                    serializer(value);
                    break;
                default:
                    // Nested, apply custom serialization
                    WriteAsset(emitter, value as XRAsset);
                    break;
            }
        }

        private static void WriteAsset(IEmitter emitter, XRAsset? asset)
        {
            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));
            {
                emitter.Emit(new Scalar("ID"));
                emitter.Emit(new Scalar(asset?.ID.ToString() ?? "null"));
            }
            emitter.Emit(new MappingEnd());
        }
    }
}
