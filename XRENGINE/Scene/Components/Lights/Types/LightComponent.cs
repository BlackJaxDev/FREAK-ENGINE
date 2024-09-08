﻿using Extensions;
using System.Numerics;
using XREngine.Data;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Trees;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene;

namespace XREngine.Components.Lights
{
    public abstract class LightComponent : XRComponent, IRenderable
    {
        protected ColorF3 _color = new(1.0f, 1.0f, 1.0f);
        protected float _diffuseIntensity = 1.0f;

        protected int _lightIndex = -1;
        protected RenderCommandCollection _renderCommands;
        private XRMaterialFrameBuffer? _shadowMap;
        private XRCamera? _shadowCamera;

        protected BoundingRectangle _shadowMapRenderRegion = new(1024, 1024);
        private ELightType _type = ELightType.Dynamic;
        private bool _castsShadows = true;
        private Matrix4x4 _lightMatrix = Matrix4x4.Identity;

        private readonly NearToFarRenderCommandSorter _nearToFarSorter = new();
        private readonly FarToNearRenderCommandSorter _farToNearSorter = new();

        private float _shadowMaxBias = 0.1f;
        private float _shadowMinBias = 0.00001f;
        private float _shadowExponent = 1.0f;
        private float _shadowExponentBase = 0.04f;

        public LightComponent() : base()
        {
            //TODO: I think this can have a limited number of render passes compared to a normal, non-shadow pass render
            _renderCommands = new RenderCommandCollection(new()
            {
                { 0, _nearToFarSorter }, //Background
                { 1, _nearToFarSorter }, //OpaqueDeferredLit
                { 2, _nearToFarSorter }, //DeferredDecals
                { 3, _nearToFarSorter }, //OpaqueForward
                { 4, _farToNearSorter }, //TransparentForward
                { 5, _nearToFarSorter }, //OnTopForward
            });
            RenderInfo = new RenderInfo3D(this) { VisibleInLightingProbes = false };
            RenderedObjects = [RenderInfo];
        }

        public Matrix4x4 LightMatrix
        {
            get => _lightMatrix;
            protected set => SetField(ref _lightMatrix, value);
        }

        public XRMaterialFrameBuffer? ShadowMap
        {
            get => _shadowMap;
            protected set => SetField(ref _shadowMap, value);
        }

        public XRCamera? ShadowCamera
        {
            get => _shadowCamera;
            protected set => SetField(ref _shadowCamera, value);
        }

        public bool CastsShadows
        {
            get => _castsShadows;
            set => SetField(ref _castsShadows, value);
        }

        public float ShadowExponentBase 
        {
            get => _shadowExponentBase;
            set => SetField(ref _shadowExponentBase, value);
        }

        public float ShadowExponent
        {
            get => _shadowExponent;
            set => SetField(ref _shadowExponent, value);
        }

        public float ShadowMinBias
        {
            get => _shadowMinBias;
            set => SetField(ref _shadowMinBias, value);
        }

        public float ShadowMaxBias
        {
            get => _shadowMaxBias;
            set => SetField(ref _shadowMaxBias, value);
        }

        public uint ShadowMapResolutionWidth
        {
            get => (uint)_shadowMapRenderRegion.Width;
            set => SetShadowMapResolution(value, (uint)_shadowMapRenderRegion.Height);
        }

        public uint ShadowMapResolutionHeight
        {
            get => (uint)_shadowMapRenderRegion.Height;
            set => SetShadowMapResolution((uint)_shadowMapRenderRegion.Width, value);
        }

        public ColorF3 LightColor
        {
            get => _color;
            set => SetField(ref _color, value);
        }

        public float DiffuseIntensity
        {
            get => _diffuseIntensity;
            set => SetField(ref _diffuseIntensity, value);
        }

        public ELightType Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }

        public RenderInfo RenderInfo { get; }
        public RenderInfo[] RenderedObjects { get; }

        internal void SetShadowUniforms(XRRenderProgram program)
        {
            program.Uniform(Engine.Rendering.Constants.ShadowExponentBaseUniform, ShadowExponentBase);
            program.Uniform(Engine.Rendering.Constants.ShadowExponentUniform, ShadowExponent);
            program.Uniform(Engine.Rendering.Constants.ShadowBiasMinUniform, ShadowMinBias);
            program.Uniform(Engine.Rendering.Constants.ShadowBiasMaxUniform, ShadowMaxBias);
        }

        public virtual void SetShadowMapResolution(uint width, uint height)
        {
            _shadowMapRenderRegion.Width = (int)width;
            _shadowMapRenderRegion.Height = (int)height;

            if (ShadowMap is null)
                ShadowMap = new XRMaterialFrameBuffer(GetShadowMapMaterial(width, height));
            else
                ShadowMap.Resize(width, height);
        }

        public abstract void SetUniforms(XRRenderProgram program, string? targetStructName = null);

        protected virtual IVolume? GetShadowVolume()
            => ShadowCamera?.WorldFrustum();

        public abstract XRMaterial GetShadowMapMaterial(uint width, uint height, EDepthPrecision precision = EDepthPrecision.Flt32);

        public void CollectShadowMap(VisualScene scene)
        {
            if (!CastsShadows || ShadowCamera is null)
                return;

            void AddRenderable(RenderInfo renderable)
            {
                renderable.AddRenderCommands(_renderCommands, ShadowCamera);
            }
            void AddOctreeItem(IOctreeItem octreeItem)
            {
                if (octreeItem is not RenderInfo3D renderable)
                    return;
                
                //TODO: render commands need to be in render info
                AddRenderable(renderable);
            }

            IVolume? volume = GetShadowVolume();
            if (volume is null)
            {
                //TODO: parallel renderable consumption thread?
                scene.Renderables.ForEach(AddRenderable);
                //scene.Tree.CollectAll(AddRenderable);
            }
            else if (scene.RenderablesTree is I3DRenderTree tree)
                tree.CollectIntersecting(volume, false, AddOctreeItem);
        }

        internal void SwapBuffers()
            => _renderCommands.SwapBuffers();

        private XRRenderPipeline? _shadowRenderPipeline = null;
        /// <summary>
        /// This is the rendering setup this viewport will use to render the scene the camera sees.
        /// A render pipeline is a collection of render passes that will be executed in order to render the scene and post-process the result, etc.
        /// </summary>
        public XRRenderPipeline ShadowRenderPipeline
        {
            get => _shadowRenderPipeline ?? SetFieldReturn(ref _shadowRenderPipeline, Engine.Rendering.NewRenderPipeline())!;
            set => SetField(ref _shadowRenderPipeline, value);
        }

        public void RenderShadowMap(VisualScene scene)
        {
            if (ShadowMap?.Material is null || ShadowCamera is null)
                return;

            using var overrideMat = Engine.Rendering.State.PushOverrideMaterial(ShadowMap.Material);
            using var overrideRegion = Engine.Rendering.State.PushRenderArea(_shadowMapRenderRegion);
            
            scene.PreRender(null, ShadowCamera);
            ShadowRenderPipeline.Render(scene, ShadowCamera, null, ShadowMap);
        }

        public static EPixelInternalFormat GetShadowDepthMapFormat(EDepthPrecision precision)
            => precision switch
            {
                EDepthPrecision.Int16 => EPixelInternalFormat.DepthComponent16,
                EDepthPrecision.Int24 => EPixelInternalFormat.DepthComponent24,
                EDepthPrecision.Int32 => EPixelInternalFormat.DepthComponent32,
                _ => EPixelInternalFormat.DepthComponent32f,
            };
    }
}