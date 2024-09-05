//using System.Windows.Forms;
//using XREngine.ComponentModel;
//using XREngine.Core.Shapes;
//using XREngine.Input.Devices;
//using XREngine.Rendering.Text;

//namespace XREngine.Rendering.UI.Text
//{
//    public class UITextBoxComponent : UITextRasterComponent
//    {
//        [TSerialize]
//        public bool Multiline { get; set; }

//        public UITextBoxComponent() : base()
//        {
//            _uiText = new UIString2D();
//            _uiText.Format |= TextFormatFlags.TextBoxControl;

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

//        public TextFormatFlags TextFlags 
//        {
//            get => _uiText.Format;
//            set => _uiText.Format = value;
//        }

//        protected internal override void RegisterInputs(InputInterface input)
//        {
//            base.RegisterInputs(input);
//        }
//    }
//}