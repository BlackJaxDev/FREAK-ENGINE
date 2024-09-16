using Extensions;
using Silk.NET.OpenGL;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Models.Materials.Textures;

namespace XREngine.Rendering.OpenGL
{
    public unsafe partial class OpenGLRenderer : AbstractRenderer<GL>
    {
        public OpenGLRenderer(XRWindow window) : base(window) { }

        protected override AbstractRenderAPIObject CreateAPIRenderObject(GenericRenderObject renderObject)
            => renderObject switch
            {
                XRMaterial data => new GLMaterial(this, data),
                XRMeshRenderer data => new GLMeshRenderer(this, data),
                XRRenderProgram data => new GLRenderProgram(this, data),
                XRRenderProgramPipeline data => new GLRenderProgramPipeline(this, data),
                XRRenderQuery data => new GLRenderQuery(this, data),
                XRRenderBuffer data => new GLRenderBuffer(this, data),
                XRFrameBuffer data => new GLFrameBuffer(this, data),
                XRDataBuffer data => new GLDataBuffer(this, data),
                XRTexture2D data => new GLTexture2D(this, data),
                XRTexture3D data => new GLTexture3D(this, data),
                XRTextureCube data => new GLTextureCube(this, data),
                //XRTexture2DArray data => new GLTexture2DArray(this, data),
                XRTransformFeedback data => new GLTransformFeedback(this, data),
                XRSampler s => new GLSampler(this, s),
                XRShader s => new GLShader(this, s),
                _ => throw new InvalidOperationException($"Render object type {renderObject.GetType()} is not supported.")
            };

        protected override GL GetAPI()
            => GL.GetApi(Window.GLContext);

        protected override void Initialize()
        {

        }

        protected override void CleanUp()
        {

        }

        protected override void WindowRenderCallback(double delta)
        {

        }

        public override void AllowDepthWrite(bool allow)
        {
            Api.DepthMask(allow);
        }
        public override void BindFrameBuffer(EFramebufferTarget fboTarget, int bindingId)
        {
            GLEnum target = ToGLEnum(fboTarget);
            Api.BindFramebuffer(target, (uint)bindingId);
        }
        public override void Clear(bool color, bool depth, bool stencil)
        {
            uint mask = 0;
            if (color)
                mask |= (uint)GLEnum.ColorBufferBit;
            if (depth)
                mask |= (uint)GLEnum.DepthBufferBit;
            if (stencil)
                mask |= (uint)GLEnum.StencilBufferBit;
            Api.Clear(mask);
        }

        public override void ClearColor(ColorF4 color)
        {
            Api.ClearColor(color.R, color.G, color.B, color.A);
        }
        public override void ClearDepth(float depth)
        {
            Api.ClearDepth(depth);
        }
        public override void ClearStencil(int stencil)
        {
            Api.ClearStencil(stencil);
        }
        public override void StencilMask(uint v)
        {
            Api.StencilMask(v);
        }
        public override void DepthFunc(EComparison comparison)
        {
            var comp = comparison switch
            {
                EComparison.Never => GLEnum.Never,
                EComparison.Less => GLEnum.Less,
                EComparison.Equal => GLEnum.Equal,
                EComparison.Lequal => GLEnum.Lequal,
                EComparison.Greater => GLEnum.Greater,
                EComparison.Nequal => GLEnum.Notequal,
                EComparison.Gequal => GLEnum.Gequal,
                EComparison.Always => GLEnum.Always,
                _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null),
            };
            Api.DepthFunc(comp);
        }
        public override void EnableDepthTest(bool enable)
        {
            if (enable)
                Api.Enable(EnableCap.DepthTest);
            else
                Api.Disable(EnableCap.DepthTest);
        }
        public override float GetDepth(float x, float y)
        {
            float depth = 0.0f;
            Api.ReadPixels((int)x, (int)y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, &depth);
            return depth;
        }
        public override byte GetStencilIndex(float x, float y)
        {
            byte stencil = 0;
            Api.ReadPixels((int)x, (int)y, 1, 1, PixelFormat.StencilIndex, PixelType.UnsignedByte, &stencil);
            return stencil;
        }
        public override void SetReadBuffer(EDrawBuffersAttachment attachment)
        {
            var att = attachment switch
            {
                EDrawBuffersAttachment.ColorAttachment0 => GLEnum.ColorAttachment0,
                EDrawBuffersAttachment.ColorAttachment1 => GLEnum.ColorAttachment1,
                EDrawBuffersAttachment.ColorAttachment2 => GLEnum.ColorAttachment2,
                EDrawBuffersAttachment.ColorAttachment3 => GLEnum.ColorAttachment3,
                EDrawBuffersAttachment.ColorAttachment4 => GLEnum.ColorAttachment4,
                EDrawBuffersAttachment.ColorAttachment5 => GLEnum.ColorAttachment5,
                EDrawBuffersAttachment.ColorAttachment6 => GLEnum.ColorAttachment6,
                EDrawBuffersAttachment.ColorAttachment7 => GLEnum.ColorAttachment7,
                EDrawBuffersAttachment.ColorAttachment8 => GLEnum.ColorAttachment8,
                EDrawBuffersAttachment.ColorAttachment9 => GLEnum.ColorAttachment9,
                EDrawBuffersAttachment.ColorAttachment10 => GLEnum.ColorAttachment10,
                EDrawBuffersAttachment.ColorAttachment11 => GLEnum.ColorAttachment11,
                EDrawBuffersAttachment.ColorAttachment12 => GLEnum.ColorAttachment12,
                EDrawBuffersAttachment.ColorAttachment13 => GLEnum.ColorAttachment13,
                _ => throw new ArgumentOutOfRangeException(nameof(attachment), attachment, null),
            };
            Api.DrawBuffer(att);
        }

        public static GLEnum ToGLEnum(EBufferTarget target)
            => target switch
            {
                EBufferTarget.ArrayBuffer => GLEnum.ArrayBuffer,
                EBufferTarget.ElementArrayBuffer => GLEnum.ElementArrayBuffer,
                EBufferTarget.CopyReadBuffer => GLEnum.CopyReadBuffer,
                EBufferTarget.CopyWriteBuffer => GLEnum.CopyWriteBuffer,
                EBufferTarget.PixelPackBuffer => GLEnum.PixelPackBuffer,
                EBufferTarget.PixelUnpackBuffer => GLEnum.PixelUnpackBuffer,
                EBufferTarget.TransformFeedbackBuffer => GLEnum.TransformFeedbackBuffer,
                EBufferTarget.UniformBuffer => GLEnum.UniformBuffer,
                EBufferTarget.TextureBuffer => GLEnum.TextureBuffer,
                EBufferTarget.ParameterBuffer => GLEnum.ParameterBuffer,
                EBufferTarget.ShaderStorageBuffer => GLEnum.ShaderStorageBuffer,
                _ => throw new ArgumentOutOfRangeException(nameof(target), target, null),
            };

        protected override void SetRenderArea(BoundingRectangle region)
            => Api.Viewport(region.X, region.Y, (uint)region.Width, (uint)region.Height);

        public override void CropRenderArea(BoundingRectangle region)
            => Api.Scissor(region.X, region.Y, (uint)region.Width, (uint)region.Height);

        public void CheckFrameBufferErrors(GLFrameBuffer fbo)
        {
            var result = Api.CheckNamedFramebufferStatus(fbo.BindingId, FramebufferTarget.Framebuffer);
            if (result != GLEnum.FramebufferComplete)
            {
                Debug.LogWarning($"Framebuffer {fbo.BindingId} is not complete. Status: {result}");
                switch (result)
                {
                    case GLEnum.FramebufferIncompleteMissingAttachment:
                        {
                            fbo.Data.Targets?.ForEach(a =>
                            {
                                if (a.Target == null)
                                    Debug.LogWarning($"Framebuffer {fbo.BindingId} has missing attachment.");
                            });
                        }
                        break;
                    case GLEnum.FramebufferIncompleteAttachment:
                        {
                            //Collect all requested attachments
                            void TestAttachment((IFrameBufferAttachement Target, EFrameBufferAttachment Attachment, int MipLevel, int LayerIndex) a)
                            {
                                var attachment = Api.GetNamedFramebufferAttachmentParameter(fbo.BindingId, ToGLEnum(a.Attachment), GLEnum.FramebufferAttachmentObjectType);
                                if (attachment != (int)GLEnum.Texture)
                                    Debug.LogWarning($"Framebuffer {fbo.BindingId} has incomplete attachment.");
                            }
                            fbo.Data.Targets?.ForEach(TestAttachment);
                        }
                        break;
                }
            }
        }

        public void SetMipmapParameters(uint bindingId, int minLOD, int maxLOD, int largestMipmapLevel, int smallestAllowedMipmapLevel)
        {
            Api.TextureParameterI(bindingId, TextureParameterName.TextureBaseLevel, ref largestMipmapLevel);
            Api.TextureParameterI(bindingId, TextureParameterName.TextureMaxLevel, ref smallestAllowedMipmapLevel);
            Api.TextureParameterI(bindingId, TextureParameterName.TextureMinLod, ref minLOD);
            Api.TextureParameterI(bindingId, TextureParameterName.TextureMaxLod, ref maxLOD);
        }

        public void SetMipmapParameters(ETextureTarget target, int minLOD, int maxLOD, int largestMipmapLevel, int smallestAllowedMipmapLevel)
        {
            TextureTarget t = ToTextureTarget(target);
            Api.TexParameterI(t, TextureParameterName.TextureBaseLevel, ref largestMipmapLevel);
            Api.TexParameterI(t, TextureParameterName.TextureMaxLevel, ref smallestAllowedMipmapLevel);
            Api.TexParameterI(t, TextureParameterName.TextureMinLod, ref minLOD);
            Api.TexParameterI(t, TextureParameterName.TextureMaxLod, ref maxLOD);
        }

        public void ClearTexImage(uint bindingId, int level, ColorF4 color)
        {
            void* addr = color.Address;
            Api.ClearTexImage(bindingId, level, GLEnum.Rgba, GLEnum.Float, addr);
        }

        public void ClearTexImage(uint bindingId, int level, ColorF3 color)
        {
            void* addr = color.Address;
            Api.ClearTexImage(bindingId, level, GLEnum.Rgb, GLEnum.Float, addr);
        }

        public void ClearTexImage(uint bindingId, int level, RGBAPixel color)
        {
            void* addr = color.Address;
            Api.ClearTexImage(bindingId, level, GLEnum.Rgba, GLEnum.Byte, addr);
        }

        public static TextureTarget ToTextureTarget(ETextureTarget target)
            => target switch
            {
                ETextureTarget.Texture2D => TextureTarget.Texture2D,
                ETextureTarget.Texture3D => TextureTarget.Texture3D,
                ETextureTarget.TextureCubeMap => TextureTarget.TextureCubeMap,
                _ => TextureTarget.Texture2D
            };

        public override bool CalcDotLuminance(XRTexture2D texture, Vector3 luminance, out float dotLuminance, bool genMipmapsNow = true)
        {
            dotLuminance = 1.0f;
            var glTex = GenericToAPI<GLTexture2D>(texture);

            //Calculate average color value using 1x1 mipmap of scene
            if (genMipmapsNow)
                glTex.GenerateMipmaps();
            
            //Get the average color from the scene texture
            Vector3 rgb = Vector3.Zero;
            void* addr = &rgb;
            Api.GetTextureImage(glTex.BindingId, texture.SmallestMipmapLevel, ToGLEnum(EPixelFormat.Rgb), ToGLEnum(EPixelType.Float), (uint)sizeof(Vector3), addr);

            if (float.IsNaN(rgb.X) ||
                float.IsNaN(rgb.Y) ||
                float.IsNaN(rgb.Z))
                return false;

            //Calculate luminance factor off of the average color
            dotLuminance = rgb.Dot(luminance);
            return true;
        }

        public void DeleteObjects<T>(params T[] objs) where T : GLObjectBase
        {
            if (objs.Length == 0)
                return;

            uint[] bindingIds = new uint[objs.Length];
            bindingIds.Fill(GLObjectBase.InvalidBindingId);

            for (int i = 0; i < objs.Length; ++i)
            {
                var o = objs[i];
                if (!o.IsGenerated)
                    continue;

                o.PreDeleted();
                bindingIds[i] = o.BindingId;
            }

            bindingIds = bindingIds.Where(i => i != GLObjectBase.InvalidBindingId).ToArray();
            GLObjectType type = objs[0].Type;
            uint len = (uint)bindingIds.Length;
            switch (type)
            {
                case GLObjectType.Buffer:
                    Api.DeleteBuffers(len, bindingIds);
                    break;
                case GLObjectType.Framebuffer:
                    Api.DeleteFramebuffers(len, bindingIds);
                    break;
                case GLObjectType.Program:
                    foreach (var i in objs)
                        Api.DeleteProgram(i.BindingId);
                    break;
                case GLObjectType.ProgramPipeline:
                    Api.DeleteProgramPipelines(len, bindingIds);
                    break;
                case GLObjectType.Query:
                    Api.DeleteQueries(len, bindingIds);
                    break;
                case GLObjectType.Renderbuffer:
                    Api.DeleteRenderbuffers(len, bindingIds);
                    break;
                case GLObjectType.Sampler:
                    Api.DeleteSamplers(len, bindingIds);
                    break;
                case GLObjectType.Texture:
                    Api.DeleteTextures(len, bindingIds);
                    break;
                case GLObjectType.TransformFeedback:
                    Api.DeleteTransformFeedbacks(len, bindingIds);
                    break;
                case GLObjectType.VertexArray:
                    Api.DeleteVertexArrays(len, bindingIds);
                    break;
                case GLObjectType.Shader:
                    foreach (uint i in bindingIds)
                        Api.DeleteShader(i);
                    break;
            }

            foreach (var o in objs)
            {
                if (Array.IndexOf(bindingIds, o._bindingId) < 0)
                    continue;

                o._bindingId = null;
                o.PostDeleted();
            }
        }

        public uint[] CreateObjects(GLObjectType type, uint count)
        {
            uint[] ids = new uint[count];
            switch (type)
            {
                case GLObjectType.Buffer:
                    Api.CreateBuffers(count, ids);
                    break;
                case GLObjectType.Framebuffer:
                    Api.CreateFramebuffers(count, ids);
                    break;
                case GLObjectType.Program:
                    for (int i = 0; i < count; ++i)
                        ids[i] = Api.CreateProgram();
                    break;
                case GLObjectType.ProgramPipeline:
                    Api.CreateProgramPipelines(count, ids);
                    break;
                case GLObjectType.Query:
                    //throw new InvalidOperationException("Call CreateQueries instead.");
                    Api.GenQueries(count, ids);
                    break;
                case GLObjectType.Renderbuffer:
                    Api.CreateRenderbuffers(count, ids);
                    break;
                case GLObjectType.Sampler:
                    Api.CreateSamplers(count, ids);
                    break;
                case GLObjectType.Texture:
                    //throw new InvalidOperationException("Call CreateTextures instead.");
                    Api.GenTextures(count, ids);
                    break;
                case GLObjectType.TransformFeedback:
                    Api.CreateTransformFeedbacks(count, ids);
                    break;
                case GLObjectType.VertexArray:
                    Api.CreateVertexArrays(count, ids);
                    break;
                case GLObjectType.Shader:
                    //for (int i = 0; i < count; ++i)
                    //    ids[i] = Api.CreateShader(CurrentShaderMode);
                    break;
            }
            return ids;
        }

        //public T[] CreateObjects<T>(uint count) where T : GLObjectBase, new()
        //    => CreateObjects(TypeFor<T>(), count).Select(i => (T)Activator.CreateInstance(typeof(T), this, i)!).ToArray();

        private static GLObjectType TypeFor<T>() where T : GLObjectBase, new()
            => typeof(T) switch
            {
                Type t when typeof(GLDataBuffer).IsAssignableFrom(t)
                    => GLObjectType.Buffer,

                Type t when typeof(GLShader).IsAssignableFrom(t)
                    => GLObjectType.Shader,

                Type t when typeof(GLRenderProgram).IsAssignableFrom(t)
                    => GLObjectType.Program,

                Type t when typeof(GLMeshRenderer).IsAssignableFrom(t)
                    => GLObjectType.VertexArray,

                Type t when typeof(GLRenderQuery).IsAssignableFrom(t)
                    => GLObjectType.Query,

                Type t when typeof(GLRenderProgramPipeline).IsAssignableFrom(t)
                    => GLObjectType.ProgramPipeline,

                Type t when typeof(GLTransformFeedback).IsAssignableFrom(t)
                    => GLObjectType.TransformFeedback,

                Type t when typeof(GLSampler).IsAssignableFrom(t)
                    => GLObjectType.Sampler,

                Type t when typeof(IGLTexture).IsAssignableFrom(t)
                    => GLObjectType.Texture,

                Type t when typeof(GLRenderBuffer).IsAssignableFrom(t)
                    => GLObjectType.Renderbuffer,

                Type t when typeof(GLFrameBuffer).IsAssignableFrom(t)
                    => GLObjectType.Framebuffer,

                Type t when typeof(GLMaterial).IsAssignableFrom(t)
                    => GLObjectType.Material,
                _ => throw new InvalidOperationException($"Type {typeof(T)} is not a valid GLObjectBase type."),
            };

        public static GLEnum ToGLEnum(EPixelType pixelType)
            => pixelType switch
            {
                EPixelType.UnsignedByte => GLEnum.UnsignedByte,
                EPixelType.Byte => GLEnum.Byte,
                EPixelType.UnsignedShort => GLEnum.UnsignedShort,
                EPixelType.Short => GLEnum.Short,
                EPixelType.UnsignedInt => GLEnum.UnsignedInt,
                EPixelType.Int => GLEnum.Int,
                EPixelType.HalfFloat => GLEnum.HalfFloat,
                EPixelType.Float => GLEnum.Float,
                EPixelType.UnsignedByte332 => GLEnum.UnsignedByte332,
                EPixelType.UnsignedShort565 => GLEnum.UnsignedShort565,
                EPixelType.UnsignedShort4444 => GLEnum.UnsignedShort4444,
                EPixelType.UnsignedShort5551 => GLEnum.UnsignedShort5551,
                EPixelType.UnsignedInt8888 => GLEnum.UnsignedInt8888,
                EPixelType.UnsignedInt1010102 => GLEnum.UnsignedInt1010102,
                EPixelType.UnsignedInt248 => GLEnum.UnsignedInt248,
                EPixelType.UnsignedInt5999Rev => GLEnum.UnsignedInt5999Rev,
                EPixelType.Float32UnsignedInt248Rev => GLEnum.Float32UnsignedInt248Rev,
                _ => throw new ArgumentOutOfRangeException(nameof(pixelType), pixelType, null),
            };

        public static GLEnum ToGLEnum(EPixelFormat pixelFormat)
            => pixelFormat switch
            {
                EPixelFormat.Red => GLEnum.Red,
                EPixelFormat.Rg => GLEnum.RG,
                EPixelFormat.Rgb => GLEnum.Rgb,
                EPixelFormat.Bgr => GLEnum.Bgr,
                EPixelFormat.Rgba => GLEnum.Rgba,
                EPixelFormat.Bgra => GLEnum.Bgra,
                EPixelFormat.RedInteger => GLEnum.RedInteger,
                EPixelFormat.RgbInteger => GLEnum.RgbInteger,
                EPixelFormat.BgrInteger => GLEnum.BgrInteger,
                EPixelFormat.RgbaInteger => GLEnum.RgbaInteger,
                EPixelFormat.BgraInteger => GLEnum.BgraInteger,
                EPixelFormat.StencilIndex => GLEnum.StencilIndex,
                EPixelFormat.DepthComponent => GLEnum.DepthComponent,
                EPixelFormat.DepthStencil => GLEnum.DepthStencil,
                EPixelFormat.Green => GLEnum.Green,
                EPixelFormat.Blue => GLEnum.Blue,
                EPixelFormat.Alpha => GLEnum.Alpha,
                EPixelFormat.UnsignedShort => GLEnum.UnsignedShort,
                EPixelFormat.UnsignedInt => GLEnum.UnsignedInt,
                EPixelFormat.RgInteger => GLEnum.RGInteger,
                EPixelFormat.GreenInteger => GLEnum.GreenInteger,
                EPixelFormat.BlueInteger => GLEnum.BlueInteger,
                _ => throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, null),
            };

        public static GLEnum ToGLEnum(EPixelInternalFormat internalFormat)
            => internalFormat switch
            {
                EPixelInternalFormat.R8 => GLEnum.R8,
                EPixelInternalFormat.R8Snorm => GLEnum.R8SNorm,
                EPixelInternalFormat.R16 => GLEnum.R16,
                EPixelInternalFormat.R16Snorm => GLEnum.R16SNorm,
                EPixelInternalFormat.Rg8 => GLEnum.RG8,
                EPixelInternalFormat.Rg8Snorm => GLEnum.RG8SNorm,
                EPixelInternalFormat.Rg16 => GLEnum.RG16,
                EPixelInternalFormat.Rg16Snorm => GLEnum.RG16SNorm,
                EPixelInternalFormat.R3G3B2 => GLEnum.R3G3B2,
                EPixelInternalFormat.Rgb4 => GLEnum.Rgb4,
                EPixelInternalFormat.Rgb5 => GLEnum.Rgb5,
                EPixelInternalFormat.Rgb8 => GLEnum.Rgb8,
                EPixelInternalFormat.Rgb8Snorm => GLEnum.Rgb8SNorm,
                EPixelInternalFormat.Rgb10 => GLEnum.Rgb10,
                EPixelInternalFormat.Rgb12 => GLEnum.Rgb12,
                EPixelInternalFormat.Rgb16 => GLEnum.Rgb16,
                EPixelInternalFormat.Rgb16Snorm => GLEnum.Rgb16SNorm,
                EPixelInternalFormat.Rgba2 => GLEnum.Rgba2,
                EPixelInternalFormat.Rgba4 => GLEnum.Rgba4,
                EPixelInternalFormat.Rgb5A1 => GLEnum.Rgb5A1,
                EPixelInternalFormat.Rgba8 => GLEnum.Rgba8,
                EPixelInternalFormat.Rgba8Snorm => GLEnum.Rgba8SNorm,
                EPixelInternalFormat.Rgb10A2 => GLEnum.Rgb10A2,
                EPixelInternalFormat.Rgba12 => GLEnum.Rgba12,
                EPixelInternalFormat.Rgba16 => GLEnum.Rgba16,
                EPixelInternalFormat.Rgba16Snorm => GLEnum.Rgba16SNorm,
                EPixelInternalFormat.Srgb8 => GLEnum.Srgb8,
                EPixelInternalFormat.Srgb8Alpha8 => GLEnum.Srgb8Alpha8,
                EPixelInternalFormat.R16f => GLEnum.R16f,
                EPixelInternalFormat.Rg16f => GLEnum.RG16f,
                EPixelInternalFormat.Rgb16f => GLEnum.Rgb16f,
                EPixelInternalFormat.Rgba16f => GLEnum.Rgba16f,
                EPixelInternalFormat.R32f => GLEnum.R32f,
                EPixelInternalFormat.Rg32f => GLEnum.RG32f,
                EPixelInternalFormat.Rgb32f => GLEnum.Rgb32f,
                EPixelInternalFormat.Rgba32f => GLEnum.Rgba32f,
                EPixelInternalFormat.R11fG11fB10f => GLEnum.R11fG11fB10f,
                EPixelInternalFormat.Rgb9E5 => GLEnum.Rgb9E5,
                EPixelInternalFormat.Rg32i => GLEnum.RG32i,
                EPixelInternalFormat.Rg32ui => GLEnum.RG32ui,
                EPixelInternalFormat.Rgba32i => GLEnum.Rgba32i,
                EPixelInternalFormat.Rgba32ui => GLEnum.Rgba32ui,
                EPixelInternalFormat.One => GLEnum.One,
                EPixelInternalFormat.DepthComponent => GLEnum.DepthComponent,
                EPixelInternalFormat.Alpha => GLEnum.Alpha,
                EPixelInternalFormat.Rgb => GLEnum.Rgb,
                EPixelInternalFormat.Rgba => GLEnum.Rgba,
                _ => throw new ArgumentOutOfRangeException(nameof(internalFormat), internalFormat, null),
            };

        public static GLEnum ToGLEnum(ESizedInternalFormat sizedInternalFormat)
            => sizedInternalFormat switch
            {
                ESizedInternalFormat.Rgba8 => GLEnum.Rgba8,
                ESizedInternalFormat.Rgba16 => GLEnum.Rgba16,
                ESizedInternalFormat.R8 => GLEnum.R8,
                ESizedInternalFormat.R16 => GLEnum.R16,
                ESizedInternalFormat.Rg8 => GLEnum.RG8,
                ESizedInternalFormat.Rg16 => GLEnum.RG16,
                ESizedInternalFormat.R16f => GLEnum.R16f,
                ESizedInternalFormat.R32f => GLEnum.R32f,
                ESizedInternalFormat.Rg16f => GLEnum.RG16f,
                ESizedInternalFormat.Rg32f => GLEnum.RG32f,
                ESizedInternalFormat.R8i => GLEnum.R8i,
                ESizedInternalFormat.R8ui => GLEnum.R8ui,
                ESizedInternalFormat.R16i => GLEnum.R16i,
                ESizedInternalFormat.R16ui => GLEnum.R16ui,
                ESizedInternalFormat.R32i => GLEnum.R32i,
                ESizedInternalFormat.R32ui => GLEnum.R32ui,
                ESizedInternalFormat.Rg8i => GLEnum.RG8i,
                ESizedInternalFormat.Rg8ui => GLEnum.RG8ui,
                ESizedInternalFormat.Rg16i => GLEnum.RG16i,
                ESizedInternalFormat.Rg16ui => GLEnum.RG16ui,
                ESizedInternalFormat.Rg32i => GLEnum.RG32i,
                ESizedInternalFormat.Rg32ui => GLEnum.RG32ui,
                ESizedInternalFormat.Rgba32f => GLEnum.Rgba32f,
                ESizedInternalFormat.Rgba16f => GLEnum.Rgba16f,
                ESizedInternalFormat.Rgba32ui => GLEnum.Rgba32ui,
                ESizedInternalFormat.Rgba16ui => GLEnum.Rgba16ui,
                ESizedInternalFormat.Rgba8ui => GLEnum.Rgba8ui,
                ESizedInternalFormat.Rgba32i => GLEnum.Rgba32i,
                ESizedInternalFormat.Rgba16i => GLEnum.Rgba16i,
                ESizedInternalFormat.Rgba8i => GLEnum.Rgba8i,
                _ => GLEnum.Rgba8,
            };

        public static GLEnum ToGLEnum(ETextureTarget textureTarget)
            => textureTarget switch
            {
                ETextureTarget.Texture1D => GLEnum.Texture1D,
                ETextureTarget.Texture1DArray => GLEnum.Texture1DArray,
                ETextureTarget.ProxyTexture1D => GLEnum.ProxyTexture1D,
                ETextureTarget.ProxyTexture1DArray => GLEnum.ProxyTexture1DArray,

                ETextureTarget.Texture2D => GLEnum.Texture2D,
                ETextureTarget.Texture2DArray => GLEnum.Texture2DArray,
                ETextureTarget.Texture2DMultisample => GLEnum.Texture2DMultisample,
                ETextureTarget.Texture2DMultisampleArray => GLEnum.Texture2DMultisampleArray,
                ETextureTarget.ProxyTexture2D => GLEnum.ProxyTexture2D,
                ETextureTarget.ProxyTexture2DArray => GLEnum.ProxyTexture2DArray,
                ETextureTarget.ProxyTexture2DMultisample => GLEnum.ProxyTexture2DMultisample,
                ETextureTarget.ProxyTexture2DMultisampleArray => GLEnum.ProxyTexture2DMultisampleArray,

                ETextureTarget.Texture3D => GLEnum.Texture3D,
                ETextureTarget.ProxyTexture3D => GLEnum.ProxyTexture3D,

                ETextureTarget.TextureCubeMap => GLEnum.TextureCubeMap,
                ETextureTarget.ProxyTextureCubeMap => GLEnum.ProxyTextureCubeMap,
                ETextureTarget.ProxyTextureCubeMapArray => GLEnum.ProxyTextureCubeMapArray,

                ETextureTarget.TextureRectangle => GLEnum.TextureRectangle,
                ETextureTarget.ProxyTextureRectangle => GLEnum.ProxyTextureRectangle,

                ETextureTarget.TextureBuffer => GLEnum.TextureBuffer,

                _ => GLEnum.Texture2D
            };


        public static GLEnum ToGLEnum(EFrameBufferAttachment attachment)
            => attachment switch
            {
                EFrameBufferAttachment.BackLeft => GLEnum.BackLeft,
                EFrameBufferAttachment.BackRight => GLEnum.BackRight,
                EFrameBufferAttachment.FrontLeft => GLEnum.FrontLeft,
                EFrameBufferAttachment.FrontRight => GLEnum.FrontRight,
                EFrameBufferAttachment.Color => GLEnum.Color,
                EFrameBufferAttachment.Depth => GLEnum.Depth,
                EFrameBufferAttachment.Stencil => GLEnum.Stencil,
                EFrameBufferAttachment.ColorAttachment0 => GLEnum.ColorAttachment0,
                EFrameBufferAttachment.ColorAttachment1 => GLEnum.ColorAttachment1,
                EFrameBufferAttachment.ColorAttachment2 => GLEnum.ColorAttachment2,
                EFrameBufferAttachment.ColorAttachment3 => GLEnum.ColorAttachment3,
                EFrameBufferAttachment.ColorAttachment4 => GLEnum.ColorAttachment4,
                EFrameBufferAttachment.ColorAttachment5 => GLEnum.ColorAttachment5,
                EFrameBufferAttachment.ColorAttachment6 => GLEnum.ColorAttachment6,
                EFrameBufferAttachment.ColorAttachment7 => GLEnum.ColorAttachment7,
                EFrameBufferAttachment.ColorAttachment8 => GLEnum.ColorAttachment8,
                EFrameBufferAttachment.ColorAttachment9 => GLEnum.ColorAttachment9,
                EFrameBufferAttachment.ColorAttachment10 => GLEnum.ColorAttachment10,
                EFrameBufferAttachment.ColorAttachment11 => GLEnum.ColorAttachment11,
                EFrameBufferAttachment.ColorAttachment12 => GLEnum.ColorAttachment12,
                EFrameBufferAttachment.ColorAttachment13 => GLEnum.ColorAttachment13,
                EFrameBufferAttachment.ColorAttachment14 => GLEnum.ColorAttachment14,
                EFrameBufferAttachment.ColorAttachment15 => GLEnum.ColorAttachment15,
                EFrameBufferAttachment.DepthAttachment => GLEnum.DepthAttachment,
                EFrameBufferAttachment.StencilAttachment => GLEnum.StencilAttachment,
                EFrameBufferAttachment.DepthStencilAttachment => GLEnum.DepthStencilAttachment,
                _ => throw new NotImplementedException()
            };

        public static GLEnum ToGLEnum(EFramebufferTarget target)
            => target switch
            {
                EFramebufferTarget.Framebuffer => GLEnum.Framebuffer,
                EFramebufferTarget.ReadFramebuffer => GLEnum.ReadFramebuffer,
                EFramebufferTarget.DrawFramebuffer => GLEnum.DrawFramebuffer,
                _ => throw new NotImplementedException()
            };
    }
}