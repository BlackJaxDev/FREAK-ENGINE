//using Extensions;
//using System.ComponentModel;
//using XREngine.Data.Components;
//using XREngine.Physics;
//using XREngine.Rendering;
//using XREngine.Rendering.Models;
//using XREngine.Scene;

//namespace XREngine.Components.Scene.Mesh
//{
//    public partial class SkeletalMeshComponent : XRComponent, IPreRendered
//    {
//        public SkeletalMeshComponent(GlobalFileRef<SkeletalModel> mesh, LocalFileRef<Skeleton> skeleton)
//        {
//            SkeletonOverrideRef = skeleton;
//            ModelRef = mesh;
//        }
//        public SkeletalMeshComponent()
//        {
//            SkeletonOverrideRef = new LocalFileRef<Skeleton>();
//            ModelRef = new GlobalFileRef<SkeletalModel>();
//        }

//        private SkeletalModel _modelRef;

//        /// <summary>
//        /// Retrieves the model. 
//        /// May load synchronously if not currently loaded.
//        /// </summary>
//        [Browsable(false)]
//        public SkeletalModel Model => ModelRef.File;

//        public GlobalFileRef<SkeletalModel> ModelRef
//        {
//            get => _modelRef;
//            set
//            {
//                if (_modelRef == value)
//                    return;
                
//                if (Meshes != null)
//                {
//                    foreach (SkeletalRenderableMesh mesh in Meshes)
//                    {
//                        mesh.RenderInfo.UnlinkScene();
//                        mesh.RenderInfo.IsVisible = false;
//                    }
//                    Meshes = null;
//                }

//                if (_modelRef != null)
//                {
//                    _modelRef.Loaded -= OnModelLoaded;
//                    _modelRef.Unloaded -= OnModelUnloaded;
//                }

//                _modelRef = value ?? new GlobalFileRef<SkeletalModel>();

//                _modelRef.Loaded += OnModelLoaded;
//                _modelRef.Unloaded += OnModelUnloaded;
//            }
//        }

//        private void OnModelUnloaded(SkeletalModel model)
//        {
//            if (model is null)
//                return;

//            model.RigidChildren.PostAnythingAdded -= RigidChildren_PostAnythingAdded;
//            model.RigidChildren.PostAnythingRemoved -= RigidChildren_PostAnythingRemoved;
//            model.SoftChildren.PostAnythingAdded -= SoftChildren_PostAnythingAdded;
//            model.SoftChildren.PostAnythingRemoved -= SoftChildren_PostAnythingRemoved;

//            foreach (var mesh in Meshes)
//                mesh?.RenderInfo?.UnlinkScene();
//            Meshes.Clear();
//        }
//        private async void OnModelLoaded(SkeletalModel model)
//        {
//            if (model is null)
//                return;

//            //Engine.PrintLine("Skeletal Model : OnModelLoaded");

//            model.RigidChildren.PostAnythingAdded += RigidChildren_PostAnythingAdded;
//            model.RigidChildren.PostAnythingRemoved += RigidChildren_PostAnythingRemoved;
//            model.SoftChildren.PostAnythingAdded += SoftChildren_PostAnythingAdded;
//            model.SoftChildren.PostAnythingRemoved += SoftChildren_PostAnythingRemoved;

//            Skeleton skelOverride = null;
//            if (SkeletonOverrideRef != null)
//                skelOverride = await SkeletonOverrideRef.GetInstanceAsync();

//            MakeMeshes(model, skelOverride);
//        }

//        private async void MakeMeshes(SkeletalModel model, Skeleton skeletonOverride)
//        {
//            if (Meshes != null)
//                foreach (SkeletalRenderableMesh m in Meshes)
//                {
//                    m.RenderInfo.UnlinkScene();
//                    m.RenderInfo.IsVisible = false;
//                    m.Destroy();
//                }
            
//            if (model is null)
//                return;

//            TargetSkeleton = skeletonOverride ?? await model.SkeletonRef?.GetInstanceAsync();

//            Meshes = [];

//            for (int i = 0; i < model.RigidChildren.Count; ++i)
//                RigidChildren_PostAnythingAdded(model.RigidChildren[i]);
            
//            for (int i = 0; i < model.SoftChildren.Count; ++i)
//                SoftChildren_PostAnythingAdded(model.SoftChildren[i]);
//        }

//        private void RigidChildren_PostAnythingAdded(SkeletalRigidSubMesh item)
//            => AddRenderMesh(item);
//        private void RigidChildren_PostAnythingRemoved(SkeletalRigidSubMesh item)
//            => RemoveRenderMesh(item);
//        private void SoftChildren_PostAnythingAdded(SkeletalSoftSubMesh item)
//            => AddRenderMesh(item);
//        private void SoftChildren_PostAnythingRemoved(SkeletalSoftSubMesh item)
//            => RemoveRenderMesh(item);

//        private void AddRenderMesh(ISkeletalSubMesh subMesh)
//        {
//            //Engine.PrintLine("Skeletal Model : AddRenderMesh");

//            SkeletalRenderableMesh renderMesh = new SkeletalRenderableMesh(subMesh, TargetSkeleton, this);
//            if (IsSpawned)
//                renderMesh.RenderInfo.LinkScene(renderMesh, OwningScene3D);
//            Meshes.Add(renderMesh);
//        }
//        private void RemoveRenderMesh(ISkeletalSubMesh subMesh)
//        {
//            //Engine.PrintLine("Skeletal Model : RemoveRenderMesh");

//            int match = Meshes.FindIndex(x => x.Mesh == subMesh);
//            if (Meshes.IndexInRange(match))
//            {
//                Meshes[match]?.RenderInfo?.UnlinkScene();
//                Meshes.RemoveAt(match);
//            }
//        }

//        public List<SkeletalRenderableMesh> Meshes { get; private set; }

//        public bool PreRenderEnabled { get; set; } = true;

//        public void PreRenderUpdate(XRCamera camera)
//        {
//            //_targetSkeleton?.UpdateBones(camera, Matrix4.Identity, Matrix4.Identity);
//        }
//        public void PreRenderSwap()
//        {
//            //TargetSkeleton?.SwapBuffers();
//        }
//        public void PreRender(XRViewport viewport, XRCamera camera)
//        {
//            //TargetSkeleton?.UpdateBones(camera);
//        }
//    }
//}
