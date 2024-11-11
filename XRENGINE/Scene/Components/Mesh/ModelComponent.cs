using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using XREngine.Data.Geometry;
using XREngine.Rendering.Models;
using XREngine.Scene.Transforms;

namespace XREngine.Components.Scene.Mesh
{
    public class ModelComponent : RenderableComponent
    {
        private readonly ConcurrentDictionary<SubMesh, RenderableMesh> _meshLinks = new();

        private Model? _model;
        private TransformBase? _rootBone;

        public Model? Model
        {
            get => _model;
            set => SetField(ref _model, value);
        }

        public TransformBase? RootBone
        {
            get => _rootBone;
            set => SetField(ref _rootBone, value);
        }

        private IReadOnlyDictionary<SubMesh, RenderableMesh> MeshLinks => _meshLinks;

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(Model):
                        if (Model != null)
                        {
                            foreach (SubMesh mesh in Model.Meshes)
                                if (_meshLinks.TryRemove(mesh, out RenderableMesh? mesh2))
                                    Meshes.Remove(mesh2);

                            Model.Meshes.PostAnythingAdded -= AddMesh;
                            Model.Meshes.PostAnythingRemoved -= RemoveMesh;
                        }
                        break;
                }
            }
            return change;
        }
        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Model):
                    Meshes.Clear();
                    if (Model != null)
                    {
                        foreach (SubMesh mesh in Model.Meshes)
                        {
                            RenderableMesh mesh2 = new(mesh, this)
                            {
                                RootTransform = mesh.RootTransform
                            };
                            Meshes.Add(mesh2);
                            _meshLinks.TryAdd(mesh, mesh2);
                        }

                        Model.Meshes.PostAnythingAdded += AddMesh;
                        Model.Meshes.PostAnythingRemoved += RemoveMesh;
                    }
                    break;
            }
        }

        private void AddMesh(SubMesh item)
        {
            RenderableMesh mesh = new(item, this)
            {
                RootTransform = item.RootTransform
            };
            Meshes.Add(mesh);
            _meshLinks.TryAdd(item, mesh);

            DetermineRootBone();
        }
        private void RemoveMesh(SubMesh item)
        {
            if (_meshLinks.TryRemove(item, out RenderableMesh? mesh))
                Meshes.Remove(mesh);

            DetermineRootBone();
        }

        public void DetermineRootBone()
        {
            if (Model is null)
                return;

            RootBone = TransformBase.FindCommonAncestor(
                Model.Meshes.SelectMany(x => x.LODs)
                            .SelectMany(x => x.Mesh?.UtilizedBones ?? [])
                            .Select(x => x.tfm)
                            .Distinct()
                            .ToArray());
        }

        [RequiresDynamicCode("")]
        public float? Intersect(Segment segment, out Triangle? triangle)
        {
            triangle = null;
            float? closest = null;
            foreach (RenderableMesh mesh in Meshes)
            {
                var m = mesh.CurrentLODMesh;
                if (m is null)
                    continue;

                float? distance = mesh.Intersect(GetLocalSegment(segment, m.HasSkinning), out triangle);
                if (distance.HasValue && (!closest.HasValue || distance < closest))
                    closest = distance;
            }
            return closest;
        }

        public Segment GetLocalSegment(Segment worldSegment, bool skinnedMesh)
        {
            Segment localSegment;
            if (skinnedMesh)
            {
                if (RootBone is not null)
                    localSegment = worldSegment.TransformedBy(RootBone.InverseWorldMatrix);
                else
                    localSegment = worldSegment;
            }
            else
            {
                localSegment = worldSegment.TransformedBy(Transform.InverseWorldMatrix);
            }

            return localSegment;
        }
    }
}
