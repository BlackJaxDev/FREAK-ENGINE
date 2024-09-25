namespace XREngine.Rendering.OpenGL
{
    public unsafe partial class OpenGLRenderer
    {
        /// <summary>
        /// OpenGL state wrapper for generic data objects.
        /// </summary>
        public abstract class GLObjectBase : AbstractRenderObject<OpenGLRenderer>, IGLObject
        {
            public const uint InvalidBindingId = 0;
            public abstract GLObjectType Type { get; }

            /// <summary>
            /// True if the object has been generated.
            /// Check this before using the BindingId property, as it will generate the object if it has not been generated yet.
            /// </summary>
            public override bool IsGenerated => _bindingId.HasValue && _bindingId != InvalidBindingId;

            internal uint? _bindingId;

            public bool TryGetBindingId(out uint bindingId)
            {
                if (_bindingId.HasValue)
                {
                    bindingId = _bindingId.Value;
                    return true;
                }
                else
                {
                    bindingId = InvalidBindingId;
                    return false;
                }
            }

            public GLObjectBase(OpenGLRenderer renderer) : base(renderer) { }
            public GLObjectBase(OpenGLRenderer renderer, uint id) : base(renderer) => _bindingId = id;

            public override void Destroy()
            {
                DeleteObject();
            }

            protected internal virtual void PreGenerated()
            {
                if (IsGenerated)
                    DeleteObject();
            }

            protected internal virtual void PostGenerated()
            {

            }

            private bool _invalidated = true;
            private bool _hasSentInvalidationWarning = false;
            protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
            {
                base.OnPropertyChanged(propName, prev, field);
                _invalidated = true;
                _hasSentInvalidationWarning = false;
            }

            /// <summary>
            /// Generates the object on the GPU and assigns it a unique binding id.
            /// </summary>
            public override void Generate()
            {
                if (!_invalidated)
                {
                    if (!_hasSentInvalidationWarning)
                    {
                        Debug.Out($"Attempted to generate an OpenGL object with no changes since last generation attempt. Canceling to avoid infinite recursion on generation fail.");
                        _hasSentInvalidationWarning = true;
                    }
                    return;
                }

                Debug.Out($"Generating OpenGL object {Type}");
                PreGenerated();
                _bindingId = CreateObject();
                if (_bindingId is not null && _bindingId != InvalidBindingId)
                {
                    PostGenerated();
                    _invalidated = false;
                    _hasSentInvalidationWarning = false;
                }
                else
                    Debug.Out("Failed to generate OpenGL object.");
            }

            protected internal virtual void PreDeleted()
            {

            }
            protected internal virtual void PostDeleted()
            {
                _bindingId = null;
            }

            /// <summary>
            /// The unique id of this object when generated.
            /// If not generated yet, the object will be generated on first access.
            /// Generation is deferred until necessary to allow for proper initialization of the object.
            /// </summary>
            public uint BindingId
            {
                get
                {
                    try
                    {
                        if (_bindingId is null)
                            Generate();

                        if (TryGetBindingId(out uint bindingId))
                            return bindingId;
                        else
                        {
                            Debug.LogWarning($"Failed to generate object of type {Type}.");
                            return InvalidBindingId;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex, $"Failed to generate object of type {Type}.");
                        return InvalidBindingId;
                    }
                }
            }

            GenericRenderObject IRenderAPIObject.Data => Data_Internal;
            protected abstract GenericRenderObject Data_Internal { get; }

            protected virtual uint CreateObject()
                => Renderer.CreateObjects(Type, 1)[0];
            protected virtual void DeleteObject()
            {
                if (!IsGenerated)
                    return;
                Debug.Out($"Deleting OpenGL object {Type} {BindingId}");
                PreDeleted();
                uint id = _bindingId!.Value;
                switch (Type)
                {
                    case GLObjectType.Buffer:
                        Api.DeleteBuffer(id);
                        break;
                    case GLObjectType.Framebuffer:
                        Api.DeleteFramebuffer(id);
                        break;
                    case GLObjectType.Program:
                        Api.DeleteProgram(id);
                        break;
                    case GLObjectType.ProgramPipeline:
                        Api.DeleteProgramPipeline(id);
                        break;
                    case GLObjectType.Query:
                        Api.DeleteQuery(id);
                        break;
                    case GLObjectType.Renderbuffer:
                        Api.DeleteRenderbuffer(id);
                        break;
                    case GLObjectType.Sampler:
                        Api.DeleteSampler(id);
                        break;
                    case GLObjectType.Texture:
                        Api.DeleteTexture(id);
                        break;
                    case GLObjectType.TransformFeedback:
                        Api.DeleteTransformFeedback(id);
                        break;
                    case GLObjectType.VertexArray:
                        Api.DeleteVertexArray(id);
                        break;
                    case GLObjectType.Shader:
                        Api.DeleteShader(id);
                        break;
                }
                PostDeleted();
            }
        }
    }
}