//using System.Numerics;
//using XREngine.Data.Core;

//namespace XREngine.Rendering.UI
//{
//    public class UIVector3 : XRBase
//    {
//        public static UIVector3 Zero { get; } = new UIVector3(0.0f, 0.0f, 0.0f);
//        public static UIVector3 One { get; } = new UIVector3(1.0f, 1.0f, 1.0f);

//        public UIVector3()
//        {
//            XProperty = new UIFloat();
//            YProperty = new UIFloat();
//            ZProperty = new UIFloat();
//        }
//        public UIVector3(UIFloat x, UIFloat y, UIFloat z)
//        {
//            XProperty = x;
//            YProperty = y;
//            ZProperty = z;
//        }
//        public UIVector3(UIFloat xyz)
//        {
//            XProperty = xyz;
//            YProperty = xyz;
//            ZProperty = xyz;
//        }

//        public UIFloat XProperty { get; } = new UIFloat();
//        public UIFloat YProperty { get; } = new UIFloat();
//        public UIFloat ZProperty { get; } = new UIFloat();

//        public Vector3 Xyz 
//        {
//            get => new Vector3(XProperty.Value, YProperty.Value, ZProperty.Value);
//            set
//            {
//                XProperty.Value = value.X;
//                YProperty.Value = value.Y;
//                ZProperty.Value = value.Z;
//            }
//        }
//        public Vector2 Xy
//        {
//            get => new Vector2(XProperty.Value, YProperty.Value);
//            set
//            {
//                XProperty.Value = value.X;
//                YProperty.Value = value.Y;
//            }
//        }
//        public Vector2 Yz
//        {
//            get => new Vector2(YProperty.Value, ZProperty.Value);
//            set
//            {
//                YProperty.Value = value.X;
//                ZProperty.Value = value.Y;
//            }
//        }
//        public float X
//        {
//            get => XProperty.Value;
//            set => XProperty.Value = value;
//        }
//        public float Y
//        {
//            get => YProperty.Value;
//            set => YProperty.Value = value;
//        }
//        public float Z
//        {
//            get => ZProperty.Value;
//            set => ZProperty.Value = value;
//        }
//    }
//}
