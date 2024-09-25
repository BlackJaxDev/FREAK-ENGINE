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
        public class GLRenderProgram : GLObject<XRRenderProgram>, IEnumerable<GLShader>
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
                location = Api.GetUniformLocation(BindingId, name);
                if (location < 0)
                {
                    //Debug.LogWarning($"Uniform {name} not found in OpenGL program.");
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
                location = Api.GetAttribLocation(BindingId, name);
                if (location < 0)
                {
                    //Debug.LogWarning($"Attribute {name} not found in OpenGL program.");
                    return false;
                }
                return true;
            }

            public GLRenderProgram(OpenGLRenderer renderer, XRRenderProgram data)
                : base(renderer, data)
            {
                //data.UniformLocationRequested = GetUniformLocation;

                data.UniformSetVector2Requested += Uniform;
                data.UniformSetVector3Requested += Uniform;
                data.UniformSetVector4Requested += Uniform;
                data.UniformSetQuaternionRequested += Uniform;
                data.UniformSetIntRequested += Uniform;
                data.UniformSetFloatRequested += Uniform;
                data.UniformSetUIntRequested += Uniform;
                data.UniformSetDoubleRequested += Uniform;
                data.UniformSetMatrix4x4Requested += Uniform;

                data.UniformSetVector2ArrayRequested += Uniform;
                data.UniformSetVector3ArrayRequested += Uniform;
                data.UniformSetVector4ArrayRequested += Uniform;
                data.UniformSetQuaternionArrayRequested += Uniform;
                data.UniformSetIntArrayRequested += Uniform;
                data.UniformSetFloatArrayRequested += Uniform;
                data.UniformSetUIntArrayRequested += Uniform;
                data.UniformSetDoubleArrayRequested += Uniform;
                data.UniformSetMatrix4x4ArrayRequested += Uniform;
            }

            private void Reset()
            {
                IsLinked = false;
                _attribCache.Clear();
                _uniformCache.Clear();
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
            protected override uint CreateObject()
            {
                Reset();

                _shaderCache = Data.Shaders.Select(CreateAndGenerate).ToArray();

                if (_shaderCache.Length == 0)
                    return InvalidBindingId;

                if (_shaderCache.Any(x => !x.IsCompiled))
                    return InvalidBindingId;

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
            //private void UnlinkShader(GLShader shader)
            //{
            //    Api.DetachShader(shader.BindingId, BindingId);
            //    shader.ActivePrograms.Remove(this);
            //    shader.Destroy();
            //}
            protected internal override void PostGenerated()
            {
                if (!TryGetBindingId(out uint bindingId))
                    return;
                
                _shaderCache.ForEach(LinkShader);

                Api.LinkProgram(bindingId);
                Api.GetProgram(bindingId, GLEnum.LinkStatus, out int status);
                IsLinked = status != 0;
                if (!IsLinked)
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

                //_shaderCache.ForEach(UnlinkShader);

                base.PostGenerated();
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
                fixed (Vector2* ptr = p)
                {
                    Api.ProgramUniform2(BindingId, location, (uint)p.Length, (float*)ptr);
                }
            }
            public void Uniform(int location, Vector3[] p)
            {
                fixed (Vector3* ptr = p)
                {
                    Api.ProgramUniform3(BindingId, location, (uint)p.Length, (float*)ptr);
                }
            }
            public void Uniform(int location, Vector4[] p)
            {
                fixed (Vector4* ptr = p)
                {
                    Api.ProgramUniform4(BindingId, location, (uint)p.Length, (float*)ptr);
                }
            }
            public void Uniform(int location, Quaternion[] p)
            {
                fixed (Quaternion* ptr = p)
                {
                    Api.ProgramUniform4(BindingId, location, (uint)p.Length, (float*)ptr);
                }
            }
            public void Uniform(int location, int[] p)
            {
                fixed (int* ptr = p)
                {
                    Api.ProgramUniform1(BindingId, location, (uint)p.Length, ptr);
                }
            }
            public void Uniform(int location, float[] p)
            {
                fixed (float* ptr = p)
                {
                    Api.ProgramUniform1(BindingId, location, (uint)p.Length, ptr);
                }
            }
            public void Uniform(int location, uint[] p)
            {
                fixed (uint* ptr = p)
                {
                    Api.ProgramUniform1(BindingId, location, (uint)p.Length, ptr);
                }
            }
            public void Uniform(int location, double[] p)
            {
                fixed (double* ptr = p)
                {
                    Api.ProgramUniform1(BindingId, location, (uint)p.Length, ptr);
                }
            }
            public void Uniform(int location, Matrix4x4[] p)
            {
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
}