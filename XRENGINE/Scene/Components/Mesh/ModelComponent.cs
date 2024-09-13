using XREngine.Rendering.Models;

namespace XREngine.Components.Scene.Mesh
{
    public class ModelComponent : RenderableComponent
    {
        private Model? _model;
        public Model? Model
        {
            get => _model;
            set => SetField(ref _model, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Model):
                    Meshes.Clear();
                    if (Model != null)
                        Meshes.AddRange(Model.Meshes.Select(m => new RenderableMesh(m, this)));
                    break;
            }
        }
    }
}
