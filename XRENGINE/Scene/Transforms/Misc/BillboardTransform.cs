﻿using Extensions;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Rendering;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Represents a transform that always faces the camera.
    /// Calculates the rotation on the CPU.
    /// Use vertex shaders or set billboard parameters in render options on the mesh directly on meshes for better performance.
    /// </summary>
    public class BillboardTransform : TransformBase
    {
        public BillboardTransform() : this(null) { }
        public BillboardTransform(TransformBase? parent) : base(parent)
            => XRCamera.CurrentRenderTargetChanged += OnCameraChanged;
        ~BillboardTransform()
            => XRCamera.CurrentRenderTargetChanged -= OnCameraChanged;

        private void OnCameraChanged(XRCamera? camera)
            => MarkWorldModified();

        private bool _perspective = false;
        /// <summary>
        /// If perspective is true, the billboard will face towards the camera's position.
        /// If perspective is false, the billboard will face towards the camera's near plane.
        /// </summary>
        public bool Perspective
        {
            get => _perspective;
            set => SetField(ref _perspective, value);
        }

        public Constrainer? Constrainment { get; set; }

        public abstract class Constrainer(BillboardTransform transform)
        {
            public BillboardTransform Transform { get; } = transform;

            public static TransformConstrainer FromTransform(BillboardTransform transform, TransformBase target)
                => new(transform, target);
            public static DirectionConstrainer FromDirection(BillboardTransform transform, Vector3 direction)
                => new(transform, direction);
            //public static PlaneConstrainer FromPlane(BillboardTransform transform, Plane plane)
            //    => new(transform, plane);

            public abstract void Constrain(
                Vector3 transformPos, Vector3 transformUp, Vector3 transformRight,
                Vector3 cameraPos, Vector3 cameraUp, Vector3 cameraRight,
                out Vector3 resultUp, out Vector3 resultForward);
        }
        public class TransformConstrainer(BillboardTransform transform, TransformBase target) : Constrainer(transform)
        {
            public TransformBase Target { get; set; } = target;

            public override void Constrain(
                Vector3 transformPos, Vector3 transformUp, Vector3 transformRight,
                Vector3 cameraPos, Vector3 cameraUp, Vector3 cameraRight,
                out Vector3 resultUp, out Vector3 resultForward)
            {
                Vector3 posOnConstrainLine = GeoUtil.SegmentClosestColinearPointToPoint(transformPos, Target.WorldTranslation, cameraPos);
                resultUp = (Target.WorldTranslation - transformPos).Normalize();
                resultForward = (cameraPos - posOnConstrainLine).Normalize();
            }
        }
        public class DirectionConstrainer(BillboardTransform transform, Vector3 direction) : Constrainer(transform)
        {
            public Vector3 Direction { get; set; } = direction;

            public override void Constrain(
                Vector3 transformPos, Vector3 transformUp, Vector3 transformRight,
                Vector3 cameraPos, Vector3 cameraUp, Vector3 cameraRight,
                out Vector3 resultUp, out Vector3 resultForward)
            {
                Vector3 worldDir = Vector3.TransformNormal(Direction.Normalize(), Transform.ParentWorldMatrix);
                Vector3 posOnConstrainLine = GeoUtil.RayClosestColinearPointToPoint(transformPos, worldDir, cameraPos);

                resultUp = worldDir.Normalize();
                resultForward = (cameraPos - posOnConstrainLine).Normalize();
            }
        }
        //public class PlaneConstrainer(BillboardTransform transform, Plane plane) : Constrainer(transform)
        //{
        //    public Plane Plane { get; set; } = plane;

        //    public override void Constrain(
        //        Vector3 transformPos, Vector3 transformUp, Vector3 transformRight,
        //        Vector3 cameraPos, Vector3 cameraUp, Vector3 cameraRight,
        //        out Vector3 resultUp, out Vector3 resultForward)
        //    {

        //    }
        //}

        private bool _billboardActive = true;
        public bool BillboardActive
        {
            get => _billboardActive;
            set => SetField(ref _billboardActive, value);
        }

        private bool _scaleByDistance = false;
        /// <summary>
        /// If true, the billboard will scale based on the distance from the camera.
        /// </summary>
        public bool ScaleByDistance
        {
            get => _scaleByDistance;
            set => SetField(ref _scaleByDistance, value);
        }

        /// <summary>
        /// This scalar is multiplied by the distance from the camera to scale the billboard.
        /// </summary>
        private float _distanceScalar = 1.0f;
        public float ScaleReferenceDistance
        {
            get => _distanceScalar;
            set => SetField(ref _distanceScalar, value);
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(Constrainment):
                        if (Constrainment is TransformConstrainer transformConstrainer)
                            transformConstrainer.Target.WorldMatrixChanged -= OnConstrainDirectionChanged;
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
                case nameof(Perspective):
                case nameof(ScaleByDistance):
                case nameof(ScaleReferenceDistance):
                    MarkWorldModified();
                    break;

                case nameof(Constrainment):
                    if (Constrainment is TransformConstrainer transformConstrainer)
                        transformConstrainer.Target.WorldMatrixChanged += OnConstrainDirectionChanged;
                    MarkWorldModified();
                    break;
            }
        }

        private void OnConstrainDirectionChanged(TransformBase @base)
            => MarkWorldModified();

        protected override Matrix4x4 CreateWorldMatrix()
        {
            var camera = XRCamera.CurrentRenderTarget;
            if (camera is null)
                return Parent?.WorldMatrix ?? Matrix4x4.Identity;

            var pos = Parent?.WorldTranslation ?? Vector3.Zero;

            Vector3 cameraPos = Perspective
                ? camera.Transform.WorldTranslation
                : GeoUtil.ClosestPointPlanePoint(camera.GetNearPlane(), pos);

            Matrix4x4 transform = Matrix4x4.Identity;
            if (BillboardActive)
            {
                var up = Parent?.WorldUp ?? Vector3.UnitY;
                var right = Parent?.WorldRight ?? Vector3.UnitX;

                var cameraUp = camera.Transform.WorldUp;
                var cameraRight = camera.Transform.WorldRight;

                Vector3 resultForward;
                Vector3 resultUp;

                if (Constrainment is not null)
                {
                    Constrainment.Constrain(
                        pos, up, right,
                        cameraPos, cameraUp, cameraRight,
                        out resultUp, out resultForward);
                }
                else
                    resultUp = Vector3.Cross(cameraRight, resultForward = (cameraPos - pos).Normalize()).Normalize();
                
                transform = Matrix4x4.CreateWorld(pos, resultForward, resultUp);
            }
            if (ScaleByDistance)
            {
                float distance = pos.Distance(cameraPos);
                float scale = distance / ScaleReferenceDistance;
                return Matrix4x4.CreateScale(scale) * transform;
            }
            else
                return transform;
        }

        protected override Matrix4x4 CreateLocalMatrix()
        {
            return Matrix4x4.Identity;
        }
    }
}