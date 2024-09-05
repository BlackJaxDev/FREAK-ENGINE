using Extensions;
using Silk.NET.OpenGL;

namespace XREngine.Rendering.OpenGL
{
    public unsafe partial class OpenGLRenderer
    {
        public class GLShader : GLObject<XRShader>
        {
            public GLShader(OpenGLRenderer renderer, XRShader data) : base(renderer, data) 
            {
                Data.PropertyChanged += Data_PropertyChanged;
                OnSourceChanged();
            }

            private void Data_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(XRShader.Source))
                    OnSourceChanged();
            }

            public override GLObjectType Type => GLObjectType.Shader;

            public event DelCompile? Compiled;
            public event Action? SourceChanged;

            public string? SourceText => Data.Source;
            public EShaderType Mode => Data.Type;

            public string? LocalIncludeDirectoryPath { get; set; } = null;

            public bool IsCompiled { get; private set; } = false;
            public GLRenderProgram? OwningProgram { get; set; }

            public void SetSource(bool compile = true)
            {
                IsCompiled = false;

                if (!IsGenerated)
                    return;

                Renderer.CurrentShaderMode = ToGLEnum(Mode);

                string? trueScript = ResolveFullSource();
                if (trueScript is null)
                    return;

                Api.ShaderSource(BindingId, trueScript);

                if (compile)
                {
                    bool success = Compile(out _);
                    if (!success)
                        Debug.Out(GetFullSource(true));
                }

                SourceChanged?.Invoke();

                if (OwningProgram != null && OwningProgram.IsGenerated)
                {
                    OwningProgram.Destroy();
                    OwningProgram.Generate();
                }
            }

            private ShaderType ToGLEnum(EShaderType mode)
            {
                throw new NotImplementedException();
            }

            private void OnSourceChanged()
            {
                IsCompiled = false;
                //if (!IsActive)
                //    return;
                //Engine.Renderer.SetShaderMode(ShaderMode);
                //Engine.Renderer.SetShaderSource(BindingId, _sourceCache);
                //bool success = Compile(out string info);
                //if (!success)
                //    Engine.PrintLine(GetSource(true));
                Destroy();
                SourceChanged?.Invoke();
                if (OwningProgram != null && OwningProgram.IsGenerated)
                {
                    OwningProgram.Destroy();
                    //OwningProgram.Generate();
                }
            }
            protected internal override void PreGenerated()
            {
                Renderer.CurrentShaderMode = ToGLEnum(Mode);
            }
            protected internal override void PostGenerated()
            {
                if (SourceText == null || SourceText.Length == 0)
                    return;
                
                Renderer.CurrentShaderMode = ToGLEnum(Mode);
                string? trueScript = ResolveFullSource();
                Api.ShaderSource(BindingId, trueScript);
                if (!Compile(out _))
                    Debug.Out(GetFullSource(true));
            }
            public string GetFullSource(bool lineNumbers)
            {
                string? source = string.Empty;
                string? trueScript = ResolveFullSource();
                if (lineNumbers)
                {
                    //Split the source by new lines
                    string[]? s = trueScript?.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                    //Add the line number to the source so we can go right to errors on specific lines
                    if (s != null)
                        for (int i = 0; i < s.Length; i++)
                            source += $"{(i + 1).ToString().PadLeft(s.Length.ToString().Length, '0')}: {s[i] ?? string.Empty} {Environment.NewLine}";
                }
                else
                    source += trueScript + Environment.NewLine;
                return source;
            }
            public bool Compile(out string? info, bool printLogInfo = true)
            {
                Renderer.CurrentShaderMode = ToGLEnum(Mode);
                IsCompiled = Renderer.CompileShader(BindingId, out info);
                if (printLogInfo)
                {
                    if (!string.IsNullOrEmpty(info))
                        Debug.Out(info);
                    else if (!IsCompiled)
                        Debug.Out("Unable to compile shader, but no error was returned.");

                }
                Compiled?.Invoke(IsCompiled, info);
                return IsCompiled;
            }
            public string? ResolveFullSource()
            {
                List<string?> resolvedPaths = [];
                string? src = ResolveIncludesRecursive(SourceText, resolvedPaths) ?? SourceText;

                if (resolvedPaths.Count > 0)
                    Debug.Out($"Resolved {resolvedPaths.Count} includes:{Environment.NewLine}{src}");
                
                return src;
            }

            /// <summary>
            /// Searches for '#include' directives in the source text and replaces them with the contents of the included file.
            /// </summary>
            /// <param name="sourceText"></param>
            /// <param name="resolvedPaths"></param>
            /// <returns></returns>
            private string? ResolveIncludesRecursive(string? sourceText, List<string?> resolvedPaths)
            {
                if (string.IsNullOrEmpty(sourceText))
                    return null;

                int[] includeLocations = sourceText.FindAllOccurrences(0, "#include");
                (int Index, int Length, string InsertText)[] insertions = new (int, int, string)[includeLocations.Length];
                for (int i = 0; i < includeLocations.Length; ++i)
                {
                    int loc = includeLocations[i];
                    int pathIndex = loc + 8;
                    while (char.IsWhiteSpace(sourceText[pathIndex])) ++pathIndex;
                    char first = sourceText[pathIndex];
                    int endIndex;
                    int startIndex;
                    if (first == '"')
                    {
                        startIndex = pathIndex + 1;
                        endIndex = sourceText.FindFirst(pathIndex + 1, '"');
                    }
                    else
                    {
                        startIndex = pathIndex;
                        endIndex = sourceText.FindFirst(pathIndex + 1, x => char.IsWhiteSpace(x));
                    }
                    string fileText;
                    string includePath = sourceText[startIndex..endIndex];
                    if (string.IsNullOrWhiteSpace(includePath))
                        fileText = string.Empty;
                    else
                    {
                        try
                        {
                            if (!includePath.IsAbsolutePath())
                            {
                                bool valid = false;
                                string? fullPath = null;
                                string?[] dirCheckPaths = 
                                [
                                    LocalIncludeDirectoryPath,
                                    //File?.DirectoryPath,
                                    //Engine.Game?.DirectoryPath
                                ];
                                foreach (string? dirPath in dirCheckPaths)
                                {
                                    if (!string.IsNullOrWhiteSpace(dirPath))
                                    {
                                        fullPath = Path.Combine(dirPath, includePath);
                                        valid = File.Exists(fullPath);
                                        if (valid)
                                            break;
                                    }
                                }
                                includePath = !valid ? Path.GetFullPath(includePath) : fullPath ?? string.Empty;
                            }
                            if (resolvedPaths.Contains(includePath))
                            {
                                //Infinite recursion, path already visited
                                Debug.Out($"Infinite include recursion detected; the path '{includePath}' will not be included again.");
                                fileText = string.Empty;
                            }
                            else
                            {
                                fileText = File.ReadAllText(includePath);
                                resolvedPaths.Add(includePath);
                                fileText = ResolveIncludesRecursive(fileText, resolvedPaths) ?? string.Empty;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Out(ex.Message);
                            fileText = string.Empty;
                        }
                    }
                    insertions[i] = (loc, endIndex + 1 - loc, fileText);
                }

                int offset = 0;
                int index;
                foreach (var (Index, Length, InsertText) in insertions)
                {
                    index = Index + offset;
                    sourceText = sourceText.Remove(index, Length);
                    sourceText = sourceText.Insert(index, InsertText);
                    offset += InsertText.Length - Length;
                }

                return sourceText;
            }
        }

        public ShaderType CurrentShaderMode { get; private set; } = ShaderType.FragmentShader;

        public bool CompileShader(uint bindingId, out string? info)
        {
            Api.CompileShader(bindingId);
#if DEBUG
            Api.GetShader(bindingId, GLEnum.CompileStatus, out int status);
            Api.GetShaderInfoLog(bindingId, out info);
#else
            info = null;
#endif
            return status != 0;
        }
    }
}