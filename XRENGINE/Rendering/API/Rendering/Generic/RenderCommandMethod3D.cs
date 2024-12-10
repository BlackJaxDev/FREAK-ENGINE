namespace XREngine.Rendering.Commands
{
    public class RenderCommandMethod3D(int renderPass, RenderCommandMethod3D.DelRender render) : RenderCommand3D(renderPass)
    {
        public delegate void DelRender(bool shadowPass);

        public DelRender Rendered { get; set; } = render;

        public override void Render(bool shadowPass)
            => Rendered?.Invoke(shadowPass);
    }
}
