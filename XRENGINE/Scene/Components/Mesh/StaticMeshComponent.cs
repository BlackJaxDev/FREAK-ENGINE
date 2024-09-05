using Extensions;
using System.Numerics;
using XREngine.Data.Components;
using XREngine.Physics;
using XREngine.Rendering.Models;

namespace XREngine.Components.Scene.Mesh
{
    public partial class StaticMeshComponent : XRComponent, IRigidBodyCollidable
    {
        public StaticMeshComponent()
            : this(null, null) { }
        public StaticMeshComponent(StaticModel? model)
            : this(model, null) { }
        public StaticMeshComponent(StaticModel? model, RigidBodyConstructionInfo? info)
            : base()
        {
            _model = model;

            if (info is null)
                RigidBodyCollision = null;
            else
            {
                info.CollisionShape = model?.CollisionShape;
                info.InitialWorldTransform = Transform.WorldMatrix;
                RigidBodyCollision = Physics.XRRigidBody.New(info);

                //WorldTransformChanged += ThisTransformUpdated;
                //ThisTransformUpdated();
            }
        }

        private void RigidBodyTransformUpdated(Matrix4x4 transform)
        {
            //_readingPhysicsTransform = true;
            //WorldMatrix.Value = _rigidBodyCollision.InterpolationWorldTransform;
            //_readingPhysicsTransform = false;
        }
        //private void ThisTransformUpdated()
        //    => _rigidBodyCollision.WorldTransform = WorldMatrix;

        private StaticModel? _model;
        public StaticModel? Model
        {
            get => _model;
            set
            {
                if (_model == value)
                    return;

                //if (Meshes != null)
                //{
                //    if (IsSpawned)
                //        foreach (StaticRenderableMesh mesh in Meshes)
                //            mesh.RenderInfo.IsVisible = false;
                //    Meshes = null;
                //}

                //if (_model != null)
                //{
                //    _model.Loaded -= OnModelLoaded;
                //    _model.Unloaded -= OnModelUnloaded;
                //}

                _model = value;
            }
        }

        private Physics.XRRigidBody? _rigidBodyCollision = null;
        public Physics.XRRigidBody? RigidBodyCollision
        {
            get => _rigidBodyCollision;
            set
            {
                if (_rigidBodyCollision == value)
                    return;
                if (_rigidBodyCollision != null)
                {
                    if (IsActive)
                        World?.PhysicsScene?.RemoveCollisionObject(_rigidBodyCollision);

                    _rigidBodyCollision.Owner = null;
                    _rigidBodyCollision.TransformChanged -= RigidBodyTransformUpdated;
                }
                _rigidBodyCollision = value;
                if (_rigidBodyCollision != null)
                {
                    _rigidBodyCollision.Owner = this;
                    _rigidBodyCollision.TransformChanged += RigidBodyTransformUpdated;

                    if (IsActive)
                        World?.PhysicsScene?.AddCollisionObject(_rigidBodyCollision);
                }
            }
        }

        public List<StaticRenderableMesh> Meshes { get; private set; } = [];

        Physics.XRRigidBody IRigidBodyCollidable.RigidBodyCollision => RigidBodyCollision;
        Matrix4x4 ICollidable.CollidableWorldMatrix
        {
            get => Transform.WorldMatrix;
            set => Transform.DeriveWorldMatrix(value);
        }

        private void OnModelUnloaded(StaticModel model)
        {
            if (model is null)
                return;

            //model.RigidChildren.PostAnythingAdded -= RigidChildren_PostAnythingAdded;
            //model.RigidChildren.PostAnythingRemoved -= RigidChildren_PostAnythingRemoved;
            //model.SoftChildren.PostAnythingAdded -= SoftChildren_PostAnythingAdded;
            //model.SoftChildren.PostAnythingRemoved -= SoftChildren_PostAnythingRemoved;

            //foreach (var mesh in Meshes)
            //    mesh?.RenderInfo?.UnlinkScene();
            Meshes.Clear();
        }
        private void OnModelLoaded(StaticModel model)
        {
            if (model is null)
                return;

            //model.RigidChildren.PostAnythingAdded += RigidChildren_PostAnythingAdded;
            //model.RigidChildren.PostAnythingRemoved += RigidChildren_PostAnythingRemoved;
            //model.SoftChildren.PostAnythingAdded += SoftChildren_PostAnythingAdded;
            //model.SoftChildren.PostAnythingRemoved += SoftChildren_PostAnythingRemoved;

            //Meshes = new List<StaticRenderableMesh>(model.RigidChildren.Count + model.SoftChildren.Count);

            //for (int i = 0; i < model.RigidChildren.Count; ++i)
            //    RigidChildren_PostAnythingAdded(model.RigidChildren[i]);
            //for (int i = 0; i < model.SoftChildren.Count; ++i)
            //    SoftChildren_PostAnythingAdded(model.SoftChildren[i]);

            //ModelLoaded?.Invoke();
        }

        //private void RigidChildren_PostAnythingAdded(StaticRigidSubMesh item)
        //    => AddRenderMesh(item);
        //private void RigidChildren_PostAnythingRemoved(StaticRigidSubMesh item)
        //    => RemoveRenderMesh(item);
        //private void SoftChildren_PostAnythingAdded(StaticSoftSubMesh item)
        //    => AddRenderMesh(item);
        //private void SoftChildren_PostAnythingRemoved(StaticSoftSubMesh item)
        //    => RemoveRenderMesh(item);

        private void AddRenderMesh(IStaticSubMesh subMesh)
        {
            //StaticRenderableMesh m = new(subMesh, this);
            //if (IsSpawned)
            //    m.RenderInfo.LinkScene(m, OwningScene3D);
            //Meshes.Add(m);
        }
        private void RemoveRenderMesh(IStaticSubMesh subMesh)
        {
            int match = Meshes.FindIndex(x => x.Mesh == subMesh);
            if (Meshes.IndexInRange(match))
            {
                //Meshes[match]?.RenderInfo?.UnlinkScene();
                Meshes.RemoveAt(match);
            }
        }

        protected internal override void Start()
        {
            //if (Meshes is null)
            //{
            //    if (!_model.IsLoaded)
            //        await _model.GetInstanceAsync();
            //    else
            //        OnModelLoaded(_model.File);
            //}

            //if (Meshes != null)
            //    foreach (BaseRenderableMesh3D m in Meshes)
            //        m.RenderInfo.LinkScene(m, OwningScene3D);

            base.Start();
        }
        protected internal override void Stop()
        {
            base.Stop();
            //foreach (BaseRenderableMesh3D m in Meshes)
            //    m.Destroy();
        }

#if EDITOR
        protected internal override void OnHighlightChanged(bool highlighted)
        {
            base.OnHighlightChanged(highlighted);

            if (OwningScene is null)
                return;

            foreach (StaticRenderableMesh m in Meshes)
                foreach (var lod in m.LODs)
                    Editor.EditorState.RegisterHighlightedMaterial(lod.Manager.Material, highlighted, OwningScene);
        }
        protected internal override void OnSelectedChanged(bool selected)
        {
            if (Meshes != null)
                foreach (StaticRenderableMesh m in Meshes)
                {
                    var cull = m?.RenderInfo?.CullingVolume;
                    if (cull != null)
                        cull.RenderInfo.IsVisible = selected;

                    //Editor.EditorState.RegisterSelectedMesh(m, selected, OwningScene);
                }
        }
#endif
    }
}
