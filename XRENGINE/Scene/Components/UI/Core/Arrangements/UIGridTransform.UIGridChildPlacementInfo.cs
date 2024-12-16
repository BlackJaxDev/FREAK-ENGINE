using System.ComponentModel;

namespace XREngine.Rendering.UI
{
    public partial class UIGridTransform
    {
        public class UIGridChildPlacementInfo(UITransform owner) : UIChildPlacementInfo(owner)
        {
            private int _row = 0;
            private int _column = 0;
            private int _rowSpan = 1;
            private int _columnSpan = 1;

            public int Row
            {
                get => _row;
                set => SetField(ref _row, value);
            }
            public int Column
            {
                get => _column;
                set => SetField(ref _column, value);
            }
            public int RowSpan
            {
                get => _rowSpan;
                set => SetField(ref _rowSpan, value);
            }
            public int ColumnSpan
            {
                get => _columnSpan;
                set => SetField(ref _columnSpan, value);
            }

            public HashSet<int> AssociatedRowIndices { get; } = [];
            public HashSet<int> AssociatedColumnIndices { get; } = [];
        }
    }
}