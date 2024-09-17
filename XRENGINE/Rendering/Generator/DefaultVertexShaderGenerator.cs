using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering.Shaders.Generator
{
    /// <summary>
    /// Generates a typical vertex shader for use with most models.
    /// </summary>
    public class DefaultVertexShaderGenerator : ShaderGeneratorBase
    {
        public const string FragPosName = "FragPos";
        public const string FragNormName = "FragNorm";
        public const string FragTanName = "FragTan";
        public const string FragColorName = "FragColor{0}";
        public const string FragUVName = "FragUV{0}";

        private XRMesh Mesh;

        /// <summary>
        /// Creates the vertex shader to render a typical model.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="allowMeshMorphing"></param>
        /// <param name="useMorphMultiRig"></param>
        /// <param name="allowColorMorphing"></param>
        /// <returns></returns>
        public override string Generate(XRMesh mesh)
        {
            Mesh = mesh;

            //Write #definitions
            WriteVersion();
            Line();

            //Write header in fields (from buffers)
            WriteBuffers();
            Line();

            //Write header uniforms
            WriteMatrixUniforms();
            Line();

            //Write header out fields (to fragment shader)
            WriteOutData();
            Line();

            //For some reason, this is necessary
            if (Engine.Rendering.Settings.AllowShaderPipelines)
            {
                Line("out gl_PerVertex");
                OpenBracket();
                Line("Vector4 gl_Position;");
                Line("float gl_PointSize;");
                Line("float gl_ClipDistance[];");
                CloseBracket(null, true);
                Line();
            }

            StartMain();

            if (mesh.UtilizedBones.Length > 1)
                WriteSkinnedMeshInputs();
            else
                WriteStaticMeshInputs();

            if (mesh.ColorBuffers is not null)
                for (int i = 0; i < mesh.ColorBuffers.Length; ++i)
                    Line("{0} = {2}{1};", string.Format(FragColorName, i), i, ECommonBufferType.Colors.ToString());

            if (mesh.TexCoordBuffers is not null)
                for (int i = 0; i < mesh.TexCoordBuffers.Length; ++i)
                    Line("{0} = {2}{1};", string.Format(FragUVName, i), i, ECommonBufferType.TextureCoordinates.ToString());

            string source = EndMain();
            Debug.Out(source);
            return source;
        }
        private void WriteBuffers()
        {
            uint blendshapeCount = Mesh.BlendshapeCount;
            bool weighted = Mesh.UtilizedBones.Length > 1;
            EShaderVarType intVarType = Engine.Rendering.Settings.UseIntegerUniformsInShaders ? EShaderVarType._int : EShaderVarType._float;
            uint location = 0u;

            WriteInVar(location++, EShaderVarType._vector3, ECommonBufferType.Positions.ToString());

            if (Mesh.NormalsBuffer is not null)
                WriteInVar(location++, EShaderVarType._vector3, ECommonBufferType.Normals.ToString());

            if (Mesh.TangentsBuffer is not null)
                WriteInVar(location++, EShaderVarType._vector3, ECommonBufferType.Tangents.ToString());

            if (Mesh.ColorBuffers is not null)
                for (uint i = 0; i < Mesh.ColorBuffers.Length; ++i)
                    WriteInVar(location++, EShaderVarType._vector4, ECommonBufferType.Colors + i.ToString());

            if (Mesh.TexCoordBuffers is not null)
                for (uint i = 0; i < Mesh.TexCoordBuffers.Length; ++i)
                    WriteInVar(location++, EShaderVarType._vector2, ECommonBufferType.TextureCoordinates + i.ToString());

            if (weighted)
            {
                WriteInVar(location++, intVarType, ECommonBufferType.BoneMatrixOffsetsPerFacepoint.ToString());
                WriteInVar(location++, intVarType, ECommonBufferType.BoneMatrixCountsPerFacepoint.ToString());
            }

            if (blendshapeCount > 0)
            {
                WriteInVar(location++, intVarType, ECommonBufferType.BlendshapeOffsetsPerFacepoint.ToString());
                WriteInVar(location++, intVarType, ECommonBufferType.BlendshapeCountsPerFacepoint.ToString());
            }

            //type = ECommonBufferType.BoneMatrixWeights;
            //if (weighted)
            //    for (uint i = 0; i < blendshapeCount; ++i)
            //        WriteInVar(location + i, EShaderVarType._Vector4, VertexAttribInfo.GetAttribName(ECommonBufferType.BoneMatrixWeights, i));
        }
        private void WriteMatrixUniforms()
        {
            //WriteUniform(EShaderVarType._mat4, EEngineUniform.ModelMatrix.ToString());
            //WriteUniform(EShaderVarType._mat3, EEngineUniform.NormalMatrix.ToString());
            //WriteUniform(EShaderVarType._mat4, EEngineUniform.WorldToCameraSpaceMatrix.ToString());
            //if (mesh.BillboardingFlags != ECameraTransformFlags.None)
            //    WriteUniform(EShaderVarType._mat4, EEngineUniform.CameraToWorldSpaceMatrix.ToString());
            //WriteUniform(EShaderVarType._mat4, EEngineUniform.ProjMatrix.ToString());
            //if (mesh.IsWeighted)
            //{
            //    //if (RenderSettings.SkinOnGPU)
            //    //{
            //    StartUniformBlock("Bones");
            //    WriteUniform(EShaderVarType._mat4, Uniform.BoneTransformsName + "[" + (mesh.BoneCount + 1) + "]");
            //    EndUniformBlock("BoneDef");
            //    //}
            //    if (UseMorphs)
            //        WriteUniform(EShaderVarType._mat4, Uniform.MorphWeightsName + "[" + mesh.BlendshapeCount + "]");
            //}
        }

        /// <summary>
        /// This information is sent to the fragment shader.
        /// </summary>
        private void WriteOutData()
        {
            //WriteOutVar(0, EShaderVarType._vector3, FragPosName);

            //if (mesh.HasNormals)
            //    WriteOutVar(1, EShaderVarType._vector3, FragNormName);

            ////if (_info.HasBinormals)
            ////    WriteOutVar(2, EShaderVarType._Vector3, FragBinormName);

            //if (mesh.HasTangents)
            //    WriteOutVar(3, EShaderVarType._vector3, FragTanName);

            //for (int i = 0; i < mesh.ColorCount; ++i)
            //    WriteOutVar(4 + i, EShaderVarType._vector4, string.Format(FragColorName, i));

            //for (int i = 0; i < mesh.TexcoordCount; ++i)
            //    WriteOutVar(6 + i, EShaderVarType._vector2, string.Format(FragUVName, i));
        }

        /// <summary>
        /// Calculates positions, and optionally normals, tangents, and binormals for a rigged mesh.
        /// </summary>
        private void WriteSkinnedMeshInputs()
        {
            //bool hasNBT = mesh.HasNormals || mesh.HasTangents;

            //Line("Vector4 finalPosition = Vector4(0.0f);");
            //Line("Vector4 basePosition = Vector4(Position0, 1.0f);");

            //if (mesh.HasNormals)
            //{
            //    Line("Vector3 finalNormal = Vector3(0.0f);");
            //    Line("Vector3 baseNormal = Normal0;");
            //}
            //if (mesh.HasTangents)
            //{
            //    Line("Vector3 finalTangent = Vector3(0.0f);");
            //    Line("Vector3 baseTangent = Tangent0;");
            //}

            //Line();
            //if (!MultiRig)
            //{
            //    Line("int index;");
            //    Line("float weight;");

            //    OpenLoop(4);
            //    {
            //        if (RenderState.UseIntegerWeightingIds)
            //            Line("index = {0}0[i];", ECommonBufferType.BoneMatrixOffsetsPerFacepoint.ToString());
            //        else
            //            Line("index = int({0}0[i]);", ECommonBufferType.BoneMatrixOffsetsPerFacepoint.ToString());

            //        Line("weight = {0}0[i];", ECommonBufferType.BoneMatrixWeights.ToString());

            //        Line($"finalPosition += (BoneDef.{Uniform.BoneTransformsName}[index] * basePosition) * weight;");
            //        if (hasNBT)
            //        {
            //            Line($"mat3 nrmMtx = mat3(transpose(inverse(BoneDef.{Uniform.BoneTransformsName}[index])));");
            //            if (mesh.HasNormals)
            //                Line($"finalNormal += (nrmMtx * baseNormal) * weight;");
            //            //if (_info.HasBinormals)
            //            //    Line($"finalBinormal += (nrmMtx * baseBinormal) * weight;");
            //            if (mesh.HasTangents)
            //                Line($"finalTangent += (nrmMtx * baseTangent) * weight;");
            //        }
            //    }
            //    CloseBracket();

            //    Line();
            //    if (mesh.HasNormals)
            //        Line($"{FragNormName} = normalize(NormalMatrix * finalNormal);");
            //    //if (_info.HasBinormals)
            //    //    Line($"{FragBinormName} = normalize(NormalMatrix * finalBinormal);");
            //    if (mesh.HasTangents)
            //        Line($"{FragTanName} = normalize(NormalMatrix * finalTangent);");
            //}
            //else
            //{
            //    Line("float totalWeight = 0.0f;");
            //    Line($"for (int i = 0; i < {mesh.BlendshapeCount}; ++i)");
            //    Line("totalWeight += MorphWeights[i];");
            //    Line();
            //    Line("float baseWeight = 1.0f - totalWeight;");
            //    Line("float total = totalWeight + baseWeight;");
            //    Line();
            //    Line("basePosition *= baseWeight;");
            //    if (mesh.HasNormals)
            //        Line("baseNormal *= baseWeight;");
            //    //if (_info.HasBinormals)
            //    //    Line("baseBinormal *= baseWeight;");
            //    if (mesh.HasTangents)
            //        Line("baseTangent *= baseWeight;");
            //    Line();

            //    OpenLoop(4);
            //    for (int i = 0; i < mesh.BlendshapeCount; ++i)
            //    {
            //        Line("finalPosition += BoneDef.{0}[{1}{3}[i]] * Vector4(Position{5}, 1.0f) * {2}{3}[i] * {4}[i];", Uniform.BoneTransformsName, ECommonBufferType.BoneMatrixOffsetsPerFacepoint, ECommonBufferType.BoneMatrixWeights, i, Uniform.MorphWeightsName, i + 1);
            //        if (mesh.HasNormals)
            //            Line("finalNormal += (mat3(BoneDef.{0}[{1}{3}[i]]) * Normal{5}) * {2}{3}[i] * {4}[i];", Uniform.BoneTransformsName, ECommonBufferType.BoneMatrixOffsetsPerFacepoint, ECommonBufferType.BoneMatrixWeights, i, Uniform.MorphWeightsName, i + 1);
            //        //if (_info.HasBinormals)
            //        //    Line("finalBinorm += (mat3(BoneDef.{0}[{1}{3}[i]]) * Binormal{5}) * {2}{3}[i] * {4}[i];", Uniform.BoneTransformsName, EBufferType.MatrixIds, EBufferType.MatrixWeights, i, Uniform.MorphWeightsName, i + 1);
            //        if (mesh.HasTangents)
            //            Line("finalTangent += (mat3(BoneDef.{0}[{1}{3}[i]]) * Tangent{5}) * {2}{3}[i] * {4}[i];", Uniform.BoneTransformsName, ECommonBufferType.BoneMatrixOffsetsPerFacepoint, ECommonBufferType.BoneMatrixWeights, i, Uniform.MorphWeightsName, i + 1);
            //        if (i + 1 != mesh.BlendshapeCount)
            //            Line();
            //    }
            //    CloseBracket();

            //    if (mesh.HasNormals)
            //        Line($"{FragNormName} = normalize(NormalMatrix * (finalNormal / total));");
            //    //if (_info.HasBinormals)
            //    //    Line($"{FragBinormName} = normalize(NormalMatrix * (finalBinormal / total));");
            //    if (mesh.HasTangents)
            //        Line($"{FragTanName} = normalize(NormalMatrix * (finalTangent / total));");
            //    Line("finalPosition /= Vector4(Vector3(total), 1.0f);");
            //}
            //ResolvePosition("finalPosition");
        }
        /// <summary>
        /// Calculates positions, and optionally normals, tangents, and binormals for a static mesh.
        /// </summary>
        private void WriteStaticMeshInputs()
        {
            //Line("Vector4 position = Vector4(Position0, 1.0f);");
            //if (mesh.HasNormals)
            //    Line("Vector3 normal = Normal0;");
            ////if (_info.HasBinormals)
            ////    Line("Vector3 binormal = Binormal0;");
            //if (mesh.HasTangents)
            //    Line("Vector3 tangent = Tangent0;");
            //Line();

            //if (UseMorphs)
            //{
            //    Line("float totalWeight = 0.0f;");
            //    OpenLoop(mesh.BlendshapeCount);
            //    Line($"totalWeight += {Uniform.MorphWeightsName}[i];");
            //    CloseBracket();

            //    Line("float baseWeight = 1.0f - totalWeight;");
            //    Line("float invTotal = 1.0f / (totalWeight + baseWeight);");
            //    Line();

            //    Line("position *= baseWeight;");
            //    if (mesh.HasNormals)
            //        Line("normal *= baseWeight;");
            //    //if (_info.HasBinormals)
            //    //    Line("binormal *= baseWeight;");
            //    if (mesh.HasTangents)
            //        Line("tangent *= baseWeight;");
            //    Line();

            //    for (int i = 0; i < mesh.BlendshapeCount; ++i)
            //    {
            //        Line($"position += Vector4(Position{i + 1}, 1.0f) * MorphWeights[{i}];");
            //        if (mesh.HasNormals)
            //            Line($"normal += Normal{i + 1} * MorphWeights[{i}];");
            //        //if (_info.HasBinormals)
            //        //    Line($"binormal += Binormal{i + 1} * MorphWeights[{i}];");
            //        if (mesh.HasTangents)
            //            Line($"tangent += Tangent{i + 1} * MorphWeights[{i}];");
            //    }
            //    Line();
            //    Line("position *= invTotal;");
            //    if (mesh.HasNormals)
            //        Line("normal *= invTotal;");
            //    //if (_info.HasBinormals)
            //    //    Line("binormal *= invTotal;");
            //    if (mesh.HasTangents)
            //        Line("tangent *= invTotal;");
            //    Line();
            //}

            //ResolvePosition("position");
            //if (mesh.HasNormals)
            //    Line($"{FragNormName} = normalize(NormalMatrix * normal);");
            ////if (_info.HasBinormals)
            ////    Line($"{FragBinormName} = normalize(NormalMatrix * binormal);");
            //if (mesh.HasTangents)
            //    Line($"{FragTanName} = normalize(NormalMatrix * tangent);");
        }
        private void ResolvePosition(string posName)
        {
            //Line("mat4 ViewMatrix = WorldToCameraSpaceMatrix;");
            //if (mesh.BillboardingFlags == ECameraTransformFlags.None)
            //{
            //    Line($"{posName} = ModelMatrix * Vector4({posName}.xyz, 1.0f);");
            //    Line($"{FragPosName} = {posName}.xyz;");
            //    Line($"gl_Position = ProjMatrix * ViewMatrix * {posName};");
            //    return;
            //}
            //Line("mat4 BillboardMatrix = CameraToWorldSpaceMatrix;");
            //if (mesh.BillboardingFlags.HasFlag(ECameraTransformFlags.RotateX))
            //{
            //    //Do not align X column to be stationary from camera's viewpoint
            //    Line("ViewMatrix[0][0] = 1.0f;");
            //    Line("ViewMatrix[0][1] = 0.0f;");
            //    Line("ViewMatrix[0][2] = 0.0f;");

            //    //Do not fix Y column to rotate with camera
            //    Line("BillboardMatrix[1][0] = 0.0f;");
            //    Line("BillboardMatrix[1][1] = 1.0f;");
            //    Line("BillboardMatrix[1][2] = 0.0f;");

            //    //Do not fix Z column to rotate with camera
            //    Line("BillboardMatrix[2][0] = 0.0f;");
            //    Line("BillboardMatrix[2][1] = 0.0f;");
            //    Line("BillboardMatrix[2][2] = 1.0f;");
            //}
            //if (mesh.BillboardingFlags.HasFlag(ECameraTransformFlags.RotateY))
            //{
            //    //Do not fix X column to rotate with camera
            //    Line("BillboardMatrix[0][0] = 1.0f;");
            //    Line("BillboardMatrix[0][1] = 0.0f;");
            //    Line("BillboardMatrix[0][2] = 0.0f;");

            //    //Do not align Y column to be stationary from camera's viewpoint
            //    Line("ViewMatrix[1][0] = 0.0f;");
            //    Line("ViewMatrix[1][1] = 1.0f;");
            //    Line("ViewMatrix[1][2] = 0.0f;");

            //    //Do not fix Z column to rotate with camera
            //    Line("BillboardMatrix[2][0] = 0.0f;");
            //    Line("BillboardMatrix[2][1] = 0.0f;");
            //    Line("BillboardMatrix[2][2] = 1.0f;");
            //}
            //if (mesh.BillboardingFlags.HasFlag(ECameraTransformFlags.RotateZ))
            //{
            //    //Do not fix X column to rotate with camera
            //    Line("BillboardMatrix[0][0] = 1.0f;");
            //    Line("BillboardMatrix[0][1] = 0.0f;");
            //    Line("BillboardMatrix[0][2] = 0.0f;");

            //    //Do not fix Y column to rotate with camera
            //    Line("BillboardMatrix[1][0] = 0.0f;");
            //    Line("BillboardMatrix[1][1] = 1.0f;");
            //    Line("BillboardMatrix[1][2] = 0.0f;");

            //    //Do not align Z column to be stationary from camera's viewpoint
            //    Line("ViewMatrix[2][0] = 0.0f;");
            //    Line("ViewMatrix[2][1] = 0.0f;");
            //    Line("ViewMatrix[2][2] = 1.0f;");
            //}
            //if (mesh.BillboardingFlags.HasFlag(ECameraTransformFlags.ConstrainTranslationX))
            //{
            //    //Clear X translation
            //    Line("ViewMatrix[3][0] = 0.0f;");
            //    Line("BillboardMatrix[3][0] = 0.0f;");
            //}
            //if (mesh.BillboardingFlags.HasFlag(ECameraTransformFlags.ConstrainTranslationY))
            //{
            //    //Clear Y translation
            //    Line("ViewMatrix[3][1] = 0.0f;");
            //    Line("BillboardMatrix[3][1] = 0.0f;");
            //}
            //if (mesh.BillboardingFlags.HasFlag(ECameraTransformFlags.ConstrainTranslationZ))
            //{
            //    //Clear Z translation
            //    Line("ViewMatrix[3][2] = 0.0f;");
            //    Line("BillboardMatrix[3][2] = 0.0f;");
            //}

            //Line($"{posName} = ModelMatrix * Vector4({posName}.xyz, 1.0f);");
            //Line($"{FragPosName} = (BillboardMatrix * {posName}).xyz;");
            //Line($"gl_Position = ProjMatrix * ViewMatrix * {posName};");
        }
    }
}
