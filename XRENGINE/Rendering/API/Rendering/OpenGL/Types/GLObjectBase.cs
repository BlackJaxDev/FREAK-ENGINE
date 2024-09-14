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

            public override bool IsGenerated => _bindingId.HasValue && _bindingId != InvalidBindingId;

            internal uint? _bindingId;

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

            public override void Generate()
            {
                Debug.Out($"Generating OpenGL object {Type}");
                PreGenerated();
                _bindingId = CreateObject();
                PostGenerated();
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
                        {
                            //return InvalidBindingId;
                            Generate();
                        }
                        return _bindingId!.Value;
                    }
                    catch
                    {
                        throw new Exception($"Failed to generate object of type {Type}.");
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