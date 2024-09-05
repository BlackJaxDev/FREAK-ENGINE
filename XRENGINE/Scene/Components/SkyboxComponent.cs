//using System.Numerics;
//using XREngine.Components.Scene.Mesh;
//using XREngine.Data.Components;
//using XREngine.Data.Geometry;
//using XREngine.Data.Rendering;
//using XREngine.Rendering;
//using XREngine.Rendering.Commands;
//using XREngine.Rendering.Info;
//using XREngine.Rendering.Models;
//using XREngine.Rendering.Models.Materials;
//using static XREngine.Rendering.XRMesh.Shapes;

//namespace XREngine.Actors.Types
//{
//    public class SkyboxComponent : StaticMeshComponent, IRenderable
//    {
//        public SkyboxComponent() : base()
//        {
//            HalfExtents = new(5000.0f);
//        }
//        public SkyboxComponent(TextureFile2D skyboxTexture, Vector3 halfExtents) : base()
//        {
//            SkyboxTexture = skyboxTexture;
//            HalfExtents = halfExtents;
//        }

//        private TextureFile2D? _skyboxTexture;
//        private XRMaterial? _material;
//        private Vector3 _halfExtents = new(5000.0f);
//        private float _bias = 0.0f;
//        private ECubemapTextureUVs _uvType = ECubemapTextureUVs.WidthLarger;
        
//        public float TexCoordEdgeBias
//        {
//            get => _bias;
//            set
//            {
//                _bias = value;
//                Remake();
//            }
//        }
//        public Vector3 HalfExtents
//        {
//            get => _halfExtents;
//            set
//            {
//                _halfExtents = value;
//                Remake();
//            }
//        }
//        public TextureFile2D? SkyboxTexture
//        {
//            get => _skyboxTexture;
//            set => SetField(ref _skyboxTexture, value);
//        }
//        public XRMaterial? Material
//        {
//            get => _material;
//            set
//            {
//                _material = value;

//                StaticModel? model = Model;
//                if (model is null || model.RigidChildren.Count == 0)
//                    return;

//                StaticRigidSubMesh mesh = model.RigidChildren[0];
//                if (mesh is null)
//                    return;
                
//                foreach (LOD lod in mesh.LODs)
//                    lod.Material = _material;

//                var renderMeshes = Meshes;
//                if (renderMeshes is null || renderMeshes.Count <= 0)
//                    return;

//                StaticRenderableMesh rmesh = renderMeshes[0];
//                if (rmesh.LODs is null || rmesh.LODs.Count == 0)
//                    return;

//                RenderableLOD? rlod = rmesh?.LODs[0];
//                if (rlod is null)
//                    return;

//                if (rlod.Manager is not null)
//                    rlod.Manager.Material = _material;
//            }
//        }

//        private void TextureLoaded(TextureFile2D tex)
//        {
//            if (Material is null || Material.Textures.Count == 0)
//                return;

//            XRTexture2D tref = (XRTexture2D)Material.Textures[0];
//            if (tref.Mipmaps.Length == 0)
//                return;
            
//            tref.Mipmaps[0] = tex;

//            ECubemapTextureUVs uvType = 
//                tex is null || 
//                tex.Bitmaps.Length == 0 || 
//                tex.Bitmaps[0] is null || 
//                tex.Bitmaps[0].Width > tex.Bitmaps[0].Height ?
//                ECubemapTextureUVs.WidthLarger :
//                ECubemapTextureUVs.HeightLarger;

//            if (_uvType != uvType)
//            {
//                _uvType = uvType;
//                Remake();
//            }
//        }
//        private void Remake()
//        {
//            StaticModel? model = Model;
//            if (model is null || model.RigidChildren.Count == 0)
//                return;

//            StaticRigidSubMesh? mesh = model.RigidChildren[0];
//            if (mesh is null)
//                return;

//            if (mesh.RenderInfo is not null)
//                mesh.RenderInfo.CullingVolume = new AABB(-HalfExtents, HalfExtents);

//            if (mesh.LODs.Count > 0)
//            {
//                LOD lod = mesh.LODs[0];
//                if (lod?.Mesh != null)
//                {
//                    lod.Mesh?.Destroy();
//                    lod.Mesh = SolidBox(-HalfExtents, HalfExtents, true, _uvType, TexCoordEdgeBias);
//                }
//            }

//            var renderMeshes = Meshes;
//            if (renderMeshes is null || renderMeshes.Count <= 0 || renderMeshes[0] is null)
//                return;

//            StaticRenderableMesh rmesh = renderMeshes[0];
//            if (rmesh?.LODs is null || rmesh.LODs.Count == 0)
//                return;

//            RenderableLOD rlod = rmesh.LODs[0];
//            XRMeshRenderer? manager = rlod?.Manager;
//            if (manager is null)
//                return;
            
//            manager.Mesh?.Destroy();
//            manager.Mesh = SolidBox(-HalfExtents, HalfExtents, true, _uvType, TexCoordEdgeBias);

//            //var bufInfo = manager.Mesh.BufferInfo;
//            //if (bufInfo != null)
//            //    bufInfo.CameraTransformFlags = ECameraTransformFlags.ConstrainTranslations;
//        }
//        protected override void Constructing()
//        {
//            _material = null;

//            TextureFile2D? tex = SkyboxTexture;
//            Vector3 max = HalfExtents;
//            Vector3 min = -max;

//            StaticModel skybox = new();

//            RenderingParameters renderParams = new()
//            {
//                DepthTest = new DepthTest()
//                {
//                    Enabled = ERenderParamUsage.Enabled,
//                    UpdateDepth = false,
//                    Function = EComparison.Less
//                }
//            };

//            XRTexture2D texRef = tex is not null ? new(tex) : new();
//            texRef.MagFilter = ETexMagFilter.Nearest;
//            texRef.MinFilter = ETexMinFilter.Nearest;

//            _material = XRMaterial.CreateUnlitTextureMaterialForward(texRef);
//            _material.RenderOptions = renderParams;

//            _uvType = 
//                tex is null || 
//                tex.Bitmaps.Length == 0 || 
//                tex.Bitmaps[0] is null || 
//                tex.Bitmaps[0].Width > tex.Bitmaps[0].Height ?
//                ECubemapTextureUVs.WidthLarger :
//                ECubemapTextureUVs.HeightLarger;

//            RenderInfo3D renderInfo = new(this);
//            StaticRigidSubMesh mesh = new(
//                renderInfo,
//                SolidBox(min, max, true, _uvType, TexCoordEdgeBias),
//                Material ?? XRMaterial.InvalidMaterial);

//            //foreach (LOD lod in mesh.LODs)
//            //    lod.TransformFlags = ECameraTransformFlags.ConstrainTranslations;

//            skybox.RigidChildren.Add(mesh);

//            Model = skybox;
//        }

//        public RenderInfo[] RenderedObjects { get; }

//        public void AddRenderCommands(RenderCommandCollection passes, XRCamera camera)
//        {

//        }
//    }
//}
