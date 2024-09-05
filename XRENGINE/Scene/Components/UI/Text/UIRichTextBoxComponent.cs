//using XREngine.Data.Geometry;

//namespace XREngine.Rendering.UI.Text
//{
//    public class UIRichTextBoxComponent : UITextRasterComponent
//    {
//        public UIRichTextBoxComponent() : base()
//        {
//            TextDrawer.Text.Add(_uiText);
//        }

//        protected override void OnResizeLayout(BoundingRectangleF parentRegion)
//        {
//            base.OnResizeLayout(parentRegion);
//            _uiText.Region.Extents = ActualSize.Value;
//        }

//        private readonly UIString2D _uiText = new UIString2D();

//        public string Text
//        {
//            get => _uiText.Text;
//            set => _uiText.Text = value;
//        }

//        public bool AllowHorizontalScroll { get; set; } = false;
//        public bool AllowVerticalScroll { get; set; } = true;

//        public TextFormatFlags TextFlags 
//        {
//            get => _uiText.Format;
//            set => _uiText.Format = value;
//        }
//    }
//}