using ImageMagick;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Trees;
using XREngine.Input.Devices;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene;
using XREngine.Scene.Transforms;

namespace XREngine.Editor;

public class EditorFlyingCameraPawnComponent : FlyingCameraPawnComponent, IRenderable
{
    public EditorFlyingCameraPawnComponent()
    {
        _postRenderRC = new((int)EDefaultRenderPass.PostRender, PostRender);
        _renderHighlightRC = new((int)EDefaultRenderPass.OpaqueForward, RenderHighlight);
        RenderedObjects = 
        [
            RenderInfo3D.New(this, _postRenderRC),
            RenderInfo3D.New(this, _renderHighlightRC)
        ];
    }

    private void RenderHighlight(bool shadowPass)
    {
        if (shadowPass)
            return;

        if (_hitTriangle is not null)
        {
            //Debug.Out($"Hit triangle: {_hitTriangle}");
            Engine.Rendering.Debug.RenderTriangle(_hitTriangle.Value, ColorF4.Yellow, true);
        }
    }

    private readonly RenderCommandMethod3D _postRenderRC;
    private readonly RenderCommandMethod3D _renderHighlightRC;
    private List<SceneNode>? _lastSelection = null;
    private int _lastHitIndex = 0;

    public RenderInfo[] RenderedObjects { get; }

    static void ScreenshotCallback(MagickImage img)
        => img?.Write(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "screenshot.png"));

    private void PostRender(bool shadowPass)
    {
        var rend = AbstractRenderer.Current;
        if (rend is null || Engine.Rendering.State.PipelineState?.WindowViewport is null)
            return;

        if (_wantsScreenshot)
        {
            _wantsScreenshot = false;
            rend.GetScreenshotAsync(Engine.Rendering.State.PipelineState.WindowViewport.Region, false, ScreenshotCallback);
        }

        var cam = GetCamera();
        if (cam is null)
            return;

        var input = LocalInput;
        if (input is null)
            return;

        var pos = input?.Mouse?.CursorPosition;
        if (pos is null)
            return;

        rend.BindFrameBuffer(EFramebufferTarget.ReadFramebuffer, 0);
        //rend.SetReadBuffer(EDrawBuffersAttachment.None);
        float? depth = rend.GetDepth(pos.Value.X, pos.Value.Y);
        if (depth is not null && depth.Value > 0.0f && depth.Value < 1.0f)
        {
            Debug.Out($"Depth: {depth}");
        }
    }

    protected override void MouseRotate(float x, float y)
    {
        //Rotate about selected nodes, if any
        if (Selection.SceneNodes.Length > 0)
        {
            Vector3 avgPoint = Vector3.Zero;
            foreach (var node in Selection.SceneNodes)
                avgPoint += node.Transform.WorldTranslation;
            avgPoint /= Selection.SceneNodes.Length;
            ArcBallRotate(y, x, avgPoint);
        }
        else
            base.MouseRotate(x, y);
    }

    protected override void Tick()
    {
        base.Tick();
        Highlight();
        if (CtrlPressed && (LocalInput?.Keyboard?.GetKeyState(EKey.S, EButtonInputType.Pressed) ?? false))
            QueueScreenshot();
    }

    private bool _wantsScreenshot = false;
    private void QueueScreenshot()
        => _wantsScreenshot = true;

    public override void RegisterInput(InputInterface input)
    {
        base.RegisterInput(input);
        input.RegisterMouseButtonEvent(EMouseButton.LeftClick, EButtonInputType.Pressed, Select);
    }

    private SortedDictionary<float, ITreeItem>? _lastRaycast = null;
    private readonly object _raycastLock = new();
    private Triangle? _hitTriangle = null;
    private TransformBase? _hitTransform = null;

    private void Highlight()
    {
        if (World is null)
        {
            lock (_raycastLock)
                _lastRaycast = null;
            return;
        }

        var cam = GetCamera();
        if (cam is null)
            return;

        var input = LocalInput;
        if (input is null)
            return;

        var pos = input?.Mouse?.CursorPosition;
        if (pos is null)
            return;

        Task.Run(() => Raycast(cam, pos.Value));
    }

    private void Raycast(CameraComponent cam, Vector2 pos)
    {
        var result = World!.Raycast(cam, pos, out _hitTriangle, out _hitTransform);
        lock (_raycastLock)
            _lastRaycast = result;

        if (_hitTransform is not null)
            Debug.Out($"Hit transform: {_hitTransform}");
    }

    private void Select()
    {
        var input = LocalInput;
        if (input is null)
            return;

        var lc = input?.Mouse?.LeftClick;
        if (lc is null)
            return;

        if (!lc.GetState(EButtonInputType.Pressed))
            return;

        List<SceneNode> currentHits = [];
        lock (_raycastLock)
        {
            if (_lastRaycast is not null)
            {
                foreach (var hit in _lastRaycast.Values)
                {
                    if (hit is not RenderInfo3D ri)
                        continue;
                    if (ri.Owner is not XRComponent comp)
                        continue;
                    if (comp.SceneNode is null)
                        continue;
                    currentHits.Add(comp.SceneNode);
                }
            }
        }

        SceneNode? node = null;
        if (_lastSelection is null)
        {
            _lastSelection = currentHits;
            _lastHitIndex = 0;
            if (currentHits.Count >= 1)
                Selection.SceneNodes = [currentHits[_lastHitIndex = 0]];
            else
                Selection.SceneNodes = [];
            return;
        }

        //intersect with the last hit values to see if we are still hitting the same thing
        bool sameNodes = currentHits.Count > 0 && currentHits.Intersect(_lastSelection).Count() == currentHits.Count;
        if (sameNodes)
        {
            //cycle the selection
            _lastHitIndex = (_lastHitIndex + 1) % currentHits.Count;
            node = currentHits[_lastHitIndex];
        }

        if (node is null)
            return;

        if (!ApplyModifiers(input, node))
            Selection.SceneNodes = [node];
    }

    private static bool ApplyModifiers(LocalInputInterface? input, SceneNode node)
    {
        var kbd = input?.Keyboard;
        if (kbd is null)
            return false;
        
        //control is toggle, alt is remove, shift is add

        if (kbd.GetKeyState(EKey.ControlLeft, EButtonInputType.Pressed) ||
            kbd.GetKeyState(EKey.ControlRight, EButtonInputType.Pressed))
        {
            if (Selection.SceneNodes.Contains(node))
                RemoveNode(node);
            else
                AddNode(node);

            return true;
        }
        else if (kbd.GetKeyState(EKey.AltLeft, EButtonInputType.Pressed) ||
                 kbd.GetKeyState(EKey.AltRight, EButtonInputType.Pressed))
        {
            RemoveNode(node);
            return true;
        }
        else if (kbd.GetKeyState(EKey.ShiftLeft, EButtonInputType.Pressed) ||
                 kbd.GetKeyState(EKey.ShiftRight, EButtonInputType.Pressed))
        {
            AddNode(node);
            return true;
        }

        return false;
    }

    private static void AddNode(SceneNode node)
    {
        if (Selection.SceneNodes.Contains(node))
            return;

        Selection.SceneNodes = [.. Selection.SceneNodes, node];
    }

    private static void RemoveNode(SceneNode node)
    {
        if (!Selection.SceneNodes.Contains(node))
            return;

        Selection.SceneNodes = Selection.SceneNodes.Where(n => n != node).ToArray();
    }
}
