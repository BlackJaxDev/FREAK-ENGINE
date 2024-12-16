namespace XREngine.Rendering.UI
{
    public partial class UIListTransform
    {
        public class UIListChildPlacementInfo(UITransform owner) : UIChildPlacementInfo(owner)
        {
            private int _index;
            public int Index
            {
                get => _index;
                set => SetField(ref _index, value);
            }
        }
    }
}