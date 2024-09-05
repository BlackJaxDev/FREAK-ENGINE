namespace XREngine.Rendering
{
    ///// <summary>
    ///// This class describes what data buffers this mesh utilizes.
    ///// </summary>
    //public sealed class XRMeshDescriptor : XRBase
    //{
    //    public event Action? Changed;

    //    private int _blendshapeCount = 0;
    //    private int _texcoordCount = 0;
    //    private int _colorCount = 0;
    //    private int _boneCount = 0;
    //    private bool _hasNormals = false;
    //    private bool _hasTangents = false;

    //    //Note: if there's only one bone, we can just multiply the model matrix by the bone's frame matrix. No need for weighting.
    //    public bool IsWeighted => BoneCount > 1;
    //    public bool IsSingleBound => BoneCount == 1;
    //    public bool HasSkinning => BoneCount > 0;
    //    public bool HasTexCoords => TexcoordCount > 0;
    //    public bool HasColors => ColorCount > 0;

    //    public int BlendshapeCount
    //    {
    //        get => _blendshapeCount;
    //        set => _blendshapeCount = value;
    //    }
    //    public int TexcoordCount
    //    {
    //        get => _texcoordCount;
    //        set => _texcoordCount = value;
    //    }
    //    public int ColorCount
    //    {
    //        get => _colorCount;
    //        set => _colorCount = value;
    //    }
    //    public int BoneCount
    //    {
    //        get => _boneCount;
    //        set => _boneCount = value;
    //    }
    //    public bool HasNormals
    //    {
    //        get => _hasNormals;
    //        set => _hasNormals = value;
    //    }
    //    public bool HasTangents
    //    {
    //        get => _hasTangents;
    //        set => _hasTangents = value;
    //    }

    //    /// <summary>
    //    /// Billboarding flags for the automatically generated vertex shader.
    //    /// </summary>
    //    public ECameraTransformFlags BillboardingFlags { get; set; } = ECameraTransformFlags.None;

    //    /// <summary>
    //    /// Returns a descriptor for a mesh that has positions and colors.
    //    /// </summary>
    //    /// <param name="colorCount"></param>
    //    /// <returns></returns>
    //    public static XRMeshDescriptor PositionsColors(int colorCount = 1) => new() { ColorCount = colorCount };
    //    /// <summary>
    //    /// Returns a descriptor for a mesh that has positions and texcoords.
    //    /// </summary>
    //    /// <param name="texCoordCount"></param>
    //    /// <returns></returns>
    //    public static XRMeshDescriptor PositionsUVs(int texCoordCount = 1) => new() { TexcoordCount = texCoordCount };
    //    /// <summary>
    //    /// Returns a descriptor for a mesh that has positions, normals, and texcoords.
    //    /// </summary>
    //    /// <param name="texCoordCount"></param>
    //    /// <returns></returns>
    //    public static XRMeshDescriptor PositionsNormalsUVs(int texCoordCount = 1) => new() { TexcoordCount = texCoordCount, HasNormals = true };
    //    /// <summary>
    //    /// Returns a descriptor for a mesh that has positions and normals.
    //    /// </summary>
    //    /// <returns></returns>
    //    public static XRMeshDescriptor PositionsNormals() => new() { HasNormals = true };
    //    /// <summary>
    //    /// Returns a descriptor for a mesh that has positions.
    //    /// </summary>
    //    /// <returns></returns>
    //    public static XRMeshDescriptor Positions() => new();
    //}
}