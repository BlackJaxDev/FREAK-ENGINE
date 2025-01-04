using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.UI;
using XREngine.Scene;

namespace XREngine.Editor;

public partial class HierarchyPanel : EditorPanel
{
    public List<SceneNode> RootNodes { get; set; } = [];

    private const float Margin = 4.0f;

    protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
    {
        base.OnPropertyChanged(propName, prev, field);
        switch (propName)
        {
            case nameof(RootNodes):
                RemakeChildren();
                break;
        }
    }

    protected override void OnComponentActivated()
    {
        base.OnComponentActivated();
        RemakeChildren();
    }
    protected override void OnComponentDeactivated()
    {
        base.OnComponentDeactivated();
        SceneNode.Transform.Clear();
    }

    public void RemakeChildren()
    {
        SceneNode.Transform.Clear();
        //Create the root menu transform - this is a horizontal list of buttons.
        CreateTree(SceneNode, this);
    }

    private XRMaterial? _menuBackgroundMaterial;
    public XRMaterial MenuBackgroundMaterial
    {
        get => _menuBackgroundMaterial ??= MakeBackgroundMaterial();
        set => SetField(ref _menuBackgroundMaterial, value);
    }

    private static XRMaterial MakeBackgroundMaterial()
    {
        var floorShader = ShaderHelper.LoadEngineShader("Misc\\TestFloor.frag");
        ShaderVar[] floorUniforms =
        [
            new ShaderVector4(ColorF4.Charcoal, "MatColor"),
            new ShaderFloat(10.0f, "BlurStrength"),
            new ShaderInt(20, "SampleCount"),
            new ShaderVector3(Globals.Backward, "PlaneNormal"),
        ];
        XRTexture2D grabTex = XRTexture2D.CreateGrabPassTextureResized(0.2f);
        var floorMat = new XRMaterial(floorUniforms, [grabTex], floorShader);
        floorMat.RenderOptions.CullMode = ECullMode.None;
        floorMat.RenderOptions.RequiredEngineUniforms = EUniformRequirements.Camera;
        floorMat.RenderPass = (int)EDefaultRenderPass.TransparentForward;
        return floorMat;
    }

    public float ItemHeight { get; set; } = 30.0f;

    public partial class NodeWrapper(SceneNode node) : UIComponent
    {
        private bool _highlighted;

        public SceneNode Node { get; set; } = node;
        public bool IsRoot => Node?.Parent is null;
        public bool IsLeaf => (Node?.Transform?.Children?.Count ?? 0) <= 0;
        public bool Highlighted
        {
            get => _highlighted;
            set => SetField(ref _highlighted, value);
        }
        public int Depth => Node?.Transform?.Depth ?? 0;
    }

    private void CreateTree(SceneNode parentNode, HierarchyPanel hierarchyPanel)
    {
        var listNode = parentNode.NewChild<UIMaterialComponent>(out var menuMat);
        menuMat.Material = MenuBackgroundMaterial;
        var listTfm = listNode.SetTransform<UIListTransform>();
        listTfm.DisplayHorizontal = false;
        listTfm.ItemSpacing = 0;
        listTfm.Padding = new Vector4(0.0f);
        listTfm.ItemAlignment = EListAlignment.TopOrLeft;
        listTfm.ItemSize = ItemHeight;
        listTfm.Width = null;
        listTfm.Height = null;
        CreateNodes(listNode, RootNodes);
    }

    private static void CreateNodes(SceneNode listNode, IEnumerable<SceneNode> nodes)
    {
        var copy = nodes.Where(x => x is not null).ToList();
        foreach (SceneNode node in copy)
        {
            var buttonNode = listNode.NewChild<UIButtonComponent, UIMaterialComponent>(out var button, out var background);
            button.Name = node.Name;

            var mat = XRMaterial.CreateUnlitColorMaterialForward(ColorF4.Transparent);
            mat.EnableTransparency();
            background.Material = mat;

            var buttonTfm = buttonNode.GetTransformAs<UIBoundableTransform>(true)!;
            buttonTfm.MaxAnchor = new Vector2(0.0f, 1.0f);
            buttonTfm.Margins = new Vector4(0.0f);

            buttonNode.NewChild<UITextComponent>(out var text);

            var textTfm = text.BoundableTransform;
            textTfm.Margins = new Vector4(10.0f, Margin, 10.0f, Margin);

            text.FontSize = 14;
            text.Text = node.Name;
            text.Color = ColorF4.Gray;
            textTfm.Translation = new Vector2(node.Transform.Depth * 15, 0.0f);
            textTfm.Width = null;
            textTfm.MaxAnchor = new Vector2(0.0f, 1.0f);
            text.HorizontalAlignment = EHorizontalAlignment.Left;
            text.VerticalAlignment = EVerticalAlignment.Center;

            CreateNodes(listNode, node.Transform.Children.Select(x => x.SceneNode!));
        }
    }
}
