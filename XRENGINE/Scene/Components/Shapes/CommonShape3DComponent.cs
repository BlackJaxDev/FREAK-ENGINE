using XREngine.Data.Geometry;
using XREngine.Physics;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene.Transforms;

namespace XREngine.Data.Components
{
    public abstract class CommonShape3DComponent<T> : CollidableShape3DComponent where T : IShape, new()
    {
        protected CommonShape3DComponent(T shape, ICollisionObjectConstructionInfo? info = null)
            : base()
        {
            _shape = shape;
            RenderCommand = new RenderCommandMethod3D(0, Render);
            GenerateCollisionObject(info);
        }

        public RenderCommandMethod3D RenderCommand { get; }
        protected T _shape;

        public override RenderInfo3D RenderInfo
        {
            get => base.RenderInfo;
            set
            {
                base.RenderInfo = value;
                RenderInfo.CullingVolume = _shape;
                RenderInfo.CastsShadows = false;
                RenderInfo.ReceivesShadows = false;
            }
        }

        public virtual T Shape
        {
            get => _shape;
            set
            {
                //if (_shape != null)
                //    _shape.VolumePropertyChanged -= _shape_VolumePropertyChanged;
                _shape = value ?? new T();
                //_shape.VolumePropertyChanged += _shape_VolumePropertyChanged;
                RenderInfo.CullingVolume = _shape;
                ShapeChanged();
            }
        }

        private void ShapeChanged()
        {
            if (CollisionObject == null)
                return;
            
            CollisionObject.CollisionShape = GetCollisionShape();
            RigidBodyUpdated();
        }

        protected virtual void Render(bool shadowPass)
        {
            //_shape?.Render(shadowPass);
        }

        protected override RenderCommand3D GetRenderCommand()
            => RenderCommand;

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            base.OnTransformWorldMatrixChanged(transform);
            //_shape?.SetTransformMatrix(WorldMatrix);
        }

        //public override CollisionShape GetCollisionShape()
        //{
        //    //_shape?.GetCollisionShape();
        //}
    }
}
