using System.Collections.Concurrent;
using XREngine.Rendering.Models;

namespace XREngine.Components.Scene.Mesh
{
    public class ModelComponent : RenderableComponent
    {
        private readonly ConcurrentDictionary<SubMesh, RenderableMesh> _meshLinks = new();

        private Model? _model;
        public Model? Model
        {
            get => _model;
            set => SetField(ref _model, value);
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
                                RemoveMesh(mesh);

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
                            AddMesh(mesh);

                        Model.Meshes.PostAnythingAdded += AddMesh;
                        Model.Meshes.PostAnythingRemoved += RemoveMesh;
                    }
                    break;
            }
        }

        private void AddMesh(SubMesh item)
        {
            RenderableMesh mesh = new(item, this);
            Meshes.Add(mesh);
            _meshLinks.TryAdd(item, mesh);
        }
        private void RemoveMesh(SubMesh item)
        {
            if (_meshLinks.TryRemove(item, out RenderableMesh? mesh))
                Meshes.Remove(mesh);
        }
    }
}
