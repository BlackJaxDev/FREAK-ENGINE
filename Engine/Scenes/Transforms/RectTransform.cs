using XREngine.Data.Transforms.Vectors;

namespace XREngine.Scenes.Transforms
{
    public class RectTransform : Transform
    {
        public Vec2 AnchoredPosition { get; set; }
        public Vec2 AnchorMin { get; set; }
        public Vec2 AnchorMax { get; set; }
        public Vec2 OffsetMin { get; set; }
        public Vec2 OffsetMax { get; set; }
        public Vec2 Pivot { get; set; }
        public Vec2 SizeDelta { get; set; }

        public RectTransform()
        {
            AnchoredPosition = Vec2.Zero;
            AnchorMin = new Vec2(0.5f, 0.5f);
            AnchorMax = new Vec2(0.5f, 0.5f);
            OffsetMin = Vec2.Zero;
            OffsetMax = Vec2.Zero;
            Pivot = new Vec2(0.5f, 0.5f);
            SizeDelta = Vec2.Zero;
        }

        public float Width
        {
            get => SizeDelta.x;
            set => SizeDelta = new Vec2(value, SizeDelta.y);
        }

        public float Height
        {
            get => SizeDelta.y;
            set => SizeDelta = new Vec2(SizeDelta.x, value);
        }

        public Vec3[] GetWorldCorners()
        {
            Vec3[] corners = new Vec3[4];

            // Calculate the width and height of the rect
            float width = Width + OffsetMax.x - OffsetMin.x;
            float height = Height + OffsetMax.y - OffsetMin.y;

            // Calculate the corners
            corners[0] = new Vec3(AnchoredPosition.x + OffsetMin.x, AnchoredPosition.y + OffsetMin.y, 0); // bottom left
            corners[1] = new Vec3(AnchoredPosition.x + OffsetMin.x, AnchoredPosition.y + height, 0); // top left
            corners[2] = new Vec3(AnchoredPosition.x + width, AnchoredPosition.y + height, 0); // top right
            corners[3] = new Vec3(AnchoredPosition.x + width, AnchoredPosition.y + OffsetMin.y, 0); // bottom right

            return corners;
        }
    }
}
