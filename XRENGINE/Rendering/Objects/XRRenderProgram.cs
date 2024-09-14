using System.Collections;
using System.Numerics;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering
{
    public class XRRenderProgram : GenericRenderObject, IEnumerable<XRShader>
    {
        public TransformBase? LightProbeTransform { get; set; }

        /// <summary>
        /// The shaders that make up the program.
        /// </summary>
        public EventList<XRShader> Shaders { get; } = [];
        /// <summary>
        /// Set by the renderer to indicate if the program is valid and compiled.
        /// </summary>
        public bool IsValid { get; set; } = true;

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

        /// <summary>
        /// Mask of the shader types included in the program.
        /// </summary>
        public EProgramStageMask ShaderTypeMask
        {
            get
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
        }

        public XRRenderProgram(params XRShader[] shaders)
            => Shaders = [.. shaders];
        public XRRenderProgram(IEnumerable<XRShader> shaders)
            => Shaders = new EventList<XRShader>(shaders);

        public IEnumerator<XRShader> GetEnumerator()
            => ((IEnumerable<XRShader>)Shaders).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)Shaders).GetEnumerator();

        public void Uniform(string name, Matrix4x4 value) => UniformSetMatrix4x4Requested?.Invoke(name, value);
        public void Uniform(string name, Quaternion value) => UniformSetQuaternionRequested?.Invoke(name, value);

        public void Uniform(string name, Matrix4x4[] value) => UniformSetMatrix4x4ArrayRequested?.Invoke(name, value);
        public void Uniform(string name, Quaternion[] value) => UniformSetQuaternionArrayRequested?.Invoke(name, value);

        public void Uniform(string name, bool value) => UniformSetBoolRequested?.Invoke(name, value);
        public void Uniform(string name, BoolVector2 value) => UniformSetBoolVector2Requested?.Invoke(name, value);
        public void Uniform(string name, BoolVector3 value) => UniformSetBoolVector3Requested?.Invoke(name, value);
        public void Uniform(string name, BoolVector4 value) => UniformSetBoolVector4Requested?.Invoke(name, value);

        public void Uniform(string name, float value) => UniformSetFloatRequested?.Invoke(name, value);
        public void Uniform(string name, Vector2 value) => UniformSetVector2Requested?.Invoke(name, value);
        public void Uniform(string name, Vector3 value) => UniformSetVector3Requested?.Invoke(name, value);
        public void Uniform(string name, Vector4 value) => UniformSetVector4Requested?.Invoke(name, value);

        public void Uniform(string name, float[] value) => UniformSetFloatArrayRequested?.Invoke(name, value);
        public void Uniform(string name, Vector2[] value) => UniformSetVector2ArrayRequested?.Invoke(name, value);
        public void Uniform(string name, Vector3[] value) => UniformSetVector3ArrayRequested?.Invoke(name, value);
        public void Uniform(string name, Vector4[] value) => UniformSetVector4ArrayRequested?.Invoke(name, value);

        public void Uniform(string name, double value) => UniformSetDoubleRequested?.Invoke(name, value);
        public void Uniform(string name, DVector2 value) => UniformSetDVector2Requested?.Invoke(name, value);
        public void Uniform(string name, DVector3 value) => UniformSetDVector3Requested?.Invoke(name, value);
        public void Uniform(string name, DVector4 value) => UniformSetDVector4Requested?.Invoke(name, value);

        public void Uniform(string name, double[] value) => UniformSetDoubleArrayRequested?.Invoke(name, value);
        public void Uniform(string name, DVector2[] value) => UniformSetDVector2ArrayRequested?.Invoke(name, value);
        public void Uniform(string name, DVector3[] value) => UniformSetDVector3ArrayRequested?.Invoke(name, value);
        public void Uniform(string name, DVector4[] value) => UniformSetDVector4ArrayRequested?.Invoke(name, value);

        public void Uniform(string name, int value) => UniformSetIntRequested?.Invoke(name, value);
        public void Uniform(string name, IVector2 value) => UniformSetIVector2Requested?.Invoke(name, value);
        public void Uniform(string name, IVector3 value) => UniformSetIVector3Requested?.Invoke(name, value);
        public void Uniform(string name, IVector4 value) => UniformSetIVector4Requested?.Invoke(name, value);

        public void Uniform(string name, int[] value) => UniformSetIntArrayRequested?.Invoke(name, value);
        public void Uniform(string name, IVector2[] value) => UniformSetIVector2ArrayRequested?.Invoke(name, value);
        public void Uniform(string name, IVector3[] value) => UniformSetIVector3ArrayRequested?.Invoke(name, value);
        public void Uniform(string name, IVector4[] value) => UniformSetIVector4ArrayRequested?.Invoke(name, value);

        public void Uniform(string name, uint value) => UniformSetUIntRequested?.Invoke(name, value);
        public void Uniform(string name, UVector2 value) => UniformSetUVector2Requested?.Invoke(name, value);
        public void Uniform(string name, UVector3 value) => UniformSetUVector3Requested?.Invoke(name, value);
        public void Uniform(string name, UVector4 value) => UniformSetUVector4Requested?.Invoke(name, value);

        public void Uniform(string name, uint[] value) => UniformSetUIntArrayRequested?.Invoke(name, value);
        public void Uniform(string name, UVector2[] value) => UniformSetUVector2ArrayRequested?.Invoke(name, value);
        public void Uniform(string name, UVector3[] value) => UniformSetUVector3ArrayRequested?.Invoke(name, value);
        public void Uniform(string name, UVector4[] value) => UniformSetUVector4ArrayRequested?.Invoke(name, value);

        public void Sampler(string name, XRTexture texture, int textureUnit)
            => SamplerRequested?.Invoke(name, texture, textureUnit);
    }
}
