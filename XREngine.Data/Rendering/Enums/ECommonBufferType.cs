﻿namespace XREngine.Data.Rendering
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
        Positions,
        /// <summary>
        /// 3D normals to calculate lighting for each vertex.
        /// </summary>
        Normals,
        /// <summary>
        /// 3D tangents to calculate lighting for each vertex.
        /// </summary>
        Tangents,
        /// <summary>
        /// Color values for each vertex.
        /// </summary>
        Colors,
        /// <summary>
        /// Texture coordinates to align textures for each vertex.
        /// </summary>
        TextureCoordinates,

        /// <summary>
        /// The offset into the blendshape delta buffers for each vertex.
        /// </summary>
        BlendshapeOffsetsPerFacepoint,
        /// <summary>
        /// The number of blendshapes affecting each vertex.
        /// </summary>
        BlendshapeCountsPerFacepoint,

        /// <summary>
        /// The offset into the bone matrix array for each vertex.
        /// </summary>
        BoneMatrixOffsetsPerFacepoint,
        /// <summary>
        /// The number of bones affecting the postion of each vertex.
        /// </summary>
        BoneMatrixCountsPerFacepoint,

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

        BoneMatrices,

    }
}