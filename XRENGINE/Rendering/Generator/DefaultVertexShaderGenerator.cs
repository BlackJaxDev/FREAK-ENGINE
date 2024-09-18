using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering.Shaders.Generator
{
    /// <summary>
    /// Generates a typical vertex shader for use with most models.
    /// </summary>
    public class DefaultVertexShaderGenerator(XRMesh mesh) : ShaderGeneratorBase(mesh)
    {
        //Buffers coming into the vertex shader for each vertex
        public const string VertPosName = "Position";
        public const string VertNormName = "Normal";
        public const string VertTanName = "Tangent";
        public const string VertColorName = "Color{0}";
        public const string VertUVName = "UV{0}";

        //Buffers leaving the vertex shader for each vertex
        public const string FragPosName = "FragPos";
        public const string FragNormName = "FragNorm";
        public const string FragTanName = "FragTan";
        public const string FragBinormName = "FragBinorm"; //Binormal is created in vertex shader if tangents exist
        public const string FragColorName = "FragColor{0}";
        public const string FragUVName = "FragUV{0}";

        //SSBO names
        public const string BoneDataBuffer = "BoneData";
        public const string BoneWeightData = "BoneWeightData";
        public const string BlendshapeData = "BlendshapeData";
        public const string BlendshapeDeltas = "BlendshapeDeltaData";

        /// <summary>
        /// Creates the vertex shader to render a typical model.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="allowMeshMorphing"></param>
        /// <param name="useMorphMultiRig"></param>
        /// <param name="allowColorMorphing"></param>
        /// <returns></returns>
        public override string Generate()
        {
            //Write #definitions
            WriteVersion();
            Line();

            //Write header in fields (from buffers)
            WriteBuffers();
            Line();

            //Write single uniforms
            WriteUniforms();
            Line();

            //Write header uniforms
            WriteSSBOs();
            Line();

            //Write header out fields (to fragment shader)
            WriteOutData();
            Line();

            //For some reason, this is necessary
            WritePipelineData();

            StartMain();

            //Transform position, normals and tangents
            if (Mesh.UtilizedBones.Length > 1)
                WriteSkinnedMeshInputs();
            else
                WriteStaticMeshInputs();

            if (Mesh.ColorBuffers is not null)
                for (int i = 0; i < Mesh.ColorBuffers.Length; ++i)
                    Line($"{string.Format(FragColorName, i)} = {ECommonBufferType.Colors}{i};");

            if (Mesh.TexCoordBuffers is not null)
                for (int i = 0; i < Mesh.TexCoordBuffers.Length; ++i)
                    Line($"{string.Format(FragUVName, i)} = {ECommonBufferType.TextureCoordinates}{i};");

            string source = EndMain();
            Debug.Out(source);
            return source;
        }

        private void WritePipelineData()
        {
            if (!Engine.Rendering.Settings.AllowShaderPipelines)
                return;
            
            Line("out gl_PerVertex");
            OpenBracket();
            Line("Vector4 gl_Position;");
            Line("float gl_PointSize;");
            Line("float gl_ClipDistance[];");
            CloseBracket(null, true);
            Line();
        }

        private void WriteBuffers()
        {
            uint blendshapeCount = Mesh.BlendshapeCount;
            bool weighted = Mesh.UtilizedBones.Length > 1;
            EShaderVarType intVarType = Engine.Rendering.Settings.UseIntegerUniformsInShaders ? EShaderVarType._int : EShaderVarType._float;
            uint location = 0u;

            WriteInVar(location++, EShaderVarType._vec3, ECommonBufferType.Positions.ToString());

            if (Mesh.NormalsBuffer is not null)
                WriteInVar(location++, EShaderVarType._vec3, ECommonBufferType.Normals.ToString());

            if (Mesh.TangentsBuffer is not null)
                WriteInVar(location++, EShaderVarType._vec3, ECommonBufferType.Tangents.ToString());

            if (Mesh.ColorBuffers is not null)
                for (uint i = 0; i < Mesh.ColorBuffers.Length; ++i)
                    WriteInVar(location++, EShaderVarType._vec4, ECommonBufferType.Colors + i.ToString());

            if (Mesh.TexCoordBuffers is not null)
                for (uint i = 0; i < Mesh.TexCoordBuffers.Length; ++i)
                    WriteInVar(location++, EShaderVarType._vec2, ECommonBufferType.TextureCoordinates + i.ToString());

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
        }

        private void WriteUniforms()
        {
            WriteUniform(EShaderVarType._mat4, EEngineUniform.ModelMatrix.ToString());

            //TODO: stereo support
            WriteUniform(EShaderVarType._mat4, EEngineUniform.ViewMatrix.ToString());
            WriteUniform(EShaderVarType._mat4, EEngineUniform.ProjMatrix.ToString());
            //WriteUniform(EShaderVarType._mat4, EEngineUniform.LeftEyeViewMatrix.ToString());
            //WriteUniform(EShaderVarType._mat4, EEngineUniform.LeftEyeProjMatrix.ToString());
            //WriteUniform(EShaderVarType._mat4, EEngineUniform.RightEyeViewMatrix.ToString());
            //WriteUniform(EShaderVarType._mat4, EEngineUniform.RightEyeProjMatrix.ToString());

        }

        /// <summary>
        /// Shader buffer objects
        /// </summary>
        private void WriteSSBOs()
        {
            int bindingIndex = 0;

            if (Mesh.UtilizedBones.Length > 1)
            {
                //Matrices
                using (StartBufferBlock(BoneDataBuffer, bindingIndex++))
                    WriteUniform(EShaderVarType._mat4, $"{ECommonBufferType.BoneMatrices}[]");

                //Bone weights and indices into the matrix buffer
                using (StartBufferBlock(BoneWeightData, bindingIndex++))
                {
                    WriteUniform(EShaderVarType._int, ECommonBufferType.BoneMatrixIndices.ToString());
                    WriteUniform(EShaderVarType._float, ECommonBufferType.BoneMatrixWeights.ToString());
                }
            }

            if (Mesh.BlendshapeCount > 0)
            {
                using (StartBufferBlock(BlendshapeDeltas, bindingIndex++))
                {
                    WriteUniform(EShaderVarType._vec3, ECommonBufferType.BlendshapePositionDeltas.ToString());
                    WriteUniform(EShaderVarType._vec3, ECommonBufferType.BlendshapeNormalDeltas.ToString());
                    WriteUniform(EShaderVarType._vec3, ECommonBufferType.BlendshapeTangentDeltas.ToString());
                }

                using (StartBufferBlock(BlendshapeData, bindingIndex++))
                {
                    WriteUniform(EShaderVarType._int, ECommonBufferType.BlendshapeIndices.ToString());
                    WriteUniform(EShaderVarType._float, ECommonBufferType.BlendshapeWeights.ToString());
                }
            }
        }

        /// <summary>
        /// This information is sent to the fragment shader.
        /// </summary>
        private void WriteOutData()
        {
            int location = 0;
            WriteOutVar(location++, EShaderVarType._vec3, FragPosName);

            if (mesh.NormalsBuffer is not null)
                WriteOutVar(location++, EShaderVarType._vec3, FragNormName);

            if (mesh.TangentsBuffer is not null)
            {
                WriteOutVar(location++, EShaderVarType._vec3, FragTanName);
                WriteOutVar(location++, EShaderVarType._vec3, FragBinormName);
            }

            if (mesh.ColorBuffers is not null)
                for (int i = 0; i < mesh.ColorBuffers.Length; ++i)
                    WriteOutVar(location++, EShaderVarType._vec4, string.Format(FragColorName, i));

            if (mesh.TexCoordBuffers is not null)
                for (int i = 0; i < mesh.TexCoordBuffers.Length; ++i)
                    WriteOutVar(location++, EShaderVarType._vec2, string.Format(FragUVName, i));
        }

        /// <summary>
        /// Calculates positions, and optionally normals, tangents, and binormals for a rigged mesh.
        /// </summary>
        private void WriteSkinnedMeshInputs()
        {
            bool hasNormals = Mesh.NormalsBuffer is not null;
            bool hasTangents = Mesh.TangentsBuffer is not null;
            bool hasNBT = hasNormals || hasTangents;

            Line("vec4 finalPosition = vec4(0.0f);");
            Line("vec4 basePosition = vec4(Position, 1.0f);");

            if (hasNormals)
            {
                Line("vec3 finalNormal = vec3(0.0f);");
                Line("vec4 baseNormal = vec4(Normal, 0.0);");
            }
            if (hasTangents)
            {
                Line("vec3 finalTangent = vec3(0.0f);");
                Line("vec4 baseTangent = vec4(Tangent, 0.0);");
            }

            Line();

            if (Mesh.BlendshapeCount > 0)
            {
                //Calculate blendshapes on unskinned mesh
            }

            //Loop over the bone count supplied to this vertex
            Line($"for (int i = 0; i < {ECommonBufferType.BoneMatrixCountsPerFacepoint}; i++)");
            OpenBracket();
            {
                Line($"int index = {ECommonBufferType.BoneMatrixOffsetsPerFacepoint} + i;");
                Line($"int boneIndex = {ECommonBufferType.BoneMatrixIndices}[index]");
                Line($"float weight = {ECommonBufferType.BoneMatrixWeights}[index];");

                Line("if (weight > 0.0)");
                OpenBracket();
                Line($"mat4 boneMatrix = {ECommonBufferType.BoneMatrices}[boneIndex];");
                Line("finalPosition += (boneMatrix * basePosition) * weight;");
                Line("finalNormal += (boneMatrix * baseNormal).xyz * weight;");
                Line("finalTangent += (boneMatrix * baseTangent).xyz * weight;");
                CloseBracket();
            }
            CloseBracket();

            Line();
            if (hasNormals)
            {
                Line($"{FragNormName} = normalize(normalMatrix * finalNormal);");
                if (hasTangents)
                {
                    Line($"{FragTanName} = normalize(normalMatrix * finalTangent);");
                    Line("vec3 finalBinormal = cross(finalNormal, finalTangent);");
                    Line($"{FragBinormName} = normalize(normalMatrix * finalBinormal);");
                }
            }

            ResolvePosition("finalPosition");
        }
        /// <summary>
        /// Calculates positions, and optionally normals, tangents, and binormals for a static mesh.
        /// </summary>
        private void WriteStaticMeshInputs()
        {
            Line("vec4 position = vec4(Position, 1.0f);");
            if (mesh.NormalsBuffer is not null)
                Line("vec3 normal = Normal;");
            if (mesh.TangentsBuffer is not null)
                Line("vec3 tangent = Tangent;");
            Line();

            if (Mesh.BlendshapeCount > 0)
            {
                //Line("float totalWeight = 0.0f;");
                //OpenLoop(mesh.BlendshapeCount);
                //Line($"totalWeight += {Uniform.MorphWeightsName}[i];");
                //CloseBracket();

                //Line("float baseWeight = 1.0f - totalWeight;");
                //Line("float invTotal = 1.0f / (totalWeight + baseWeight);");
                //Line();

                //Line("position *= baseWeight;");
                //if (mesh.HasNormals)
                //    Line("normal *= baseWeight;");
                ////if (_info.HasBinormals)
                ////    Line("binormal *= baseWeight;");
                //if (mesh.HasTangents)
                //    Line("tangent *= baseWeight;");
                //Line();

                //for (int i = 0; i < mesh.BlendshapeCount; ++i)
                //{
                //    Line($"position += Vector4(Position{i + 1}, 1.0f) * MorphWeights[{i}];");
                //    if (mesh.HasNormals)
                //        Line($"normal += Normal{i + 1} * MorphWeights[{i}];");
                //    //if (_info.HasBinormals)
                //    //    Line($"binormal += Binormal{i + 1} * MorphWeights[{i}];");
                //    if (mesh.HasTangents)
                //        Line($"tangent += Tangent{i + 1} * MorphWeights[{i}];");
                //}
                //Line();
                //Line("position *= invTotal;");
                //if (mesh.HasNormals)
                //    Line("normal *= invTotal;");
                ////if (_info.HasBinormals)
                ////    Line("binormal *= invTotal;");
                //if (mesh.HasTangents)
                //    Line("tangent *= invTotal;");
                //Line();
            }

            ResolvePosition("position");

            if (mesh.NormalsBuffer is not null)
            {
                Line($"{FragNormName} = normalize(normalMatrix * normal);");
                if (mesh.TangentsBuffer is not null)
                {
                    Line($"{FragTanName} = normalize(normalMatrix * tangent);");
                    Line("vec3 binormal = cross(normal, tangent);");
                    Line($"{FragBinormName} = normalize(normalMatrix * binormal);");
                }
            }
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

            Line($"{FragPosName} = (mvpMatrix * {posName}).xyz;");
            Line($"gl_Position = mvpMatrix * {posName};");
        }
    }
}
