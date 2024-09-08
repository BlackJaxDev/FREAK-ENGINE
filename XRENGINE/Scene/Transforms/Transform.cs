﻿using System.Numerics;
using XREngine.Data.Transforms.Rotations;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// This is the default derived transform class for scene nodes.
    /// Can transform the node in any order of translation, scale and rotation.
    /// T-R-S is default (translation, rotated at that point, and then scaled in that coordinate system).
    /// </summary>
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
            : this(Vector3.Zero, Quaternion.Identity) { }
        public Transform(Vector3 scale, Vector3 translation, Quaternion rotation, TransformBase? parent = null, EOrder order = EOrder.TRS)
            : base(parent)
        {
            Scale = scale;
            Translation = translation;
            Rotation = rotation;
            Order = order;
        }
        public Transform(Vector3 scale, Vector3 translation, Rotator rotation, TransformBase? parent = null, EOrder order = EOrder.TRS)
            : base(parent)
        {
            Scale = scale;
            Translation = translation;
            Rotator = rotation;
            Order = order;
        }
        public Transform(Vector3 translation, Quaternion rotation, TransformBase? parent = null, EOrder order = EOrder.TRS)
            : this(Vector3.One, translation, rotation, parent, order) { }
        public Transform(Vector3 translation, Rotator rotation, TransformBase? parent = null, EOrder order = EOrder.TRS)
            : this(Vector3.One, translation, rotation, parent, order) { }
        public Transform(Quaternion rotation, TransformBase? parent = null, EOrder order = EOrder.TRS)
            : this(Vector3.Zero, rotation, parent, order) { }
        public Transform(Rotator rotation, TransformBase? parent = null, EOrder order = EOrder.TRS)
            : this(Vector3.Zero, rotation, parent, order) { }
        public Transform(Vector3 translation, TransformBase? parent = null, EOrder order = EOrder.TRS)
            : this(translation, Quaternion.Identity, parent, order) { }
        public Transform(TransformBase? parent = null, EOrder order = EOrder.TRS)
            : this(Quaternion.Identity, parent, order) { }

        private Vector3 _scale;
        public Vector3 Scale
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

        private Vector3 _translation;
        public Vector3 Translation
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

        public Rotator Rotator
        {
            get => Rotator.FromQuaternion(Rotation);
            set => Rotation = value.ToQuaternion();
        }

        private Quaternion _rotation;
        public Quaternion Rotation
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

        public void ApplyRotation(Quaternion rotation)
            => Rotation = Quaternion.Normalize(Rotation * rotation);

        public void ApplyTranslation(Vector3 translation)
            => Translation += translation;

        public void ApplyScale(Vector3 scale)
            => Scale *= scale;

        private Func<Matrix4x4>? _localMatrixGen;
        protected override Matrix4x4 CreateLocalMatrix()
            => _localMatrixGen?.Invoke() ?? Matrix4x4.Identity;

        private Matrix4x4 RTS() =>
            Matrix4x4.CreateFromQuaternion(Rotation) *
            Matrix4x4.CreateTranslation(Translation) *
            Matrix4x4.CreateScale(Scale);

        private Matrix4x4 SRT() =>
            Matrix4x4.CreateScale(Scale) *
            Matrix4x4.CreateFromQuaternion(Rotation) *
            Matrix4x4.CreateTranslation(Translation);

        private Matrix4x4 TSR() =>
            Matrix4x4.CreateTranslation(Translation) *
            Matrix4x4.CreateScale(Scale) *
            Matrix4x4.CreateFromQuaternion(Rotation);

        private Matrix4x4 STR() =>
            Matrix4x4.CreateScale(Scale) *
             Matrix4x4.CreateTranslation(Translation) *
             Matrix4x4.CreateFromQuaternion(Rotation);

        private Matrix4x4 RST() =>
            Matrix4x4.CreateFromQuaternion(Rotation) *
            Matrix4x4.CreateScale(Scale) *
            Matrix4x4.CreateTranslation(Translation);

        private Matrix4x4 TRS() =>
            Matrix4x4.CreateTranslation(Translation) *
            Matrix4x4.CreateFromQuaternion(Rotation) *
            Matrix4x4.CreateScale(Scale);

        /// <summary>
        /// Transforms the position in the direction of the local forward vector.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void TranslateRelative(float x, float y, float z)
            => Translation += Vector3.Transform(new Vector3(x, y, z), Matrix4x4.CreateFromQuaternion(Rotation));
    }
}