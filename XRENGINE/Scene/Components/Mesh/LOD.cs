using XREngine.Data.Core;

namespace XREngine.Rendering.Models
{
    public class LOD : XRBase
    {
        public LOD() 
            : this(null, null, 0) { }

        public LOD(
            XRMaterial? material,
            XRMesh? mesh,
            float visibleDistance)
        {
            _visibleDistance = visibleDistance;
            _mesh = mesh;
            _material = material;
            _renderer = new XRMeshRenderer(_mesh, _material);
        }

        private XRMaterial? _material;
        public XRMaterial? Material
        {
            get => _material;
            set => SetField(ref _material, value);
        }

        private XRMesh? _mesh;
        public XRMesh? Mesh
        {
            get => _mesh;
            set => SetField(ref _mesh, value);
        }

        private float _visibleDistance = 0.0f;
        public float VisibleDistance
        {
            get => _visibleDistance;
            set => SetField(ref _visibleDistance, value);
        }

        private XRMeshRenderer _renderer;
        public XRMeshRenderer Renderer => _renderer;

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Material):
                case nameof(Mesh):
                    _renderer.Destroy();
                    _renderer = new XRMeshRenderer(_mesh, _material);
                    break;
            }
        }
    }
}
