//using System.ComponentModel;
//using System.Numerics;
//using XREngine.Data.Rendering;
//using XREngine.Rendering;
//using XREngine.Rendering.Models.Materials;

//namespace XREngine.Components
//{
//    public class DecalComponent : BoxComponent, I3DRenderable
//    {
//        public XRMaterial Material { get; set; }

//        public DecalComponent()
//            : base()
//        {
//            RenderInfo.VisibleByDefault = true;
//        }
//        public DecalComponent(Vector3 halfExtents)
//            : base(halfExtents, null)
//        {
//            RenderInfo.VisibleByDefault = true;
//        }
//        public DecalComponent(Vector3 halfExtents, TextureFile2D texture)
//            : base(halfExtents, null)
//        {
//            RenderInfo.VisibleByDefault = true;
//            if (texture != null)
//                Material = CreateDefaulXRMaterial(texture);
//        }
//        public DecalComponent(Vector3 halfExtents, XRMaterial material)
//            : base(halfExtents, null)
//        {
//            RenderInfo.VisibleByDefault = true;
//            Material = material;
//        }
//        public DecalComponent(float height, TextureFile2D texture)
//            : base(new Vector3(1.0f, height, 1.0f), null)
//        {
//            RenderInfo.VisibleByDefault = true;
//            if (texture != null)
//            {
//                var bmp = texture.GetBitmap();
//                if (bmp != null)
//                {
//                    _shape.HalfExtents.Value = new Vector3(bmp.Width * 0.5f, height, bmp.Height * 0.5f);
//                    Material = CreateDefaulXRMaterial(texture);
//                }
//            }
//        }

//        public override Box Shape
//        {
//            get => base.Shape;
//            set
//            {
//                if (_shape != null)
//                {
//                    _shape.HalfExtentsPreSet -= ShapeHalfExtentsPreSet;
//                    _shape.HalfExtentsPostSet -= ShapeHalfExtentsPostSet;
//                    _shape.HalfExtents.Changed -= UpdateRenderCommandMatrix;
//                }

//                base.Shape = value;

//                _shape.HalfExtentsPreSet += ShapeHalfExtentsPreSet;
//                _shape.HalfExtentsPostSet += ShapeHalfExtentsPostSet;
//                _shape.HalfExtents.Changed += UpdateRenderCommandMatrix;
//            }
//        }

//        private void ShapeHalfExtentsPreSet(Box box, Vector3 halfExtents)
//            => _shape.HalfExtents.Changed -= UpdateRenderCommandMatrix;
//        private void ShapeHalfExtentsPostSet(Box box, Vector3 halfExtents)
//            => _shape.HalfExtents.Changed += UpdateRenderCommandMatrix;
//        private void UpdateRenderCommandMatrix()
//        {
//#if EDITOR
//            PreviewIconRenderCommand.Position = WorldPoint;
//#endif
//            Vector3 halfExtents = _shape.HalfExtents.Value;
//            RenderCommandDecal.WorldMatrix = WorldMatrix * halfExtents.AsScaleMatrix();
//        }
//        protected override void OnWorldTransformChanged(bool recalcChildWorldTransformsNow = true)
//        {
//            UpdateRenderCommandMatrix();
//            base.OnWorldTransformChanged(recalcChildWorldTransformsNow);
//        }

//        /// <summary>
//        /// Generates a basic decal material that projects a single texture onto surfaces. Texture may use transparency.
//        /// </summary>
//        /// <param name="texture">The texture to project as a decal.</param>
//        /// <returns>The <see cref="XRMaterial"/> to be used with a <see cref="DecalComponent"/>.</returns>
//        public static XRMaterial CreateDefaulXRMaterial(TextureFile2D texture)
//        {
//            XRTexture2D[] decalRefs =
//            [
//                null, //Viewport's Albedo/Opacity texture
//                null, //Viewport's Normal texture
//                null, //Viewport's RMSI texture
//                null, //Viewport's Depth texture
//                new XRTexture2D("DecalTexture", texture)
//            ];
//            XRShader decalShader = XRShader.EngineShader(Path.Combine(Viewport.SceneShaderPath, "DeferredDecal.fs"), EShaderType.Fragment);
//            ShaderVar[] decalVars = [];
//            RenderingParameters decalRenderParams = new()
//            {
//                CullMode = ECulling.Front,
//                Requirements = EUniformRequirements.Camera,
//                DepthTest = new DepthTest() { Enabled = ERenderParamUsage.Disabled }
//            };
//            return new XRMaterial("DecalMat", decalRenderParams, decalVars, decalRefs, decalShader);
//        }
//        protected override void OnSpawned()
//        {
//            if (Material is null)
//                return;

//            RenderCommandDecal.Mesh = new MeshRenderer(BoundingBox.SolidMesh(-Vector3.One, Vector3.One), Material);
//            RenderCommandDecal.Mesh.SettingUniforms += DecalManager_SettingUniforms;

//            base.OnSpawned();
//        }
//        protected virtual void DecalManager_SettingUniforms(RenderProgram vtxProg, RenderProgram matProg)
//        {
//            if (matProg is null)
//                return;

//            XRViewport? v = RenderState.CurrentlyRenderingViewport;
//            if (v is null)
//                return;

//            matProg.Sampler("Texture0", v.AlbedoOpacityTexture, 0);
//            matProg.Sampler("Texture1", v.NormalTexture, 1);
//            matProg.Sampler("Texture2", v.RMSITexture, 2);
//            matProg.Sampler("Texture3", v.DepthViewTexture, 3);
//            matProg.Uniform("BoxWorldMatrix", WorldMatrix);
//            matProg.Uniform("InvBoxWorldMatrix", InverseWorldMatrix);
//            matProg.Uniform("BoxHalfScale", _shape.HalfExtents.Value);
//        }

//        public RenderCommandMesh3D RenderCommandDecal { get; }
//            = new RenderCommandMesh3D(ERenderPass.DeferredDecals);

//#if EDITOR

//        [Category("Editor Traits")]
//        public bool ScalePreviewIconByDistance { get; set; } = true;
//        [Category("Editor Traits")]
//        public float PreviewIconScale { get; set; } = 0.05f;

//        string IEditorPreviewIconRenderable.PreviewIconName => PreviewIconName;
//        protected string PreviewIconName { get; } = "CameraIcon.png";

//        PreviewRenderCommand3D IEditorPreviewIconRenderable.PreviewIconRenderCommand
//        {
//            get => PreviewIconRenderCommand;
//            set => PreviewIconRenderCommand = value;
//        }
//        private PreviewRenderCommand3D _previewIconRenderCommand;
//        private PreviewRenderCommand3D PreviewIconRenderCommand 
//        {
//            get => _previewIconRenderCommand ??= CreatePreviewRenderCommand(PreviewIconName);
//            set => _previewIconRenderCommand = value; 
//        }
//#endif

//        //TODO: separate visibility of the decal mesh and wireframe intersection
//        public override void AddRenderables(RenderCommandCollection passes, XRCamera camera)
//        {
//            passes.Add(RenderCommandDecal);
//#if EDITOR
//            if (Engine.EditorState.InEditMode)
//            {
//                base.AddRenderables(passes, camera);
//                AddPreviewRenderCommand(PreviewIconRenderCommand, passes, camera, ScalePreviewIconByDistance, PreviewIconScale);
//            }
//#endif
//        }
//    }
//}
