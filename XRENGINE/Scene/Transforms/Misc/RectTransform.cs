using System.Numerics;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Represents a transform for a UI element.
    /// </summary>
    public class RectTransform : Transform
    {
        private Vector2 anchoredPosition;
        private Vector2 anchorMin;
        private Vector2 anchorMax;
        private Vector2 offsetMin;
        private Vector2 offsetMax;
        private Vector2 pivot;
        private Vector2 sizeDelta;

        public Vector2 AnchoredPosition
        {
            get => anchoredPosition;
            set => SetField(ref anchoredPosition, value);
        }
        public Vector2 AnchorMin
        {
            get => anchorMin;
            set => SetField(ref anchorMin, value);
        }
        public Vector2 AnchorMax
        {
            get => anchorMax;
            set => SetField(ref anchorMax, value);
        }
        public Vector2 OffsetMin
        {
            get => offsetMin;
            set => SetField(ref offsetMin, value);
        }
        public Vector2 OffsetMax
        {
            get => offsetMax;
            set => SetField(ref offsetMax, value);
        }
        public Vector2 Pivot
        {
            get => pivot;
            set => SetField(ref pivot, value);
        }
        public Vector2 SizeDelta
        {
            get => sizeDelta;
            set => SetField(ref sizeDelta, value);
        }

        public RectTransform() : this(null) { }
        public RectTransform(TransformBase? parent) : base(parent)
        {
            AnchoredPosition = Vector2.Zero;
            AnchorMin = new Vector2(0.5f, 0.5f);
            AnchorMax = new Vector2(0.5f, 0.5f);
            OffsetMin = Vector2.Zero;
            OffsetMax = Vector2.Zero;
            Pivot = new Vector2(0.5f, 0.5f);
            SizeDelta = Vector2.Zero;
        }

        public float Width
        {
            get => SizeDelta.X;
            set => SizeDelta = new Vector2(value, SizeDelta.Y);
        }

        public float Height
        {
            get => SizeDelta.Y;
            set => SizeDelta = new Vector2(SizeDelta.X, value);
        }

        public Vector3[] GetWorldCorners()
        {
            Vector3[] corners = new Vector3[4];

            float width = Width + OffsetMax.X - OffsetMin.X;
            float height = Height + OffsetMax.Y - OffsetMin.Y;

            corners[0] = new Vector3(AnchoredPosition.X + OffsetMin.X, AnchoredPosition.Y + OffsetMin.Y, 0); // bottom left
            corners[1] = new Vector3(AnchoredPosition.X + OffsetMin.X, AnchoredPosition.Y + height, 0); // top left
            corners[2] = new Vector3(AnchoredPosition.X + width, AnchoredPosition.Y + height, 0); // top right
            corners[3] = new Vector3(AnchoredPosition.X + width, AnchoredPosition.Y + OffsetMin.Y, 0); // bottom right

            return corners;
        }
    }
}
