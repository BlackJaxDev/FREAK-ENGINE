using System.Numerics;

namespace XREngine.Scene.Transforms
{
    public class MirroredTransform : TransformBase
    {
        public MirroredTransform() { }
        public MirroredTransform(TransformBase parent)
            : base(parent) { }

        private TransformBase? _mirrorForwardTransform;
        public TransformBase? MirrorForwardTransform
        {
            get => _mirrorForwardTransform;
            set => SetField(ref _mirrorForwardTransform, value);
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(MirrorForwardTransform):
                        if (MirrorForwardTransform is not null)
                            MirrorForwardTransform.WorldMatrixChanged -= OnSourceMatrixChanged;
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
                case nameof(MirrorForwardTransform):
                    if (MirrorForwardTransform is not null)
                        MirrorForwardTransform.WorldMatrixChanged += OnSourceMatrixChanged;
                    break;
            }
        }

        private void OnSourceMatrixChanged(TransformBase @base)
            => MarkWorldModified();

        private static Vector3 Reflect(Vector3 vector, Vector3 normal)
            => vector - 2.0f * Vector3.Dot(vector, normal) * normal;

        protected override Matrix4x4 CreateWorldMatrix()
        {
            //Take parent world matrix and mirror it along the forward axis of the mirror transform
            if (MirrorForwardTransform is null || Parent is null)
                return Parent?.WorldMatrix ?? Matrix4x4.Identity;

            Vector3 mirrorNormal = -MirrorForwardTransform.WorldForward;
            Vector3 mirrorPosition = MirrorForwardTransform.WorldTranslation;
            Vector3 cameraPos = Parent.WorldTranslation;
            Vector3 cameraTarget = Parent.WorldTranslation + Parent.WorldForward;
            Vector3 reflectedCameraPos = Reflect(cameraPos - mirrorPosition, mirrorNormal) + mirrorPosition;
            Vector3 reflectedCameraTarget = Reflect(cameraTarget - mirrorPosition, mirrorNormal) + mirrorPosition;
            return Matrix4x4.CreateLookAt(reflectedCameraPos, reflectedCameraTarget, Parent.WorldUp);
        }

        protected override Matrix4x4 CreateLocalMatrix()
            => Matrix4x4.Identity;
    }
}