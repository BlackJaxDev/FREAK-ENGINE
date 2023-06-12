using Google.Type;
using XREngine.Data.Transforms;
using XREngine.Data.Transforms.Rotations;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Scenes.Transforms
{
    public class Transform : TransformBase
    {
        public enum EOrder
        {
            TRS,
            RST,
            STR,
            TSR,
            SRT,
            RTS,
        }

        public Transform()
            : this(new Vec3(1, 1, 1), new Vec3(0, 0, 0), Quat.Identity) { }
        public Transform(Vec3 scale, Vec3 translation, Quat rotation, TransformBase? parent = null, EOrder order = EOrder.TRS)
            : base(parent)
        {
            Scale = scale;
            Translation = translation;
            Rotation = rotation;
            Order = order;
        }

        private Vec3 _scale;
        public Vec3 Scale
        {
            get => _scale;
            set
            {
                lock (_lock)
                {
                    _scale = value;
                    MarkLocalModified();
                }
            }
        }

        private Vec3 _translation;
        public Vec3 Translation
        {
            get => _translation;
            set
            {
                lock (_lock)
                {
                    _translation = value;
                    MarkLocalModified();
                }
            }
        }

        private Quat.RotationOrder _eulerRotationOrder = Quat.RotationOrder.XYZ;
        public Quat.RotationOrder EulerRotationOrder
        {
            get => _eulerRotationOrder;
            set => _eulerRotationOrder = value;
        }

        public Vec3 EulerRotation
        {
            get => Rotation.ToEulerAngles(EulerRotationOrder);
            set => Rotation = Quat.FromEulerAngles(value, EulerRotationOrder);
        }

        private Quat _rotation;
        public Quat Rotation
        {
            get => _rotation;
            set
            {
                lock (_lock)
                {
                    _rotation = value;
                    MarkLocalModified();
                }
            }
        }

        private EOrder _order;
        public EOrder Order
        {
            get => _order;
            set
            {
                lock (_lock)
                {
                    _order = value;
                    _localMatrixGen = _order switch
                    {
                        EOrder.RST => RST,
                        EOrder.STR => STR,
                        EOrder.TSR => TSR,
                        EOrder.SRT => SRT,
                        EOrder.RTS => RTS,
                        _ => TRS,
                    };
                }
            }
        }

        public void ApplyRotation(Quat rotation)
        {
            Rotation = (Rotation * rotation).Normalized();
        }
        public void ApplyTranslation(Vec3 translation)
        {
            Translation += translation;
        }
        public void ApplyScale(Vec3 scale)
        {
            Scale *= scale;
        }

        private Func<Matrix> _localMatrixGen;
        protected override Matrix CreateLocalMatrix() => _localMatrixGen();

        private Matrix RTS() =>
            Matrix.CreateRotation(Rotation) *
            Matrix.CreateTranslation(Translation) *
            Matrix.CreateScale(Scale);

        private Matrix SRT() =>
            Matrix.CreateScale(Scale) *
            Matrix.CreateRotation(Rotation) *
            Matrix.CreateTranslation(Translation);

        private Matrix TSR() =>
            Matrix.CreateTranslation(Translation) *
            Matrix.CreateScale(Scale) *
            Matrix.CreateRotation(Rotation);

        private Matrix STR() =>
            Matrix.CreateScale(Scale) *
             Matrix.CreateTranslation(Translation) *
             Matrix.CreateRotation(Rotation);

        private Matrix RST() =>
            Matrix.CreateRotation(Rotation) *
            Matrix.CreateScale(Scale) *
            Matrix.CreateTranslation(Translation);

        private Matrix TRS() =>
            Matrix.CreateTranslation(Translation) *
            Matrix.CreateRotation(Rotation) *
            Matrix.CreateScale(Scale);
    }
}