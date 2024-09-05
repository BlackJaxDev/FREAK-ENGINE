using XREngine.Data.Core;

namespace XREngine.Data.Rendering
{
    /// <summary>
    /// Defines indices into the mesh's buffers to represent weights, positions, normals, etc for a singular vertex.
    /// </summary>
    public class VertexIndices : XRBase
    {
        /// <summary>
        /// References the mesh's buffers via binding name to index positions, normals etc for this vertex.
        /// </summary>
        public Dictionary<string, uint> BufferBindings { get; } = [];

        /// <summary>
        /// Index into the mesh's weights list.
        /// </summary>
        public int WeightIndex { get; set; }
    }
}