namespace XREngine.Data.Rendering
{
    public enum ECommonBufferType
    {
        /// <summary>
        /// Use this for uncommon buffer types.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// 3D coordinates for the location of each vertex.
        /// </summary>
        Position,
        /// <summary>
        /// 3D normals to calculate lighting for each vertex.
        /// </summary>
        Normal,
        /// <summary>
        /// 3D tangents to calculate lighting for each vertex.
        /// </summary>
        Tangent,
        /// <summary>
        /// Color values for each vertex.
        /// </summary>
        Color,
        /// <summary>
        /// Texture coordinates to align textures for each vertex.
        /// </summary>
        TexCoord,

        /// <summary>
        /// The offset into the blendshape delta buffers for each vertex.
        /// Add indices up until count to retrieve all deltas for a vertex.
        /// </summary>
        BlendshapeOffset,
        /// <summary>
        /// The number of blendshapes affecting each vertex.
        /// </summary>
        BlendshapeCount,

        /// <summary>
        /// The offset into the indices/weights array for each vertex.
        /// Add indices up until count to retrieve all bone indices/weights for a vertex.
        /// </summary>
        BoneMatrixOffset,
        /// <summary>
        /// The number of bones affecting the postion of each vertex.
        /// </summary>
        BoneMatrixCount,

        /// <summary>
        /// The weight of each bone affecting the position of each vertex.
        /// </summary>
        BoneMatrixWeights,
        /// <summary>
        /// The index into the bone matrix buffer for each bone affecting each vertex.
        /// </summary>
        BoneMatrixIndices,

        /// <summary>
        /// The user-set weight of each blendshape affecting each vertex.
        /// </summary>
        BlendshapeWeights,
        /// <summary>
        /// The index into the blendshape delta buffers for each blendshape affecting each vertex.
        /// </summary>
        BlendshapeIndices,

        /// <summary>
        /// The animated world matrices for each bone utilized by the mesh.
        /// The first matrix is identity.
        /// </summary>
        BoneMatrices,
        /// <summary>
        /// The bind pose inverse world matrices for each bone utilized by the mesh.
        /// The first matrix is identity.
        /// </summary>
        BoneInvBindMatrices,

        /// <summary>
        /// Remapped array of position offsets.
        /// No 0 values are stored in this buffer.
        /// </summary>
        BlendshapePositionDeltas,
        /// <summary>
        /// Remapped array of normal offsets.
        /// No 0 values are stored in this buffer.
        /// </summary>
        BlendshapeNormalDeltas,
        /// <summary>
        /// Remapped array of tangent offsets.
        /// No 0 values are stored in this buffer.
        /// </summary>
        BlendshapeTangentDeltas,

        GlyphTransforms,
        GlyphTexCoords,
    }
}
