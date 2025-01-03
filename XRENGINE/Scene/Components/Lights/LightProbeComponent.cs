﻿using MIConvexHull;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;
using XREngine.Rendering;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene.Transforms;
using XREngine.Timers;

namespace XREngine.Components.Lights
{
    public class LightProbeComponent : SceneCaptureComponent, IRenderable, IVertex
    {
        /// <summary>
        /// Position for delaunay triangulation
        /// </summary>
        double[] IVertex.Position => [Transform.WorldTranslation.X, Transform.WorldTranslation.Y, Transform.WorldTranslation.Z];

        public LightProbeComponent() : base()
        {
            _realTimeTimer = new GameTimer(this);
            RenderedObjects = 
            [
                VisualRenderInfo = RenderInfo3D.New(this, _visualRC = new RenderCommandMesh3D((int)EDefaultRenderPass.OpaqueForward)),
            ];
        }

        private readonly RenderCommandMesh3D _visualRC;

        public bool PreviewEnabled 
        {
            get => VisualRenderInfo.IsVisible;
            set => VisualRenderInfo.IsVisible = value;
        }

        public RenderInfo3D VisualRenderInfo { get; }
        public RenderInfo[] RenderedObjects { get; }

        private bool _generateIrradiance = false;
        private bool _generatePrefilter = false;

        private GameTimer _realTimeTimer;

        private bool _realTime = false;
        /// <summary>
        /// If true, the light probe will update in real time.
        /// </summary>
        public bool RealTimeCapture
        {
            get => _realTime;
            set => SetField(ref _realTime, value);
        }

        private TimeSpan? _realTimeUpdateInterval = TimeSpan.FromMilliseconds(1000.0f);
        public TimeSpan? RealTimeCaptureUpdateInterval 
        {
            get => _realTimeUpdateInterval;
            set => SetField(ref _realTimeUpdateInterval, value);
        }

        private XRCubeFrameBuffer? _irradianceFBO;
        private XRCubeFrameBuffer? _prefilterFBO;
        private DateTime _lastUpdateTime = DateTime.Now;
        private XRTextureCube? _irradianceTexture;
        private XRTextureCube? _prefilterTexture;
        private XRMeshRenderer? _irradianceSphere;

        public XRTextureCube? IrradianceTexture
        {
            get => _irradianceTexture;
            private set => SetField(ref _irradianceTexture, value);
        }
        public XRTextureCube? PrefilterTex
        {
            get => _prefilterTexture;
            private set => SetField(ref _prefilterTexture, value);
        }
        public XRMeshRenderer? PreviewSphere
        {
            get => _irradianceSphere;
            private set => SetField(ref _irradianceSphere, value);
        }

        public enum ERenderPreview
        {
            Environment,
            Irradiance,
            Prefilter,
        }

        private ERenderPreview _previewDisplay = ERenderPreview.Environment;
        public ERenderPreview PreviewDisplay
        {
            get => _previewDisplay;
            set => SetField(ref _previewDisplay, value);
        }

        public XRTexture? GetPreviewTexture()
            => PreviewDisplay switch
            {
                ERenderPreview.Irradiance => IrradianceTexture,
                ERenderPreview.Prefilter => PrefilterTex,
                _ => _environmentTextureCubemap as XRTexture ?? _environmentTextureEquirect,
            };

        public string GetPreviewShaderPath()
            => PreviewDisplay switch
            {
                ERenderPreview.Irradiance or ERenderPreview.Prefilter => "Scene3D\\Cubemap.fs",
                _ => _environmentTextureCubemap is not null ? "Scene3D\\Cubemap.fs" : "Scene3D\\Equirect.fs",
            };

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            if (_visualRC != null)
                _visualRC.WorldMatrix = Transform.WorldMatrix;
            
            base.OnTransformWorldMatrixChanged(transform);
        }

        public void FullCapture(uint colorResolution, bool captureDepth, uint depthResolution)
        {
            SetCaptureResolution(colorResolution, captureDepth, depthResolution);
            QueueCapture();
        }

        private void QueueCapture()
            => World?.Lights?.QueueForCapture(this);

        protected override void InitializeForCapture()
        {
            base.InitializeForCapture();

            //Irradiance texture doesn't need to be very high quality, 
            //linear filtering on low resolution will do fine
            IrradianceTexture = new XRTextureCube(64, EPixelInternalFormat.Rgb8, EPixelFormat.Rgb, EPixelType.UnsignedByte, false)
            {
                MinFilter = ETexMinFilter.Linear,
                MagFilter = ETexMagFilter.Linear,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                WWrap = ETexWrapMode.ClampToEdge,
                Resizable = false,
                SizedInternalFormat = ESizedInternalFormat.Rgb8,
                AutoGenerateMipmaps = false,
            };

            PrefilterTex = new XRTextureCube(ColorResolution, EPixelInternalFormat.Rgb16f, EPixelFormat.Rgb, EPixelType.HalfFloat, false)
            {
                MinFilter = ETexMinFilter.LinearMipmapLinear,
                MagFilter = ETexMagFilter.Linear,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                WWrap = ETexWrapMode.ClampToEdge,
                Resizable = false,
                SizedInternalFormat = ESizedInternalFormat.Rgb16f,
                AutoGenerateMipmaps = false,
            };

            ShaderVar[] prefilterVars =
            [
                new ShaderFloat(0.0f, "Roughness"),
                new ShaderInt((int)ColorResolution, "CubemapDim"),
            ];

            XRShader irrShader = ShaderHelper.LoadEngineShader("Scene3D\\IrradianceConvolution.fs", EShaderType.Fragment);
            XRShader prefShader = ShaderHelper.LoadEngineShader("Scene3D\\Prefilter.fs", EShaderType.Fragment);

            RenderingParameters r = new();
            r.DepthTest.Enabled = ERenderParamUsage.Disabled;
            r.CullMode = ECullMode.None;
            XRTexture[] texArray = [_environmentTextureCubemap!];
            XRMaterial irrMat = new([], texArray, irrShader);
            XRMaterial prefMat = new(prefilterVars, texArray, prefShader);

            _irradianceFBO = new XRCubeFrameBuffer(irrMat, null, 0.0f, 1.0f, false);
            _prefilterFBO = new XRCubeFrameBuffer(prefMat, null, 0.0f, 1.0f, false);

            CachePreviewSphere();
        }

        public void InitializeStatic()
        {
            //Irradiance texture doesn't need to be very high quality, 
            //linear filtering on low resolution will do fine
            IrradianceTexture = new XRTextureCube(64, EPixelInternalFormat.Rgb8, EPixelFormat.Rgb, EPixelType.UnsignedByte, false)
            {
                MinFilter = ETexMinFilter.Linear,
                MagFilter = ETexMagFilter.Linear,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                WWrap = ETexWrapMode.ClampToEdge,
                Resizable = false,
                SizedInternalFormat = ESizedInternalFormat.Rgb8,
                AutoGenerateMipmaps = false,
            };

            PrefilterTex = new XRTextureCube(ColorResolution, EPixelInternalFormat.Rgb16f, EPixelFormat.Rgb, EPixelType.HalfFloat, false)
            {
                MinFilter = ETexMinFilter.LinearMipmapLinear,
                MagFilter = ETexMagFilter.Linear,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                WWrap = ETexWrapMode.ClampToEdge,
                Resizable = false,
                SizedInternalFormat = ESizedInternalFormat.Rgb16f,
                AutoGenerateMipmaps = false,
            };

            ShaderVar[] prefilterVars =
            [
                new ShaderFloat(0.0f, "Roughness"),
                new ShaderInt((int)ColorResolution, "CubemapDim"),
            ];

            XRShader irrShader = ShaderHelper.LoadEngineShader("Scene3D\\IrradianceConvolutionEquirect.fs");
            XRShader prefShader = ShaderHelper.LoadEngineShader("Scene3D\\PrefilterEquirect.fs");

            RenderingParameters r = new();
            r.DepthTest.Enabled = ERenderParamUsage.Disabled;
            r.CullMode = ECullMode.None;
            XRTexture[] texArray = [_environmentTextureEquirect!];
            XRMaterial irrMat = new([], texArray, irrShader);
            XRMaterial prefMat = new(prefilterVars, texArray, prefShader);

            _irradianceFBO = new XRCubeFrameBuffer(irrMat, null, 0.0f, 1.0f, false);
            _prefilterFBO = new XRCubeFrameBuffer(prefMat, null, 0.0f, 1.0f, false);

            CachePreviewSphere();
        }

        private XRTexture2D? _environmentTextureEquirect;
        public XRTexture2D? EnvironmentTextureEquirect
        {
            get => _environmentTextureEquirect;
            set => SetField(ref _environmentTextureEquirect, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(EnvironmentTextureEquirect):
                    if (EnvironmentTextureEquirect is not null)
                        InitializeStatic();
                    break;
                case nameof(PreviewDisplay):
                    CachePreviewSphere();
                    break;
                case nameof(RealTimeCapture):
                    if (RealTimeCapture)
                        _realTimeTimer.StartMultiFire(QueueCapture, RealTimeCaptureUpdateInterval ?? TimeSpan.Zero);
                    else
                        _realTimeTimer.Cancel();
                    break;
                case nameof(RealTimeCaptureUpdateInterval):
                    _realTimeTimer.TimeBetweenFires = RealTimeCaptureUpdateInterval ?? TimeSpan.Zero;
                    break;
            }
        }

        public void GenerateIrradianceMap()
            => _generateIrradiance = true;

        private void GenerateIrradianceInternal()
        {
            if (IrradianceTexture is null)
                return;

            _environmentTextureEquirect?.Bind();
            _environmentTextureEquirect?.GenerateMipmapsGPU();

            uint res = IrradianceTexture.Extent;
            //AbstractRenderer.Current?.SetRenderArea(new BoundingRectangle(IVector2.Zero, new IVector2((int)res, (int)res)));
            using (Engine.Rendering.State.RenderingPipelineState?.PushRenderArea(new BoundingRectangle(IVector2.Zero, new IVector2((int)res, (int)res))))
            {
                for (int i = 0; i < 6; ++i)
                {
                    _irradianceFBO!.SetRenderTargets((IrradianceTexture, EFrameBufferAttachment.ColorAttachment0, 0, i));
                    using (_irradianceFBO!.BindForWritingState())
                    {
                        Engine.Rendering.State.ClearByBoundFBO();
                        //Engine.Rendering.State.EnableDepthTest(false);
                        //Engine.Rendering.State.StencilMask(~0u);
                        _irradianceFBO.RenderFullscreen(ECubemapFace.PosX + i);
                    }
                }
            }
        }

        public void GeneratePrefilterMap()
            => _generatePrefilter = true;

        private void GeneratePrefilterInternal()
        {
            if (PrefilterTex is null)
                return;

            _environmentTextureEquirect?.Bind();
            _environmentTextureEquirect?.GenerateMipmapsGPU();

            PrefilterTex.Bind();
            PrefilterTex.GenerateMipmapsGPU();

            int maxMipLevels = 5;
            int res = _prefilterFBO!.Material!.Parameter<ShaderInt>(1)!.Value;
            for (int mip = 0; mip < maxMipLevels; ++mip)
            {
                int mipWidth = (int)(res * Math.Pow(0.5, mip));
                int mipHeight = (int)(res * Math.Pow(0.5, mip));
                float roughness = (float)mip / (maxMipLevels - 1);

                _prefilterFBO.Material.Parameter<ShaderFloat>(0)!.Value = roughness;
                _prefilterFBO.Material.Parameter<ShaderInt>(1)!.Value = (int)ColorResolution;

                using (Engine.Rendering.State.RenderingPipelineState?.PushRenderArea(new BoundingRectangle(IVector2.Zero, new IVector2(mipWidth, mipHeight))))
                {
                    for (int i = 0; i < 6; ++i)
                    {
                        _prefilterFBO.SetRenderTargets((PrefilterTex, EFrameBufferAttachment.ColorAttachment0, mip, i));
                        using (_prefilterFBO.BindForWritingState())
                        {
                            Engine.Rendering.State.ClearByBoundFBO();
                            //Engine.Rendering.State.EnableDepthTest(false);
                            //Engine.Rendering.State.StencilMask(~0u);
                            _prefilterFBO.RenderFullscreen(ECubemapFace.PosX + i);
                        }
                    }
                }
            }
            
        }

        private void CachePreviewSphere()
        {
            PreviewSphere?.Destroy();

            int pass = (int)EDefaultRenderPass.OpaqueForward;
            PreviewSphere = new XRMeshRenderer(XRMesh.Shapes.SolidSphere(Vector3.Zero, 1.0f, 20u), new([GetPreviewTexture()], XRShader.EngineShader(GetPreviewShaderPath(), EShaderType.Fragment)) { RenderPass = pass });
            _visualRC.Mesh = PreviewSphere;
            _visualRC.WorldMatrix = Transform.WorldMatrix;
            _visualRC.RenderPass = pass;
            //VisualRenderInfo.CullingVolume = new Sphere(Vector3.Zero, 1.0f);
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            World?.Lights.LightProbes.Add(this);
        }

        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();
            World?.Lights.LightProbes.Remove(this);
        }

        //private bool _capturing = false;
        //public override void CollectVisible()
        //{
        //    if (RealTimeCapture && RealTimeCaptureUpdateInterval is not null && (DateTime.Now - _lastUpdateTime < RealTimeCaptureUpdateInterval))
        //        return;
            
        //    _lastUpdateTime = DateTime.Now;
        //    _capturing = true;
        //    base.CollectVisible();
        //}
        //public override void SwapBuffers()
        //{
        //    if (_capturing)
        //        base.SwapBuffers();
        //}
        public override void Render()
        {
            //if (!_capturing)
            //    return;
            //_capturing = false;

            base.Render();
            GenerateIrradianceInternal();
            GeneratePrefilterInternal();
        }
    }
}
