//using System.Numerics;
//using XREngine.Data.Core;

//namespace XREngine.Rendering.UI
//{
//    public class UIVector2 : XRBase
//    {
//        public static UIVector2 Zero { get; } = new UIVector2(0.0f, 0.0f);
//        public static UIVector2 One { get; } = new UIVector2(1.0f, 1.0f);

//        public UIVector2()
//        {
//            XProperty = new UIFloat();
//            YProperty = new UIFloat();
//        }
//        public UIVector2(UIFloat x, UIFloat y)
//        {
//            XProperty = x;
//            YProperty = y;
//        }
//        public UIVector2(UIFloat xy)
//        {
//            XProperty = xy;
//            YProperty = xy;
//        }

//        public UIFloat XProperty { get; } = new UIFloat();
//        public UIFloat YProperty { get; } = new UIFloat();

//        public Vector2 Xy
//        {
//            get => new Vector2(XProperty.Value, YProperty.Value);
//            set
//            {
//                XProperty.Value = value.X;
//                YProperty.Value = value.Y;
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
//    }
//}
