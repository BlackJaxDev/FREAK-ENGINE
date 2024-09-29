using Extensions;
using Silk.NET.OpenGL;
using System.Collections;
using System.Collections.Concurrent;
using System.Numerics;

namespace XREngine.Rendering.OpenGL
{
    public unsafe partial class OpenGLRenderer
    {
        public delegate void DelCompile(bool compiledSuccessfully, string? compileInfo);
        public class GLRenderProgram(OpenGLRenderer renderer, XRRenderProgram data) : GLObject<XRRenderProgram>(renderer, data), IEnumerable<GLShader>
        {
            private bool _isValid = true;
            public bool IsLinked
            {
                get => _isValid;
                private set => SetField(ref _isValid, value);
            }

            public override GLObjectType Type => GLObjectType.Program;

            private readonly ConcurrentDictionary<string, int>
                _uniformCache = new(),
                _attribCache = new();

            private readonly ConcurrentBag<string> _failedAttributes = [];
            private readonly ConcurrentBag<string> _failedUniforms = [];

            private GLShader[] _shaderCache = [];
            protected GLShader[] ShaderObjects
            {
                get => _shaderCache;
                set => SetField(ref _shaderCache, value ?? []);
            }

            protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
            {
                base.OnPropertyChanged(propName, prev, field);
                switch (propName)
                {
                    case nameof(Data.Shaders):
                        //Force a recompilation - programs cannot be relinked.
                        Destroy();
                        break;
                }
            }

            /// <summary>
            /// If the program has been generated and linked successfully,
            /// this will return the location of the uniform with the given name.
            /// Cached for performance and thread-safe.
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public int GetUniformLocation(string name)
            {
                if (_uniformCache.TryGetValue(name, out int value))
                    return value;

                if (!GetUniform(name, out value))
                    return -1;

                _uniformCache.TryAdd(name, value);
                return value;
            }
            private bool GetUniform(string name, out int location)
            {
                bool failed = _failedUniforms.Contains(name);
                if (failed)
                {
                    location = -1;
                    return false;
                }
                location = Api.GetUniformLocation(BindingId, name);
                if (location < 0)
                {
                    if (!failed)
                    {
                        _failedUniforms.Add(name);
                        Debug.LogWarning($"Uniform {name} not found in OpenGL program.");
                    }
                    return false;
                }
                return true;
            }
            /// <summary>
            /// If the program has been generated and linked successfully,
            /// this will return the location of the attribute with the given name.
            /// Cached for performance and thread-safe.
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public int GetAttributeLocation(string name)
            {
                if (_attribCache.TryGetValue(name, out int value))
                    return value;

                if (!GetAttribute(name, out value))
                    return -1;

                _attribCache.TryAdd(name, value);
                return value;
            }
            private bool GetAttribute(string name, out int location)
            {
                bool failed = _failedAttributes.Contains(name);
                if (failed)
                {
                    location = -1;
                    return false;
                }
                location = Api.GetAttribLocation(BindingId, name);
                if (location < 0)
                {
                    if (!failed)
                    {
                        _failedAttributes.Add(name);
                        Debug.LogWarning($"Attribute {name} not found in OpenGL program.");
                    }
                    return false;
                }
                return true;
            }

            protected override void UnlinkData()
            {
                Data.UniformSetVector2Requested -= Uniform;
                Data.UniformSetVector3Requested -= Uniform;
                Data.UniformSetVector4Requested -= Uniform;
                Data.UniformSetQuaternionRequested -= Uniform;
                Data.UniformSetIntRequested -= Uniform;
                Data.UniformSetFloatRequested -= Uniform;
                Data.UniformSetUIntRequested -= Uniform;
                Data.UniformSetDoubleRequested -= Uniform;
                Data.UniformSetMatrix4x4Requested -= Uniform;

                Data.UniformSetVector2ArrayRequested -= Uniform;
                Data.UniformSetVector3ArrayRequested -= Uniform;
                Data.UniformSetVector4ArrayRequested -= Uniform;
                Data.UniformSetQuaternionArrayRequested -= Uniform;
                Data.UniformSetIntArrayRequested -= Uniform;
                Data.UniformSetFloatArrayRequested -= Uniform;
                Data.UniformSetUIntArrayRequested -= Uniform;
                Data.UniformSetDoubleArrayRequested -= Uniform;
                Data.UniformSetMatrix4x4ArrayRequested -= Uniform;
            }

            protected override void LinkData()
            {
                //data.UniformLocationRequested = GetUniformLocation;

                Data.UniformSetVector2Requested += Uniform;
                Data.UniformSetVector3Requested += Uniform;
                Data.UniformSetVector4Requested += Uniform;
                Data.UniformSetQuaternionRequested += Uniform;
                Data.UniformSetIntRequested += Uniform;
                Data.UniformSetFloatRequested += Uniform;
                Data.UniformSetUIntRequested += Uniform;
                Data.UniformSetDoubleRequested += Uniform;
                Data.UniformSetMatrix4x4Requested += Uniform;

                Data.UniformSetVector2ArrayRequested += Uniform;
                Data.UniformSetVector3ArrayRequested += Uniform;
                Data.UniformSetVector4ArrayRequested += Uniform;
                Data.UniformSetQuaternionArrayRequested += Uniform;
                Data.UniformSetIntArrayRequested += Uniform;
                Data.UniformSetFloatArrayRequested += Uniform;
                Data.UniformSetUIntArrayRequested += Uniform;
                Data.UniformSetDoubleArrayRequested += Uniform;
                Data.UniformSetMatrix4x4ArrayRequested += Uniform;
            }

            private void Reset()
            {
                IsLinked = false;
                _attribCache.Clear();
                _uniformCache.Clear();
                _failedAttributes.Clear();
                _failedUniforms.Clear();
            }

            public override void Destroy()
            {
                base.Destroy();
                Reset();
            }

            private GLShader CreateAndGenerate(XRShader data)
            {
                GLShader shader = Renderer.GenericToAPI<GLShader>(data)!;
                shader.Generate();
                return shader;
            }
            //TODO: serialize this cache and load on startup
            private readonly ConcurrentDictionary<ulong, BinaryProgram> _binaryCache = new();
            public ulong Hash { get; private set; }
            private BinaryProgram? _cachedProgram = null;
            protected override uint CreateObject()
            {
                Reset();

                if (Data.Shaders.Count == 0)
                {
                    Debug.LogWarning("No shaders were provided to the program.");
                    return InvalidBindingId;
                }

                Hash = CalcHash(Data.Shaders.Select(x => x.Source.Text ?? string.Empty));
                bool isCached = _binaryCache.TryGetValue(Hash, out var binProg);

                if (isCached)
                    _cachedProgram = binProg;
                else
                {
                    _cachedProgram = null;

                    //Try compiling the shaders
                    _shaderCache = Data.Shaders.Select(CreateAndGenerate).ToArray();
                    if (_shaderCache.Any(x => !x.IsCompiled))
                    {
                        Debug.LogWarning("One or more shaders failed to compile.");
                        _shaderCache.ForEach(x => x.Destroy());
                        _shaderCache = [];
                        return InvalidBindingId;
                    }
                }

                uint handle = Api.CreateProgram();
                bool separable = Engine.Rendering.Settings.AllowShaderPipelines;
                Api.ProgramParameter(handle, GLEnum.ProgramSeparable, separable ? 1 : 0);

                return handle;
            }

            private void LinkShader(GLShader shader)
            {
                shader.ActivePrograms.Add(this);
                Api.AttachShader(BindingId, shader.BindingId);
            }
            private void UnlinkShader(GLShader shader)
            {
                Api.DetachShader(BindingId, shader.BindingId);
                shader.ActivePrograms.Remove(this);
                shader.Destroy();
            }

            protected internal override void PostGenerated()
            {
                if (!TryGetBindingId(out uint bindingId))
                    return;

                if (_cachedProgram is null)
                    LinkNewProgram(bindingId);
                else
                    LoadCachedProgram(bindingId);
                
                base.PostGenerated();
            }

            private void LoadCachedProgram(uint bindingId)
            {
                fixed (byte* ptr = _cachedProgram!.Value.Binary)
                    Api.ProgramBinary(bindingId, _cachedProgram.Value.Format, ptr, _cachedProgram.Value.Length);
            }

            private void LinkNewProgram(uint bindingId)
            {
                _shaderCache.ForEach(LinkShader);

                Api.LinkProgram(bindingId);
                Api.GetProgram(bindingId, GLEnum.LinkStatus, out int status);
                IsLinked = status != 0;
                if (IsLinked)
                    CacheBinary(bindingId);
                else
                    PrintLinkDebug(bindingId);

                _shaderCache.ForEach(UnlinkShader);
                _shaderCache = [];
            }

            private void PrintLinkDebug(uint bindingId)
            {
                Api.GetProgramInfoLog(bindingId, out string info);
                Debug.Out(string.IsNullOrWhiteSpace(info)
                    ? "Unable to link program, but no error was returned."
                    : info);

                //if (info.Contains("Vertex info"))
                //{
                //    RenderShader s = _shaders.FirstOrDefault(x => x.File.Type == EShaderMode.Vertex);
                //    string source = s.GetSource(true);
                //    Engine.PrintLine(source);
                //}
                //else if (info.Contains("Geometry info"))
                //{
                //    RenderShader s = _shaders.FirstOrDefault(x => x.File.Type == EShaderMode.Geometry);
                //    string source = s.GetSource(true);
                //    Engine.PrintLine(source);
                //}
                //else if (info.Contains("Fragment info"))
                //{
                //    RenderShader s = _shaders.FirstOrDefault(x => x.File.Type == EShaderMode.Fragment);
                //    string source = s.GetSource(true);
                //    Engine.PrintLine(source);
                //}
            }

            private void CacheBinary(uint bindingId)
            {
                Api.GetProgram(bindingId, GLEnum.ProgramBinaryLength, out int len);
                if (len <= 0)
                    return;
                
                byte[] binary = new byte[len];
                GLEnum format;
                uint binaryLength;
                fixed (byte* ptr = binary)
                {
                    Api.GetProgramBinary(bindingId, (uint)len, &binaryLength, &format, ptr);
                }
                _binaryCache.TryAdd(Hash, (binary, format, binaryLength));
            }

            private static ulong CalcHash(IEnumerable<string> enumerable)
            {
                ulong hash = 17ul;
                foreach (string item in enumerable)
                    hash = hash * 31ul + (ulong)(item?.GetHashCode() ?? 0);
                return hash;
            }

            public void Use()
                => Api.UseProgram(BindingId);

            public IEnumerator<GLShader> GetEnumerator()
                => ((IEnumerable<GLShader>)_shaderCache).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator()
                => ((IEnumerable<GLShader>)_shaderCache).GetEnumerator();

            #region Uniforms
            public void Uniform(EEngineUniform name, Vector2 p)
                => Uniform(name.ToString(), p);
            public void Uniform(EEngineUniform name, Vector3 p)
                => Uniform(name.ToString(), p);
            public void Uniform(EEngineUniform name, Vector4 p)
                => Uniform(name.ToString(), p);
            public void Uniform(EEngineUniform name, Quaternion p)
                => Uniform(name.ToString(), p);
            public void Uniform(EEngineUniform name, int p)
                => Uniform(name.ToString(), p);
            public void Uniform(EEngineUniform name, float p)
                => Uniform(name.ToString(), p);
            public void Uniform(EEngineUniform name, uint p)
                => Uniform(name.ToString(), p);
            public void Uniform(EEngineUniform name, double p)
                => Uniform(name.ToString(), p);
            public void Uniform(EEngineUniform name, Matrix4x4 p)
                => Uniform(name.ToString(), p);

            public void Uniform(string name, Vector2 p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, Vector3 p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, Vector4 p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, Quaternion p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, int p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, float p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, uint p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, double p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, Matrix4x4 p)
                => Uniform(GetUniformLocation(name), p);

            public void Uniform(int location, Vector2 p)
                => Api.ProgramUniform2(BindingId, location, p);
            public void Uniform(int location, Vector3 p)
                => Api.ProgramUniform3(BindingId, location, p);
            public void Uniform(int location, Vector4 p)
                => Api.ProgramUniform4(BindingId, location, p);
            public void Uniform(int location, Quaternion p)
                => Api.ProgramUniform4(BindingId, location, p);
            public void Uniform(int location, int p)
                => Api.ProgramUniform1(BindingId, location, p);
            public void Uniform(int location, float p)
                => Api.ProgramUniform1(BindingId, location, p);
            public void Uniform(int location, uint p)
                => Api.ProgramUniform1(BindingId, location, p);
            public void Uniform(int location, double p)
                => Api.ProgramUniform1(BindingId, location, p);
            public void Uniform(int location, Matrix4x4 p)
                => Api.ProgramUniformMatrix4(BindingId, location, 1, false, &p.M11);

            public void Uniform(string name, Vector2[] p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, Vector3[] p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, Vector4[] p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, Quaternion[] p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, int[] p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, float[] p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, uint[] p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, double[] p)
                => Uniform(GetUniformLocation(name), p);
            public void Uniform(string name, Matrix4x4[] p)
                => Uniform(GetUniformLocation(name), p);

            public void Uniform(int location, Vector2[] p)
            {
                if (location < 0)
                    return;
                fixed (Vector2* ptr = p)
                {
                    Api.ProgramUniform2(BindingId, location, (uint)p.Length, (float*)ptr);
                }
            }
            public void Uniform(int location, Vector3[] p)
            {
                if (location < 0)
                    return;
                fixed (Vector3* ptr = p)
                {
                    Api.ProgramUniform3(BindingId, location, (uint)p.Length, (float*)ptr);
                }
            }
            public void Uniform(int location, Vector4[] p)
            {
                if (location < 0)
                    return;
                fixed (Vector4* ptr = p)
                {
                    Api.ProgramUniform4(BindingId, location, (uint)p.Length, (float*)ptr);
                }
            }
            public void Uniform(int location, Quaternion[] p)
            {
                if (location < 0)
                    return;
                fixed (Quaternion* ptr = p)
                {
                    Api.ProgramUniform4(BindingId, location, (uint)p.Length, (float*)ptr);
                }
            }
            public void Uniform(int location, int[] p)
            {
                if (location < 0)
                    return;
                fixed (int* ptr = p)
                {
                    Api.ProgramUniform1(BindingId, location, (uint)p.Length, ptr);
                }
            }
            public void Uniform(int location, float[] p)
            {
                if (location < 0)
                    return;
                fixed (float* ptr = p)
                {
                    Api.ProgramUniform1(BindingId, location, (uint)p.Length, ptr);
                }
            }
            public void Uniform(int location, uint[] p)
            {
                if (location < 0)
                    return;
                fixed (uint* ptr = p)
                {
                    Api.ProgramUniform1(BindingId, location, (uint)p.Length, ptr);
                }
            }
            public void Uniform(int location, double[] p)
            {
                if (location < 0)
                    return;
                fixed (double* ptr = p)
                {
                    Api.ProgramUniform1(BindingId, location, (uint)p.Length, ptr);
                }
            }
            public void Uniform(int location, Matrix4x4[] p)
            {
                if (location < 0)
                    return;
                fixed (Matrix4x4* ptr = p)
                {
                    Api.ProgramUniformMatrix4(BindingId, location, (uint)p.Length, false, (float*)ptr);
                }
            }
            #endregion

            #region Samplers
            /// <summary>
            /// Passes a texture sampler into the fragment shader of this program by name.
            /// The name is cached so that retrieving the sampler's location is only required once.
            /// </summary>
            public void Sampler(string name, IGLTexture texture, int textureUnit)
            {
                Renderer.SetActiveTexture(textureUnit);
                Uniform(name, textureUnit);
                texture.Bind();
            }

            /// <summary>
            /// Passes a texture sampler value into the fragment shader of this program by location.
            /// </summary>
            public void Sampler(int location, IGLTexture texture, int textureUnit)
            {
                Renderer.SetActiveTexture(textureUnit);
                Uniform(location, textureUnit);
                texture.Bind();
            }
            #endregion
        }

        private void SetActiveTexture(int textureUnit)
            => Api.ActiveTexture(GLEnum.Texture0 + textureUnit);
    }

    internal record struct BinaryProgram(byte[] Binary, GLEnum Format, uint Length)
    {
        public static implicit operator (byte[] bin, GLEnum fmt, uint len)(BinaryProgram value)
            => (value.Binary, value.Format, value.Length);

        public static implicit operator BinaryProgram((byte[] bin, GLEnum fmt, uint len) value)
            => new(value.bin, value.fmt, value.len);
    }
}