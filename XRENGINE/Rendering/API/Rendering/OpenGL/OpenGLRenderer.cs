using Extensions;
using ImageMagick;
using Silk.NET.OpenGL;
using Silk.NET.OpenGLES.Extensions.EXT;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Models.Materials.Textures;
using PixelFormat = Silk.NET.OpenGL.PixelFormat;

namespace XREngine.Rendering.OpenGL
{
    public partial class OpenGLRenderer : AbstractRenderer<GL>
    {
        public Silk.NET.OpenGLES.GL ESApi { get; }
        public ExtMemoryObject? EXTMemoryObject { get; }
        public ExtSemaphore? EXTSemaphore { get; }
        public ExtMemoryObjectWin32? EXTMemoryObjectWin32 { get; }
        public ExtSemaphoreWin32? EXTSemaphoreWin32 { get; }
        public ExtSemaphoreFd? EXTSemaphoreFd { get; }
        public ExtMemoryObjectFd? EXTMemoryObjectFd { get; }
        
        private static string? _version = null;
        public string? Version
        {
            get
            {
                unsafe
                {
                    _version ??= new((sbyte*)Api.GetString(StringName.Version));
                }
                return _version;
            }
        }
        public OpenGLRenderer(XRWindow window, bool shouldLinkWindow = true) : base(window, shouldLinkWindow)
        {
            var api = Api;
            ESApi = Silk.NET.OpenGLES.GL.GetApi(Window.GLContext);
            EXTMemoryObject = ESApi.TryGetExtension<ExtMemoryObject>(out var ext) ? ext : null;
            EXTSemaphore = ESApi.TryGetExtension<ExtSemaphore>(out var ext2) ? ext2 : null;
            EXTMemoryObjectWin32 = ESApi.TryGetExtension<ExtMemoryObjectWin32>(out var ext3) ? ext3 : null;
            EXTSemaphoreWin32 = ESApi.TryGetExtension<ExtSemaphoreWin32>(out var ext4) ? ext4 : null;
            EXTMemoryObjectFd = ESApi.TryGetExtension<ExtMemoryObjectFd>(out var ext5) ? ext5 : null;
            EXTSemaphoreFd = ESApi.TryGetExtension<ExtSemaphoreFd>(out var ext6) ? ext6 : null;
        }

        private static void InitGL(GL api)
        {
            string version;
            unsafe
            {
                version = new((sbyte*)api.GetString(StringName.Version));
                string vendor = new((sbyte*)api.GetString(StringName.Vendor));
                string renderer = new((sbyte*)api.GetString(StringName.Renderer));
                string shadingLanguageVersion = new((sbyte*)api.GetString(StringName.ShadingLanguageVersion));
                Debug.Out($"OpenGL Version: {version}");
                Debug.Out($"OpenGL Vendor: {vendor}");
                Debug.Out($"OpenGL Renderer: {renderer}");
                Debug.Out($"OpenGL Shading Language Version: {shadingLanguageVersion}");
            }

            GLRenderProgram.ReadBinaryShaderCache(version);

            api.Enable(EnableCap.Multisample);
            api.Enable(EnableCap.TextureCubeMapSeamless);
            api.FrontFace(FrontFaceDirection.Ccw);

            api.ClipControl(GLEnum.LowerLeft, GLEnum.NegativeOneToOne);

            //Fix gamma manually inside of the post process shader
            //api.Enable(EnableCap.FramebufferSrgb);

            api.PixelStore(PixelStoreParameter.PackAlignment, 1);
            api.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            api.UseProgram(0);

            SetupDebug(api);
        }

        private unsafe static void SetupDebug(GL api)
        {
            api.Enable(EnableCap.DebugOutput);
            api.Enable(EnableCap.DebugOutputSynchronous);
            api.DebugMessageCallback(DebugCallback, null);
            uint[] ids = [];
            fixed (uint* ptr = ids)
                api.DebugMessageControl(GLEnum.DontCare, GLEnum.DontCare, GLEnum.DontCare, 0, ptr, true);
        }

        private static int[] _ignoredMessageIds =
        [
            131185, //buffer will use video memory
            131204, //no base level, no mipmaps, etc
            131169, //allocated memory for render buffer
            131154, //pixel transfer is synchronized with 3d rendering
            //131216,
            131218,
            131076,
            //1282,
            //0,
            //9,
        ];
        private static int[] _printMessageIds =
        [
            //1280, //Invalid texture format and type combination
            //1281, //Invalid texture format
            //1282,
        ];

        public unsafe static void DebugCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userParam)
        {
            if (_ignoredMessageIds.IndexOf(id) >= 0)
                return;

            string messageStr = new((sbyte*)message);
            Debug.LogWarning($"OPENGL {FormatSeverity(severity)} #{id} | {FormatSource(source)} {FormatType(type)} | {messageStr}", 1, 5);
        }

        private static string FormatSeverity(GLEnum severity)
            => severity switch
            {
                GLEnum.DebugSeverityHigh => "High",
                GLEnum.DebugSeverityMedium => "Medium",
                GLEnum.DebugSeverityLow => "Low",
                GLEnum.DebugSeverityNotification => "Notification",
                _ => severity.ToString(),
            };

        private static string FormatType(GLEnum type)
            => type switch
            {
                GLEnum.DebugTypeError => "Error",
                GLEnum.DebugTypeDeprecatedBehavior => "Deprecated Behavior",
                GLEnum.DebugTypeUndefinedBehavior => "Undefined Behavior",
                GLEnum.DebugTypePortability => "Portability",
                GLEnum.DebugTypePerformance => "Performance",
                GLEnum.DebugTypeOther => "Other",
                GLEnum.DebugTypeMarker => "Marker",
                GLEnum.DebugTypePushGroup => "Push Group",
                GLEnum.DebugTypePopGroup => "Pop Group",
                _ => type.ToString(),
            };

        private static string FormatSource(GLEnum source)
            => source switch
            {
                GLEnum.DebugSourceApi => "API",
                GLEnum.DebugSourceWindowSystem => "Window System",
                GLEnum.DebugSourceShaderCompiler => "Shader Compiler",
                GLEnum.DebugSourceThirdParty => "Third Party",
                GLEnum.DebugSourceApplication => "Application",
                GLEnum.DebugSourceOther => "Other",
                _ => source.ToString(),
            };

        public static void CheckError(string? name)
        {
            //if (Current is not OpenGLRenderer renderer)
            //    return;

            //var error = renderer.Api.GetError();
            //if (error != GLEnum.NoError)
            //    Debug.LogWarning(name is null ? error.ToString() : $"{name}: {error}", 1);
        }

        protected override AbstractRenderAPIObject CreateAPIRenderObject(GenericRenderObject renderObject)
            => renderObject switch
            {
                //Meshes
                XRMaterial data => new GLMaterial(this, data),
                XRMeshRenderer data => new GLMeshRenderer(this, data),
                XRRenderProgramPipeline data => new GLRenderProgramPipeline(this, data),
                XRRenderProgram data => new GLRenderProgram(this, data),
                XRDataBuffer data => new GLDataBuffer(this, data),
                XRSampler s => new GLSampler(this, s),
                XRShader s => new GLShader(this, s),

                //FBOs
                XRRenderBuffer data => new GLRenderBuffer(this, data),
                XRFrameBuffer data => new GLFrameBuffer(this, data),

                //TODO: Implement these

                //Texture 1D
                //XRTexture1D data => new GLTexture1D(this, data),
                //XRTexture1DArray data => new GLTexture1DArray(this, data),
                XRTexture1DView data => new GLTextureView(this, data),
                XRTexture1DArrayView data => new GLTextureView(this, data),

                //Texture 2D
                XRTexture2D data => new GLTexture2D(this, data),
                //XRTexture2DArray data => new GLTexture2DArray(this, data),
                XRTexture2DView data => new GLTextureView(this, data),
                XRTexture2DArrayView data => new GLTextureView(this, data),

                //Texture 3D
                XRTexture3D data => new GLTexture3D(this, data),
                //XRTexture3DArray data => new GLTexture3DArray(this, data),
                XRTexture3DView data => new GLTextureView(this, data),

                //Texture Cube
                XRTextureCube data => new GLTextureCube(this, data),
                //XRTextureCubeArray data => new GLTextureCubeArray(this, data),
                XRTextureCubeView data => new GLTextureView(this, data),

                //Texture Buffer
                //XRTextureBuffer data => new GLTextureBuffer(this, data),
                //XRTextureBufferArray data => new GLTextureBufferArray(this, data),
                XRTextureBufferView data => new GLTextureView(this, data),

                //Feedback
                XRRenderQuery data => new GLRenderQuery(this, data),
                XRTransformFeedback data => new GLTransformFeedback(this, data),

                _ => throw new InvalidOperationException($"Render object type {renderObject.GetType()} is not supported.")
            };

        protected override GL GetAPI()
        {
            var api = GL.GetApi(Window.GLContext);
            InitGL(api);
            return api;
        }

        public override void Initialize()
        {

        }

        public override void CleanUp()
        {

        }

        protected override void WindowRenderCallback(double delta)
        {

        }

        public override void DispatchCompute(XRRenderProgram program, int numGroupsX, int numGroupsY, int numGroupsZ)
        {
            GLRenderProgram? glProgram = GenericToAPI<GLRenderProgram>(program);
            if (glProgram is null)
                return;

            Api.UseProgram(glProgram.BindingId);
            Api.DispatchCompute((uint)numGroupsX, (uint)numGroupsY, (uint)numGroupsZ);
        }

        public override void AllowDepthWrite(bool allow)
        {
            Api.DepthMask(allow);
        }
        public override void BindFrameBuffer(EFramebufferTarget fboTarget, XRFrameBuffer? fbo)
        {
            Api.BindFramebuffer(GLObjectBase.ToGLEnum(fboTarget), GenericToAPI<GLFrameBuffer>(fbo)?.BindingId ?? 0u);
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
        public override unsafe float GetDepth(float x, float y)
        {
            float depth = 0.0f;
            Api.ReadPixels((int)x, (int)y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, &depth);
            return depth;
        }
        public override unsafe byte GetStencilIndex(float x, float y)
        {
            byte stencil = 0;
            Api.ReadPixels((int)x, (int)y, 1, 1, PixelFormat.StencilIndex, PixelType.UnsignedByte, &stencil);
            return stencil;
        }
        public override void SetReadBuffer(EReadBufferMode mode)
        {
            Api.ReadBuffer(ToGLEnum(mode));
        }
        public override void SetReadBuffer(XRFrameBuffer? fbo, EReadBufferMode mode)
        {
            Api.NamedFramebufferReadBuffer(GenericToAPI<GLFrameBuffer>(fbo)?.BindingId ?? 0, ToGLEnum(mode));
        }

        private static GLEnum ToGLEnum(EReadBufferMode mode)
        {
            return mode switch
            {
                EReadBufferMode.None => GLEnum.None,
                EReadBufferMode.Front => GLEnum.Front,
                EReadBufferMode.Back => GLEnum.Back,
                EReadBufferMode.Left => GLEnum.Left,
                EReadBufferMode.Right => GLEnum.Right,
                EReadBufferMode.FrontLeft => GLEnum.FrontLeft,
                EReadBufferMode.FrontRight => GLEnum.FrontRight,
                EReadBufferMode.BackLeft => GLEnum.BackLeft,
                EReadBufferMode.BackRight => GLEnum.BackRight,
                EReadBufferMode.ColorAttachment0 => GLEnum.ColorAttachment0,
                EReadBufferMode.ColorAttachment1 => GLEnum.ColorAttachment1,
                EReadBufferMode.ColorAttachment2 => GLEnum.ColorAttachment2,
                EReadBufferMode.ColorAttachment3 => GLEnum.ColorAttachment3,
                EReadBufferMode.ColorAttachment4 => GLEnum.ColorAttachment4,
                EReadBufferMode.ColorAttachment5 => GLEnum.ColorAttachment5,
                EReadBufferMode.ColorAttachment6 => GLEnum.ColorAttachment6,
                EReadBufferMode.ColorAttachment7 => GLEnum.ColorAttachment7,
                EReadBufferMode.ColorAttachment8 => GLEnum.ColorAttachment8,
                EReadBufferMode.ColorAttachment9 => GLEnum.ColorAttachment9,
                EReadBufferMode.ColorAttachment10 => GLEnum.ColorAttachment10,
                EReadBufferMode.ColorAttachment11 => GLEnum.ColorAttachment11,
                EReadBufferMode.ColorAttachment12 => GLEnum.ColorAttachment12,
                EReadBufferMode.ColorAttachment13 => GLEnum.ColorAttachment13,
                EReadBufferMode.ColorAttachment14 => GLEnum.ColorAttachment14,
                EReadBufferMode.ColorAttachment15 => GLEnum.ColorAttachment15,
                EReadBufferMode.ColorAttachment16 => GLEnum.ColorAttachment16,
                EReadBufferMode.ColorAttachment17 => GLEnum.ColorAttachment17,
                EReadBufferMode.ColorAttachment18 => GLEnum.ColorAttachment18,
                EReadBufferMode.ColorAttachment19 => GLEnum.ColorAttachment19,
                EReadBufferMode.ColorAttachment20 => GLEnum.ColorAttachment20,
                EReadBufferMode.ColorAttachment21 => GLEnum.ColorAttachment21,
                EReadBufferMode.ColorAttachment22 => GLEnum.ColorAttachment22,
                EReadBufferMode.ColorAttachment23 => GLEnum.ColorAttachment23,
                EReadBufferMode.ColorAttachment24 => GLEnum.ColorAttachment24,
                EReadBufferMode.ColorAttachment25 => GLEnum.ColorAttachment25,
                EReadBufferMode.ColorAttachment26 => GLEnum.ColorAttachment26,
                EReadBufferMode.ColorAttachment27 => GLEnum.ColorAttachment27,
                EReadBufferMode.ColorAttachment28 => GLEnum.ColorAttachment28,
                EReadBufferMode.ColorAttachment29 => GLEnum.ColorAttachment29,
                EReadBufferMode.ColorAttachment30 => GLEnum.ColorAttachment30,
                EReadBufferMode.ColorAttachment31 => GLEnum.ColorAttachment31,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
            };
        }

        public override void SetRenderArea(BoundingRectangle region)
            => Api.Viewport(region.X, region.Y, (uint)region.Width, (uint)region.Height);

        public override void CropRenderArea(BoundingRectangle region)
            => Api.Scissor(region.X, region.Y, (uint)region.Width, (uint)region.Height);

        public override void SetCroppingEnabled(bool enabled)
        {
            if (enabled)
                Api.Enable(EnableCap.ScissorTest);
            else
                Api.Disable(EnableCap.ScissorTest);
        }

        public void CheckFrameBufferErrors(GLFrameBuffer fbo)
        {
            var result = Api.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            string debug = GetFBODebugInfo(fbo, Environment.NewLine);
            string name = fbo.GetDescribingName();
            if (result != GLEnum.FramebufferComplete)
                Debug.LogWarning($"FBO {name} is not complete. Status: {result}{debug}", 0, 20);
            //else
            //    Debug.Out($"FBO {name} is complete.{debug}");
        }

        private static string GetFBODebugInfo(GLFrameBuffer fbo, string splitter)
        {
            string debug = string.Empty;
            if (fbo.Data.Targets is null || fbo.Data.Targets.Length == 0)
            {
                debug += $"{splitter}This FBO has no targets.";
                return debug;
            }

            foreach (var (Target, Attachment, MipLevel, LayerIndex) in fbo.Data.Targets)
            {
                GenericRenderObject? gro = Target as GenericRenderObject;
                bool targetExists = gro is not null;
                string texName = targetExists ? gro!.GetDescribingName() : "<null>";
                debug += $"{splitter}{Attachment}: {texName} Mip{MipLevel}";
                if (LayerIndex >= 0)
                    debug += $" Layer{LayerIndex}";
                if (targetExists)
                    debug += $" / {GetTargetDebugInfo(gro!)}";
            }
            return debug;
        }

        private static string GetTargetDebugInfo(GenericRenderObject gro)
        {
            string debug = string.Empty;
            switch (gro)
            {
                case XRTexture2DView t2dv:
                    debug += $"{t2dv.ViewedTexture.Width}x{t2dv.ViewedTexture.Height} | Viewing {t2dv.ViewedTexture.Name} | internal:{t2dv.InternalFormat}{FormatMipLevels(t2dv.ViewedTexture)}";
                    break;
                case XRTexture2D t2d:
                    debug += $"{t2d.Width}x{t2d.Height}{FormatMipLevels(t2d)}";
                    break;
                case XRRenderBuffer rb:
                    debug += $"{rb.Width}x{rb.Height} | {rb.Type}";
                    break;
                case XRTextureCube tc:
                    debug += $"{tc.MaxDimension}x{tc.MaxDimension}x{tc.MaxDimension}{FormatMipLevels(tc)}";
                    break;
            }
            return debug;
        }

        private static string FormatMipLevels(XRTextureCube tc)
        {
            switch (tc.Mipmaps.Length)
            {
                case 0:
                    return " | No mipmaps";
                case 1:
                    return $" | {FormatMipmap(0, tc.Mipmaps)}";
                default:
                    string mipmaps = $" | {tc.Mipmaps.Length} mipmaps";
                    for (int i = 0; i < tc.Mipmaps.Length; i++)
                        mipmaps += $"{Environment.NewLine}{FormatMipmap(i, tc.Mipmaps)}";
                    return mipmaps;
            }
        }

        private static string FormatMipLevels(XRTexture2D t2d)
        {
            switch (t2d.Mipmaps.Length)
            {
                case 0:
                    return " | No mipmaps";
                case 1:
                    return $" | {FormatMipmap(0, t2d.Mipmaps)}";
                default:
                    string mipmaps = $" | {t2d.Mipmaps.Length} mipmaps";
                    for (int i = 0; i < t2d.Mipmaps.Length; i++)
                        mipmaps += $"{Environment.NewLine}{FormatMipmap(i, t2d.Mipmaps)}";
                    return mipmaps;
            }
        }

        private static string FormatMipmap(int i, CubeMipmap[] mipmaps)
        {
            if (i >= mipmaps.Length)
                return string.Empty;

            CubeMipmap m = mipmaps[i];
            //Format all sides
            string sides = string.Empty;
            for (int j = 0; j < m.Sides.Length; j++)
            {
                Mipmap2D side = m.Sides[j];
                sides += $"{side.Width}x{side.Height} | internal:{side.InternalFormat} | {side.PixelFormat}/{side.PixelType}";
                if (j < m.Sides.Length - 1)
                    sides += Environment.NewLine;
            }
            return $"Mip{i} | {sides}";
        }

        private static string FormatMipmap(int i, Mipmap2D[] mipmaps)
        {
            if (i >= mipmaps.Length)
                return string.Empty;

            Mipmap2D m = mipmaps[i];
            return $"Mip{i} | {m.Width}x{m.Height} | internal:{m.InternalFormat} | {m.PixelFormat}/{m.PixelType}";
        }

        //public void SetMipmapParameters(uint bindingId, int minLOD, int maxLOD, int largestMipmapLevel, int smallestAllowedMipmapLevel)
        //{
        //    Api.TextureParameterI(bindingId, TextureParameterName.TextureBaseLevel, ref largestMipmapLevel);
        //    Api.TextureParameterI(bindingId, TextureParameterName.TextureMaxLevel, ref smallestAllowedMipmapLevel);
        //    Api.TextureParameterI(bindingId, TextureParameterName.TextureMinLod, ref minLOD);
        //    Api.TextureParameterI(bindingId, TextureParameterName.TextureMaxLod, ref maxLOD);
        //}

        //public void SetMipmapParameters(ETextureTarget target, int minLOD, int maxLOD, int largestMipmapLevel, int smallestAllowedMipmapLevel)
        //{
        //    TextureTarget t = ToTextureTarget(target);
        //    Api.TexParameterI(t, TextureParameterName.TextureBaseLevel, ref largestMipmapLevel);
        //    Api.TexParameterI(t, TextureParameterName.TextureMaxLevel, ref smallestAllowedMipmapLevel);
        //    Api.TexParameterI(t, TextureParameterName.TextureMinLod, ref minLOD);
        //    Api.TexParameterI(t, TextureParameterName.TextureMaxLod, ref maxLOD);
        //}

        public unsafe void ClearTexImage(uint bindingId, int level, ColorF4 color)
        {
            void* addr = color.Address;
            Api.ClearTexImage(bindingId, level, GLEnum.Rgba, GLEnum.Float, addr);
        }

        public unsafe void ClearTexImage(uint bindingId, int level, ColorF3 color)
        {
            void* addr = color.Address;
            Api.ClearTexImage(bindingId, level, GLEnum.Rgb, GLEnum.Float, addr);
        }

        public unsafe void ClearTexImage(uint bindingId, int level, RGBAPixel color)
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

        public override unsafe bool CalcDotLuminance(XRTexture2D texture, Vector3 luminance, out float dotLuminance, bool genMipmapsNow = true)
        {
            dotLuminance = 1.0f;
            var glTex = GenericToAPI<GLTexture2D>(texture);
            if (glTex is null)
                return false;

            //Calculate average color value using 1x1 mipmap of scene
            if (genMipmapsNow)
                glTex.GenerateMipmaps();
            
            //Get the average color from the scene texture
            Vector4 rgb = Vector4.Zero;
            void* addr = &rgb;
            Api.GetTextureImage(glTex.BindingId, texture.SmallestMipmapLevel, GLObjectBase.ToGLEnum(EPixelFormat.Rgba), GLObjectBase.ToGLEnum(EPixelType.Float), (uint)sizeof(Vector4), addr);

            if (float.IsNaN(rgb.X) ||
                float.IsNaN(rgb.Y) ||
                float.IsNaN(rgb.Z))
                return false;

            //Calculate luminance factor off of the average color
            dotLuminance = rgb.XYZ().Dot(luminance);
            return true;
        }

        public override void GetScreenshotAsync(BoundingRectangle region, bool withTransparency, Action<MagickImage> imageCallback)
        {
            //TODO: render to an FBO with the desired render size and capture from that, instead of using the window size.

            //TODO: multi-glcontext readback.
            //This method is async on the CPU, but still executes synchronously on the GPU.
            //https://developer.download.nvidia.com/GTC/PDF/GTC2012/PresentationPDF/S0356-GTC2012-Texture-Transfers.pdf

            uint w = (uint)region.Width;
            uint h = (uint)region.Height;
            EPixelFormat format = withTransparency ? EPixelFormat.Bgra : EPixelFormat.Bgr;
            EPixelType pixelType = EPixelType.UnsignedByte;
            var data = XRTexture.AllocateBytes(w, h, format, pixelType);

            Api.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            Api.ReadBuffer(ReadBufferMode.Front);

            nuint size = (uint)data.Length;
            uint pbo = ReadToPBO(region, w, h, format, pixelType, size, out IntPtr sync);
            void FenceCheck()
            {
                if (GetData(size, data, sync, pbo))
                {
                    Api.DeleteSync(sync);
                    Api.DeleteBuffer(pbo);
                    var image = XRTexture.NewImage(w, h, format, pixelType, data);
                    image.Flip();
                    Task.Run(() => imageCallback(image));
                }
                else
                {
                    Engine.EnqueueMainThreadTask(FenceCheck);
                }
            }
            Engine.EnqueueMainThreadTask(FenceCheck);
        }

        private unsafe uint ReadToPBO(BoundingRectangle region, uint w, uint h, EPixelFormat format, EPixelType type, nuint size, out IntPtr sync)
        {
            uint pbo = Api.GenBuffer();
            Api.BindBuffer(BufferTargetARB.PixelPackBuffer, pbo);
            Api.BufferData(GLEnum.PixelPackBuffer, size, null, GLEnum.StreamRead);
            Api.ReadPixels(region.X, region.Y, w, h, GLObjectBase.ToGLEnum(format), GLObjectBase.ToGLEnum(type), null);
            sync = Api.FenceSync(GLEnum.SyncGpuCommandsComplete, 0u);
            Api.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);
            return pbo;
        }

        private unsafe bool GetData(nuint size, byte[] data, IntPtr sync, uint pbo)
        {
            var result = Api.ClientWaitSync(sync, 0u, 0u);
            if (!(result == GLEnum.AlreadySignaled || result == GLEnum.ConditionSatisfied))
                return false;

            Api.BindBuffer(BufferTargetARB.PixelPackBuffer, pbo);
            fixed (byte* ptr = data)
            {
                Api.GetBufferSubData(GLEnum.PixelPackBuffer, IntPtr.Zero, size, ptr);
            }
            Api.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);

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

        public uint CreateMemoryObject()
            => EXTMemoryObject?.CreateMemoryObject() ?? 0;

        public uint CreateSemaphore()
            => EXTSemaphore?.GenSemaphore() ?? 0;

        public IntPtr GetMemoryObjectHandle(uint memoryObject)
        {
            if (EXTMemoryObject is null)
                return IntPtr.Zero;
            EXTMemoryObject.GetMemoryObjectParameter(memoryObject, EXT.HandleTypeOpaqueWin32Ext, out int handle);
            return (IntPtr)handle;
        }

        public IntPtr GetSemaphoreHandle(uint semaphore)
        {
            if (EXTSemaphore is null)
                return IntPtr.Zero;
            EXTSemaphore.GetSemaphoreParameter(semaphore, EXT.HandleTypeOpaqueWin32Ext, out ulong handle);
            return (IntPtr)handle;
        }

        public unsafe void SetMemoryObjectHandle(uint memoryObject, void* memoryObjectHandle)
            => EXTMemoryObjectWin32?.ImportMemoryWin32Handle(memoryObject, 0, EXT.HandleTypeOpaqueWin32Ext, memoryObjectHandle);

        public unsafe void SetSemaphoreHandle(uint semaphore, void* semaphoreHandle)
            => EXTSemaphoreWin32?.ImportSemaphoreWin32Handle(semaphore, EXT.HandleTypeOpaqueWin32Ext, semaphoreHandle);

        public override void Blit(
            XRFrameBuffer? inFBO,
            XRFrameBuffer? outFBO,
            int inX, int inY, uint inW, uint inH,
            int outX, int outY, uint outW, uint outH,
            EReadBufferMode readBufferMode,
            bool colorBit, bool depthBit, bool stencilBit,
            bool linearFilter)
        {
            ClearBufferMask mask = 0;
            if (colorBit)
                mask |= ClearBufferMask.ColorBufferBit;
            if (depthBit)
                mask |= ClearBufferMask.DepthBufferBit;
            if (stencilBit)
                mask |= ClearBufferMask.StencilBufferBit;

            var glIn = GenericToAPI<GLFrameBuffer>(inFBO);
            var glOut = GenericToAPI<GLFrameBuffer>(outFBO);
            var inID = glIn?.BindingId ?? 0u;
            var outID = glOut?.BindingId ?? 0u;

            Api.NamedFramebufferReadBuffer(inID, ToGLEnum(readBufferMode));
            Api.BlitNamedFramebuffer(
                inID,
                outID,
                inX,
                inY,
                inX + (int)inW,
                inY + (int)inH,
                outX,
                outY,
                outX + (int)outW,
                outY + (int)outH,
                mask,
                linearFilter ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest);
        }
    }
}