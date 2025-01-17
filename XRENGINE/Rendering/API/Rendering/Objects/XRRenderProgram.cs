﻿using System.Collections;
using System.Numerics;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;
using XREngine.Rendering.OpenGL;

namespace XREngine.Rendering
{
    public class XRRenderProgram : GenericRenderObject, IEnumerable<XRShader>
    {
        /// <summary>
        /// The shaders that make up the program.
        /// </summary>
        public EventList<XRShader> Shaders { get; } = [];

        public bool LinkReady { get; private set; } = false;

        /// <summary>
        /// Call this once all shaders have been added to the Shaders list to finalize the program.
        /// </summary>
        public void Link()
            => LinkReady = true;

        public event Action<string, Matrix4x4>? UniformSetMatrix4x4Requested = null;
        public event Action<string, Quaternion>? UniformSetQuaternionRequested = null;

        public event Action<string, Matrix4x4[]>? UniformSetMatrix4x4ArrayRequested = null;
        public event Action<string, Quaternion[]>? UniformSetQuaternionArrayRequested = null;

        public event Action<string, bool>? UniformSetBoolRequested = null;
        public event Action<string, BoolVector2>? UniformSetBoolVector2Requested = null;
        public event Action<string, BoolVector3>? UniformSetBoolVector3Requested = null;
        public event Action<string, BoolVector4>? UniformSetBoolVector4Requested = null;

        public event Action<string, bool[]>? UniformSetBoolArrayRequested = null;
        public event Action<string, BoolVector2[]>? UniformSetBoolVector2ArrayRequested = null;
        public event Action<string, BoolVector3[]>? UniformSetBoolVector3ArrayRequested = null;
        public event Action<string, BoolVector4[]>? UniformSetBoolVector4ArrayRequested = null;

        public event Action<string, float>? UniformSetFloatRequested = null;
        public event Action<string, Vector2>? UniformSetVector2Requested = null;
        public event Action<string, Vector3>? UniformSetVector3Requested = null;
        public event Action<string, Vector4>? UniformSetVector4Requested = null;

        public event Action<string, float[]>? UniformSetFloatArrayRequested = null;
        public event Action<string, Vector2[]>? UniformSetVector2ArrayRequested = null;
        public event Action<string, Vector3[]>? UniformSetVector3ArrayRequested = null;
        public event Action<string, Vector4[]>? UniformSetVector4ArrayRequested = null;

        public event Action<string, double>? UniformSetDoubleRequested = null;
        public event Action<string, DVector2>? UniformSetDVector2Requested = null;
        public event Action<string, DVector3>? UniformSetDVector3Requested = null;
        public event Action<string, DVector4>? UniformSetDVector4Requested = null;

        public event Action<string, double[]>? UniformSetDoubleArrayRequested = null;
        public event Action<string, DVector2[]>? UniformSetDVector2ArrayRequested = null;
        public event Action<string, DVector3[]>? UniformSetDVector3ArrayRequested = null;
        public event Action<string, DVector4[]>? UniformSetDVector4ArrayRequested = null;

        public event Action<string, int>? UniformSetIntRequested = null;
        public event Action<string, IVector2>? UniformSetIVector2Requested = null;
        public event Action<string, IVector3>? UniformSetIVector3Requested = null;
        public event Action<string, IVector4>? UniformSetIVector4Requested = null;

        public event Action<string, int[]>? UniformSetIntArrayRequested = null;
        public event Action<string, IVector2[]>? UniformSetIVector2ArrayRequested = null;
        public event Action<string, IVector3[]>? UniformSetIVector3ArrayRequested = null;
        public event Action<string, IVector4[]>? UniformSetIVector4ArrayRequested = null;

        public event Action<string, uint>? UniformSetUIntRequested = null;
        public event Action<string, UVector2>? UniformSetUVector2Requested = null;
        public event Action<string, UVector3>? UniformSetUVector3Requested = null;
        public event Action<string, UVector4>? UniformSetUVector4Requested = null;

        public event Action<string, uint[]>? UniformSetUIntArrayRequested = null;
        public event Action<string, UVector2[]>? UniformSetUVector2ArrayRequested = null;
        public event Action<string, UVector3[]>? UniformSetUVector3ArrayRequested = null;
        public event Action<string, UVector4[]>? UniformSetUVector4ArrayRequested = null;

        public event Action<string, XRTexture, int>? SamplerRequested = null;
        public event Action<int, XRTexture, int>? SamplerRequestedByLocation = null;

        public event Action<uint, XRTexture, int, bool, int, EImageAccess, EImageFormat>? BindImageTextureRequested = null;
        public event Action<uint, uint, uint, IEnumerable<(uint unit, XRTexture texture, int level, int? layer, EImageAccess access, EImageFormat format)>?>? DispatchComputeRequested = null;

        /// <summary>
        /// Mask of the shader types included in the program.
        /// </summary>
        public EProgramStageMask GetShaderTypeMask()
        {
            EProgramStageMask mask = EProgramStageMask.None;
            foreach (var shader in Shaders)
            {
                switch (shader.Type)
                {
                    case EShaderType.Vertex:
                        mask |= EProgramStageMask.VertexShaderBit;
                        break;
                    case EShaderType.TessControl:
                        mask |= EProgramStageMask.TessControlShaderBit;
                        break;
                    case EShaderType.TessEvaluation:
                        mask |= EProgramStageMask.TessEvaluationShaderBit;
                        break;
                    case EShaderType.Geometry:
                        mask |= EProgramStageMask.GeometryShaderBit;
                        break;
                    case EShaderType.Fragment:
                        mask |= EProgramStageMask.FragmentShaderBit;
                        break;
                    case EShaderType.Compute:
                        mask |= EProgramStageMask.ComputeShaderBit;
                        break;
                }
            }
            return mask;
        }

        public XRRenderProgram(bool linkNow, params XRShader[] shaders)
            : this(shaders, linkNow) { }

        public XRRenderProgram(IEnumerable<XRShader> shaders, bool linkNow = true)
        {
            Shaders.AddRange(shaders);
            if (linkNow)
                Link();
        }

        public IEnumerator<XRShader> GetEnumerator()
            => ((IEnumerable<XRShader>)Shaders).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)Shaders).GetEnumerator();

        /// <summary>
        /// Sends a Matrix4x4 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, Matrix4x4 value)
            => UniformSetMatrix4x4Requested?.Invoke(name, value);
        /// <summary>
        /// Sends a Quaternion property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, Quaternion value)
            => UniformSetQuaternionRequested?.Invoke(name, value);

        /// <summary>
        /// Sends a Matrix4x4[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, Matrix4x4[] value)
            => UniformSetMatrix4x4ArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a Quaternion[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, Quaternion[] value)
            => UniformSetQuaternionArrayRequested?.Invoke(name, value);

        /// <summary>
        /// Sends a bool property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, bool value)
            => UniformSetBoolRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a BoolVector2 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, BoolVector2 value)
            => UniformSetBoolVector2Requested?.Invoke(name, value);
        /// <summary>
        /// Sends a BoolVector3 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, BoolVector3 value)
            => UniformSetBoolVector3Requested?.Invoke(name, value);
        /// <summary>
        /// Sends a BoolVector4 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, BoolVector4 value)
            => UniformSetBoolVector4Requested?.Invoke(name, value);

        /// <summary>
        /// Sends a float property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, float value)
            => UniformSetFloatRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a Vector2 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, Vector2 value)
            => UniformSetVector2Requested?.Invoke(name, value);
        /// <summary>
        /// Sends a Vector3 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, Vector3 value)
            => UniformSetVector3Requested?.Invoke(name, value);
        /// <summary>
        /// Sends a Vector4 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, Vector4 value)
            => UniformSetVector4Requested?.Invoke(name, value);

        /// <summary>
        /// Sends a float[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, float[] value)
            => UniformSetFloatArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a Vector2[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, Vector2[] value)
            => UniformSetVector2ArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a Vector3[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, Vector3[] value)
            => UniformSetVector3ArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a Vector4[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, Vector4[] value)
            => UniformSetVector4ArrayRequested?.Invoke(name, value);

        /// <summary>
        /// Sends a double property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, double value)
            => UniformSetDoubleRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a DVector2 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, DVector2 value)
            => UniformSetDVector2Requested?.Invoke(name, value);
        /// <summary>
        /// Sends a DVector3 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, DVector3 value)
            => UniformSetDVector3Requested?.Invoke(name, value);
        /// <summary>
        /// Sends a DVector4 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, DVector4 value)
            => UniformSetDVector4Requested?.Invoke(name, value);

        /// <summary>
        /// Sends a double[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, double[] value)
            => UniformSetDoubleArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a DVector2[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, DVector2[] value)
            => UniformSetDVector2ArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a DVector3[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, DVector3[] value)
            => UniformSetDVector3ArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a DVector4[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, DVector4[] value)
            => UniformSetDVector4ArrayRequested?.Invoke(name, value);

        /// <summary>
        /// Sends a int property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, int value)
            => UniformSetIntRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a IVector2 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, IVector2 value)
            => UniformSetIVector2Requested?.Invoke(name, value);
        /// <summary>
        /// Sends a IVector3 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, IVector3 value)
            => UniformSetIVector3Requested?.Invoke(name, value);
        /// <summary>
        /// Sends a IVector4 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, IVector4 value)
            => UniformSetIVector4Requested?.Invoke(name, value);

        /// <summary>
        /// Sends a int[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, int[] value)
            => UniformSetIntArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a IVector2[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, IVector2[] value)
            => UniformSetIVector2ArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a IVector3[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, IVector3[] value)
            => UniformSetIVector3ArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a IVector4[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, IVector4[] value)
            => UniformSetIVector4ArrayRequested?.Invoke(name, value);

        /// <summary>
        /// Sends a uint property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, uint value)
            => UniformSetUIntRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a UVector2 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, UVector2 value)
            => UniformSetUVector2Requested?.Invoke(name, value);
        /// <summary>
        /// Sends a UVector3 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, UVector3 value)
            => UniformSetUVector3Requested?.Invoke(name, value);
        /// <summary>
        /// Sends a UVector4 property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, UVector4 value)
            => UniformSetUVector4Requested?.Invoke(name, value);

        /// <summary>
        /// Sends a uint[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, uint[] value)
            => UniformSetUIntArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a UVector2[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, UVector2[] value)
            => UniformSetUVector2ArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a UVector3[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, UVector3[] value)
            => UniformSetUVector3ArrayRequested?.Invoke(name, value);
        /// <summary>
        /// Sends a UVector4[] property value to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Uniform(string name, UVector4[] value)
            => UniformSetUVector4ArrayRequested?.Invoke(name, value);

        /// <summary>
        /// Sends a texture to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Sampler(string name, XRTexture texture, int textureUnit)
            => SamplerRequested?.Invoke(name, texture, textureUnit);
        /// <summary>
        /// Sends a texture to the shader program.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Sampler(int location, XRTexture texture, int textureUnit)
            => SamplerRequestedByLocation?.Invoke(location, texture, textureUnit);

        public enum EImageAccess
        {
            ReadOnly,
            WriteOnly,
            ReadWrite
        }

        public enum EImageFormat
        {
            R8,
            R16,
            R16F,
            R32F,
            RG8,
            RG16,
            RG16F,
            RG32F,
            RGB8,
            RGB16,
            RGB16F,
            RGB32F,
            RGBA8,
            RGBA16,
            RGBA16F,
            RGBA32F,
            R8I,
            R8UI,
            R16I,
            R16UI,
            R32I,
            R32UI,
            RG8I,
            RG8UI,
            RG16I,
            RG16UI,
            RG32I,
            RG32UI,
            RGB8I,
            RGB8UI,
            RGB16I,
            RGB16UI,
            RGB32I,
            RGB32UI,
            RGBA8I,
            RGBA8UI,
            RGBA16I,
            RGBA16UI,
            RGBA32I,
            RGBA32UI
        }

        public void BindImageTexture(uint unit, XRTexture texture, int level, bool layered, int layer, EImageAccess access, EImageFormat format)
            => BindImageTextureRequested?.Invoke(unit, texture, level, layered, layer, access, format);

        public void DispatchCompute(uint x, uint y, uint z, IEnumerable<(uint unit, XRTexture texture, int level, int? layer, EImageAccess access, EImageFormat format)>? textures = null)
            => DispatchComputeRequested?.Invoke(x, y, z, textures);
    }
}
