using System.Numerics;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Represents a transform for a UI element.
    /// </summary>
    public class RectTransform : Transform
    {
        public Vector2 AnchoredPosition { get; set; }
        public Vector2 AnchorMin { get; set; }
        public Vector2 AnchorMax { get; set; }
        public Vector2 OffsetMin { get; set; }
        public Vector2 OffsetMax { get; set; }
        public Vector2 Pivot { get; set; }
        public Vector2 SizeDelta { get; set; }

        public RectTransform()
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
