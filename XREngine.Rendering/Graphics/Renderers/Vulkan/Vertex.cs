using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Format = Silk.NET.Vulkan.Format;

namespace XREngine.Rendering.Graphics.Renderers.Vulkan
{
    struct Vertex
    {
        public Vector3D<float> pos;
        public Vector3D<float> color;
        public Vector2D<float> textCoord;

        public static VertexInputBindingDescription GetBindingDescription() => new()
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<Vertex>(),
            InputRate = VertexInputRate.Vertex,
        };

        public static VertexInputAttributeDescription[] GetAttributeDescriptions()
        {
            return new[]
            {
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 0,
                    Format = Format.R32G32B32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(pos)),
                },
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 1,
                    Format = Format.R32G32B32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(color)),
                },
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 2,
                    Format = Format.R32G32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(textCoord)),
                }
            };
        }
    }
}
