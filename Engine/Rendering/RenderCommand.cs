namespace XREngine.Rendering
{
    public abstract class RenderCommand : IComparable<RenderCommand>, IComparable
    {
        public RenderCommand() { }
        public RenderCommand(ERenderPass renderPass) => RenderPass = renderPass;

        public ERenderPass RenderPass { get; set; } = ERenderPass.OpaqueForward;

        public abstract int CompareTo(RenderCommand? other);
        public int CompareTo(object? obj) => CompareTo(obj as RenderCommand);
        public abstract void Render(bool shadowPass);
    }
    //public abstract class RenderCommand2D : RenderCommand
    //{
    //    public int ZIndex { get; set; }

    //    public override int CompareTo(RenderCommand? other)
    //        => ZIndex < ((other as RenderCommand2D)?.ZIndex ?? 0) ? -1 : 1;

    //    public RenderCommand2D() : base(ERenderPass.OpaqueForward) { }
    //    public RenderCommand2D(ERenderPass renderPass) : base(renderPass) { }
    //    public RenderCommand2D(ERenderPass renderPass, int zIndex) : base(renderPass) => ZIndex = zIndex;
    //}
    //public class RenderCommandMethod2D : RenderCommand2D
    //{
    //    //public RenderCommandMethod2D() { }
    //    //public RenderCommandMethod2D(Action render) : base() => Rendered = render;
    //    public RenderCommandMethod2D(ERenderPass renderPass, Action render) : base(renderPass) => Rendered = render;

    //    public Action Rendered { get; set; }
    //    public override void Render(bool shadowPass) => Rendered?.Invoke();
    //}
    //public class RenderCommandMethod3D : RenderCommand3D
    //{
    //    //public RenderCommandMethod3D() { }
    //    //public RenderCommandMethod3D(Action render) : base() => Rendered = render;
    //    public RenderCommandMethod3D(ERenderPass renderPass, DelRender render) : base(renderPass) => Rendered = render;

    //    public delegate void DelRender(bool shadowPass);
    //    public DelRender Rendered { get; set; }
    //    public override void Render(bool shadowPass) => Rendered?.Invoke(shadowPass);
    //}
    //public class RenderCommandMesh3D : RenderCommand3D
    //{
    //    [Category("Render Command")]
    //    public MeshRenderer Mesh { get; set; }
    //    [Browsable(false)]
    //    public Matrix4 WorldMatrix { get; set; } = Matrix4.Identity;
    //    [Browsable(false)]
    //    public Matrix3 NormalMatrix { get; set; } = Matrix3.Identity;
    //    public TMaterial MaterialOverride { get; set; }

    //    public RenderCommandMesh3D() : base() { }
    //    public RenderCommandMesh3D(ERenderPass renderPass) : base(renderPass) { }
    //    public RenderCommandMesh3D(
    //        ERenderPass renderPass,
    //        MeshRenderer manager,
    //        Matrix4 worldMatrix,
    //    Matrix3 normalMatrix,
    //    float renderDistance,
    //        TMaterial materialOverride = null) : base(renderPass, renderDistance)
    //    {
    //        Mesh = manager;
    //        WorldMatrix = worldMatrix;
    //        NormalMatrix = normalMatrix;
    //        MaterialOverride = materialOverride;
    //    }

    //    public override void Render(bool shadowPass)
    //    {
    //        if (shadowPass && Mesh?.TargetMesh?.Triangles is null)
    //            return;

    //        Mesh?.Render(WorldMatrix, NormalMatrix, MaterialOverride);
    //    }
    //}
    //public class RenderCommandMesh2D : RenderCommand2D
    //{
    //    private MeshRenderer _mesh;
    //    public MeshRenderer Mesh
    //    {
    //        get => _mesh;
    //        set
    //        {
    //            _mesh?.Dispose();
    //            _mesh = value;
    //        }
    //    }
    //    public Matrix4 WorldMatrix { get; set; } = Matrix4.Identity;
    //    public TMaterial MaterialOverride { get; set; }

    //    public RenderCommandMesh2D() : base() { }
    //    public RenderCommandMesh2D(ERenderPass renderPass) : base(renderPass) { }
    //    public RenderCommandMesh2D(
    //        ERenderPass renderPass,
    //        MeshRenderer manager,
    //    Matrix4 worldMatrix,
    //    int zIndex,
    //        TMaterial materialOverride = null) : base(renderPass, zIndex)
    //    {
    //        RenderPass = renderPass;
    //        Mesh = manager;
    //        WorldMatrix = worldMatrix;
    //        MaterialOverride = materialOverride;
    //    }

    //    public override void Render(bool shadowPass)
    //    {
    //        if (shadowPass && Mesh?.TargetMesh?.Triangles is null)
    //            return;

    //        Mesh?.Render(WorldMatrix, Matrix3.Identity, MaterialOverride);
    //    }
    //}
    //public class RenderCommandViewport : RenderCommandMesh2D
    //{
    //    public Viewport Viewport { get; set; }
    //    public MaterialFrameBuffer Framebuffer { get; set; }

    //    //public RenderCommandViewport() : base() { }
    //    public RenderCommandViewport(ERenderPass renderPass) : base(renderPass) { }
    //    public RenderCommandViewport(
    //        ERenderPass renderPass,
    //        Viewport viewport,
    //        MeshRenderer quad,
    //        MaterialFrameBuffer viewportFBO,
    //        Matrix4 worldMatrix,
    //        int zIndex)
    //        : base(renderPass, quad, worldMatrix, zIndex, null)
    //    {
    //        Viewport = viewport;
    //        Framebuffer = viewportFBO;
    //    }

    //    public override void Render(bool shadowPass)
    //    {
    //        //TODO: viewport renders all viewed items to the framebuffer,
    //        //But this method is called within the parent's rendering to its framebuffer.
    //        Viewport.Render(Framebuffer);
    //        FrameBuffer.CurrentlyBound?.Bind(EFramebufferTarget.DrawFramebuffer);
    //        base.Render(shadowPass);
    //    }
    //}
}
