//using XREngine.Rendering.Commands;

//namespace XREngine.Data.Rendering
//{
//    public class RenderCommandMethod2D(int renderPass, Action render) : RenderCommand2D(renderPass)
//    {
//        public Action Rendered { get; set; } = render;
//        public override void Render(bool shadowPass) => Rendered?.Invoke();
//    }
//}