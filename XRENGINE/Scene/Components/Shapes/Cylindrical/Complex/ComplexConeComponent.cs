//using System;

//namespace XREngine.Components.Scene.Shapes
//{
//    public class ComplexConeComponent : BaseShapeComponent
//    {
//        private float _tipRadius, _baseRadius;
//        private Vector3 _tipPosition, _basePosition;

//        public float BaseRadius
//        {
//            get { return _baseRadius; }
//            set { _baseRadius = value; }
//        }
//        public float TipRadius
//        {
//            get { return _tipRadius; }
//            set { _tipRadius = value; }
//        }
//        public Vector3 CenterPoint
//        {
//            get { return (_tipPosition + _basePosition) / 2.0f; }
//        }
//        public Vector3 AxisDirection
//        {
//            get { return (_tipPosition - _basePosition).NormalizedFast(); }
//            set
//            {
//                Vector3 origin = CenterPoint;

//                _tipPosition -= origin;
//                _basePosition -= origin;

//                Quaternion rotation = Quaternion.BetweenVectors(AxisDirection, value.NormalizedFast());
//                _tipPosition = rotation * _tipPosition;
//                _basePosition = rotation * _basePosition;

//                _tipPosition += origin;
//                _basePosition += origin;
//            }
//        }
//        public Vector3 TipPosition
//        {
//            get { return _tipPosition; }
//            set { _tipPosition = value; }
//        }
//        public Vector3 BasePosition
//        {
//            get { return _basePosition; }
//            set { _basePosition = value; }
//        }

//        public ComplexConeComponent(Vector3 tipPosition, Vector3 basePosition, float tipRadius, float baseRadius)
//        {
//            _tipPosition = tipPosition;
//            _basePosition = basePosition;
//            _tipRadius = tipRadius;
//            _baseRadius = baseRadius;
//        }

//        public override EContainment ContainsCone(ComplexConeComponent cone)
//        {
//            throw new NotImplementedException();
//        }

//        public override EContainment ContainsBox(BoxComponent box)
//        {
//            throw new NotImplementedException();
//        }

//        public override EContainment ContainsSphere(SphereComponent sphere)
//        {
//            throw new NotImplementedException();
//        }

//        public override EContainment ContainsCapsule(CapsuleComponent capsule)
//        {
//            throw new NotImplementedException();
//        }
//        public override EContainment ContainsPoint(Vector3 point)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
