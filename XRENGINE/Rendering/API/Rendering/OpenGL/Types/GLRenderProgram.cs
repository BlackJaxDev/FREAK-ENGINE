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
            private GLShader[] _shaderObjects = [];
            protected GLShader[] ShaderObjects
            {
                get => _shaderObjects;
                set
                {
                    _shaderObjects = value ?? [];
                    OnShaderObjectsChanged();
                }
            }

            private void OnShaderObjectsChanged()
            {
                if (_shaderObjects.Any(x => x is null))
                    _shaderObjects = _shaderObjects.Where(x => x != null).ToArray();

                foreach (var shader in _shaderObjects)
                    shader.SourceText = this;

                //Force a recompilation.
                //TODO: recompile shaders without destroying program.
                //Need to attach shaders by id to the program and recompile.
                Destroy();
            }

            private readonly ConcurrentDictionary<string, int>
                _uniformCache = new(),
                _attribCache = new();

            //private Stack<string> StructStack { get; } = new Stack<string>();
            //private string StructStackString { get; set; }
            //public void PushTargetStruct(string targetStructName)
            //{
            //    StructStack.Push(targetStructName);
            //    RemakeStructStack();
            //}
            //public void PopTargetStruct()
            //{
            //    StructStack.Pop();
            //    RemakeStructStack();
            //}
            //private void RemakeStructStack()
            //{
            //    StructStackString = string.Empty;
            //    foreach (string str in StructStack)
            //        StructStackString += $"{str}.";
            //}

            public int GetUniformLocation(string name)
            {
                if (!IsGenerated)
                    return -1;

                return _uniformCache.GetOrAdd(name, n => Api.GetUniformLocation(BindingId, n));
            }
            public int GetAttributeLocation(string name)
            {

                if (!IsGenerated)
                    return -1;

                return _attribCache.GetOrAdd(name, n => Api.GetAttribLocation(BindingId, n));
            }

            public bool IsValid { get; private set; } = true;

            public override GLObjectType Type => GLObjectType.Program;

            public GLRenderProgram(OpenGLRenderer renderer, XRRenderProgram data)
                : base(renderer, data)
            {
                if (data.Shaders.Count == 0)
                    Debug.LogWarning("No shaders were attached to the program.");
                
                _shaderObjects = data.Shaders.Select(x => new GLShader(renderer, x)).ToArray();
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

                OnShaderObjectsChanged();
            }

            public override void Destroy()
            {
                base.Destroy();

                IsValid = true;

                _attribCache.Clear();
                _uniformCache.Clear();
            }

            protected override uint CreateObject()
            {
                //Reset caches in case the attached shaders have since the program was last active.
                _attribCache.Clear();
                _uniformCache.Clear();

                IsValid = true;

                if (_shaderObjects.Length == 0)
                {
                    IsValid = false;
                    return InvalidBindingId;
                }

                _shaderObjects.ForEach(x => x.Generate());

                if (_shaderObjects.Any(x => !x.IsCompiled))
                {
                    IsValid = false;
                    return InvalidBindingId;
                }

                uint id = Renderer.GenerateProgram(Engine.Rendering.Settings.AllowShaderPipelines);

                _shaderObjects.ForEach(x => Api.AttachShader(x.BindingId, id));

                bool valid = Renderer.LinkProgram(id, out string? info);
                if (!valid)
                {
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

                    IsValid = false;
                    Api.DeleteProgram(id);
                    return InvalidBindingId;
                }

                _shaderObjects.ForEach(x =>
                {
                    Api.DetachShader(x.BindingId, id);
                    x.Destroy();
                });

                return id;
            }

            public uint GenerateProgram(bool separable)
            {
                uint handle = Api.CreateProgram();
                Api.ProgramParameter(handle, GLEnum.ProgramSeparable, separable ? 1 : 0);
                return handle;
            }

            public void Use()
                => Api.UseProgram(BindingId);

            public IEnumerator<GLShader> GetEnumerator()
                => ((IEnumerable<GLShader>)_shaderObjects).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator()
                => ((IEnumerable<GLShader>)_shaderObjects).GetEnumerator();

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

        private uint GenerateProgram(bool allowShaderPipelines)
        {
            uint handle = Api.CreateProgram();
            Api.ProgramParameter(handle, GLEnum.ProgramSeparable, allowShaderPipelines ? 1 : 0);
            return handle;
        }

        private void SetActiveTexture(int textureUnit)
            => Api.ActiveTexture(GLEnum.Texture0 + textureUnit);

        public bool LinkProgram(uint bindingId, out string? info)
        {
            info = null;
            Api.LinkProgram(bindingId);
            Api.GetProgram(bindingId, GLEnum.LinkStatus, out int status);
            if (status == 0)
            {
#if DEBUG
                Api.GetProgramInfoLog(bindingId, out info);
                Debug.Out(string.IsNullOrWhiteSpace(info) ? "Unable to link program, but no error was returned." : info);
#endif
                return false;
            }
            else
                return true;
        }
    }
}