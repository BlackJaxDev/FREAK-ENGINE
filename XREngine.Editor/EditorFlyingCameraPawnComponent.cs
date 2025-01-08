using Extensions;
using ImageMagick;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Colors;
using XREngine.Data.Core;
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

public partial class EditorFlyingCameraPawnComponent : FlyingCameraPawnComponent, IRenderable
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

    //These two drag points diverge if the camera moves, so they're both stored initially at mouse down
    public Vector3? NormalizedViewportDragPoint { get; set; } = null;
    public Vector3? WorldDragPoint { get; set; } = null;

    protected override void OnRightClick(bool pressed)
    {
        base.OnRightClick(pressed);
        if (pressed)
        {
            NormalizedViewportDragPoint = DepthHitNormalizedViewportPoint;
            WorldDragPoint = DepthHitNormalizedViewportPoint.HasValue
                ? Viewport?.NormalizedViewportToWorldCoordinate(DepthHitNormalizedViewportPoint.Value)
                : null;
        }
        else
        {
            NormalizedViewportDragPoint = null;
            WorldDragPoint = null;
        }
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
        //if ((WorldDragPoint.HasValue || DepthHitNormalizedViewportPoint.HasValue) && Viewport is not null)
        //{
        //    Vector3 pos;
        //    if (WorldDragPoint.HasValue)
        //        pos = WorldDragPoint.Value;
        //    else
        //        pos = Viewport.NormalizedViewportToWorldCoordinate(DepthHitNormalizedViewportPoint!.Value);
        //    Engine.Rendering.Debug.RenderSphere(pos, (Viewport.Camera?.DistanceFromWorldPosition(pos) ?? 1.0f) * 0.1f, false, ColorF4.Yellow, true);
        //}
        if (RenderFrustum)
        {
            var cam = GetCamera();
            if (cam is not null)
                Engine.Rendering.Debug.RenderFrustum(cam.Camera.WorldFrustum(), ColorF4.Red);
        }
    }

    private readonly RenderCommandMethod3D _postRenderRC;
    private readonly RenderCommandMethod3D _renderHighlightRC;
    private List<SceneNode>? _lastSelection = null;
    private int _lastHitIndex = 0;

    public RenderInfo[] RenderedObjects { get; }

    static void ScreenshotCallback(MagickImage img)
        => img?.Write(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "screenshot.png"));

    public Vector3? LastDepthHitNormalizedViewportPoint => _lastDepthHitNormalizedViewportPoint;
    public Vector3? DepthHitNormalizedViewportPoint
    {
        get => _depthHitNormalizedViewportPoint;
        private set
        {
            _lastDepthHitNormalizedViewportPoint = _depthHitNormalizedViewportPoint;
            _depthHitNormalizedViewportPoint = value;
        }
    }
    public float LastHitDistance => XRMath.DepthToDistance(DepthHitNormalizedViewportPoint.HasValue ? DepthHitNormalizedViewportPoint.Value.Z : 0.0f, NearZ, FarZ);
    public float NearZ => GetCamera()?.Camera.NearZ ?? 0.0f;
    public float FarZ => GetCamera()?.Camera.FarZ ?? 0.0f;

    public bool RenderFrustum { get; set; } = false;
    public bool RenderRaycast { get; set; } = false;

    private readonly SortedDictionary<float, List<(XRComponent item, object? data)>> _lastPickResults = [];
    private void PostRender(bool shadowPass)
    {
        var rend = AbstractRenderer.Current;
        if (rend is null)
            return;

        var vp = Viewport;
        if (vp is null)
            return;

        if (_wantsScreenshot)
        {
            _wantsScreenshot = false;
            rend.GetScreenshotAsync(vp.Region, false, ScreenshotCallback);
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

        Vector2 p = pos.Value;
        p.Y = vp.Height - p.Y;
        p = vp.ScreenToViewportCoordinate(p);
        p = vp.ViewportToInternalCoordinate(p);

        var fbo = vp.RenderPipelineInstance?.GetFBO<XRFrameBuffer>(DefaultRenderPipeline.ForwardPassFBOName);
        float? depth = vp.GetDepth(fbo!, p);

        p = vp.NormalizeInternalCoordinate(p);
        DepthHitNormalizedViewportPoint = depth is not null && depth.Value > 0.0f && depth.Value < 1.0f ? new Vector3(p.X, p.Y, depth.Value) : null;

        _lastRaycastSegment = vp.GetWorldSegment(p);

        vp.PickScene(p, false, true, true, _lastPickResults);
        //Task.Run(() => SetRaycastResult(orderedResults));

        //if (RenderRaycast)
        //{
        //    Vector3 start = _lastRaycastSegment.Start;
        //    Vector3 end = _lastRaycastSegment.End;
        //    Engine.Rendering.Debug.RenderLine(start, end, ColorF4.Magenta, false);
        //}

        ApplyTransformations(vp);
    }

    private void ApplyTransformations(XRViewport vp)
    {
        var tfm = TransformAs<Transform>();
        if (tfm is null)
            return;

        if (_lastScrollDelta.HasValue && DepthHitNormalizedViewportPoint.HasValue)
        {
            //Zoom towards the hit point
            float scrollSpeed = _lastScrollDelta.Value;
            _lastScrollDelta = null;
            Vector3 worldCoord = vp.NormalizedViewportToWorldCoordinate(DepthHitNormalizedViewportPoint.Value);
            float dist = Transform.WorldTranslation.Distance(worldCoord);
            tfm.Translation = Segment.PointAtLineDistance(Transform.WorldTranslation, worldCoord, scrollSpeed * dist * 0.1f * ScrollSpeed);
        }
        if (_lastMouseTranslationDelta.HasValue && WorldDragPoint.HasValue && NormalizedViewportDragPoint.HasValue)
        {
            Vector3 normCoord = NormalizedViewportDragPoint.Value;
            Vector3 worldCoord = vp.NormalizedViewportToWorldCoordinate(normCoord);
            Vector2 screenCoord = vp.DenormalizeViewportCoordinate(normCoord.XY());
            Vector2 newScreenCoord = screenCoord + _lastMouseTranslationDelta.Value;
            _lastMouseTranslationDelta = null;
            Vector3 newNormCoord = new(vp.NormalizeViewportCoordinate(newScreenCoord), normCoord.Z);
            Vector3 worldDelta = vp.NormalizedViewportToWorldCoordinate(newNormCoord) - worldCoord;
            tfm.ApplyTranslation(worldDelta);
        }
        if (_lastRotateDelta.HasValue)
        {
            if (_lastRotatePoint.HasValue)
            {
                float x = _lastRotateDelta.Value.X;
                float y = _lastRotateDelta.Value.Y;
                ArcBallRotate(y, x, _lastRotatePoint.Value);
                _lastRotateDelta = null;
            }
            else if (WorldDragPoint.HasValue)
            {
                Vector3 worldCoord = WorldDragPoint.Value;
                float x = _lastRotateDelta.Value.X;
                float y = _lastRotateDelta.Value.Y;
                ArcBallRotate(y, x, worldCoord);
                _lastRotateDelta = null;
            }
        }
    }

    private void SetRaycastResult(SortedDictionary<float, List<(ITreeItem item, object? data)>>? result)
    {
        lock (_raycastLock)
            _lastRaycast = result;

        if (result is null)
            return;

        foreach ((var dist, List<(ITreeItem item, object? data)> list) in result)
        {
            foreach (var (i, _) in list)
                Debug.Out($"Hit: {i as RenderInfo} {dist}");
        }
    }
    private Vector2? _lastRotateDelta = null;
    private Vector3? _lastRotatePoint = null;
    protected override void MouseRotate(float x, float y)
    {
        if (WorldDragPoint.HasValue)
        {
            _lastRotateDelta = new Vector2(-x * MouseRotateSpeed, y * MouseRotateSpeed);
        }
        else if (Selection.SceneNodes.Length > 0)
        {
            Vector3 avgPoint = Vector3.Zero;
            foreach (var node in Selection.SceneNodes)
                avgPoint += node.Transform.WorldTranslation;
            avgPoint /= Selection.SceneNodes.Length;

            _lastRotatePoint = avgPoint;
            _lastRotateDelta = new Vector2(-x * MouseRotateSpeed, y * MouseRotateSpeed);
        }
        else
            base.MouseRotate(x, y);
    }
    private Vector2? _lastMouseTranslationDelta = null;
    protected override void MouseTranslate(float x, float y)
    {
        if (WorldDragPoint.HasValue)
        {
            //This fixes stationary jitter caused by float imprecision
            //when recalculating the same hit point every update
            if (Math.Abs(x) < 0.00001f &&
                Math.Abs(y) < 0.00001f)
                return;

            _lastMouseTranslationDelta = new Vector2(-x, -y);
        }
        else
            base.MouseTranslate(x, y);
    }

    private float? _lastScrollDelta = null;
    protected override void OnScrolled(float diff)
    {
        if (DepthHitNormalizedViewportPoint is not null)
            _lastScrollDelta = diff;
        else
            base.OnScrolled(diff);
    }

    protected override void Tick()
    {
        base.Tick();
        Highlight();
        if (CtrlPressed && (Keyboard?.Pressed(EKey.S) ?? false))
            TakeScreenshot();
    }

    private bool _wantsScreenshot = false;

    /// <summary>
    /// Takes a screenshot of the current viewport and saves it to the desktop.
    /// </summary>
    public void TakeScreenshot()
        => _wantsScreenshot = true;

    public override void RegisterInput(InputInterface input)
    {
        base.RegisterInput(input);
        input.RegisterMouseButtonEvent(EMouseButton.LeftClick, EButtonInputType.Pressed, Select);
    }

    private SortedDictionary<float, List<(ITreeItem item, object? data)>>? _lastRaycast = null;
    private readonly object _raycastLock = new();
    private Triangle? _hitTriangle = null;
    private TransformBase? _hitTransform = null;

    private void Highlight()
    {
        if (World is not null && Viewport is not null)
        {
            var cam = GetCamera();
            if (cam is not null)
            {
                var input = LocalInput;
                if (input is not null)
                {
                    var pos = input?.Mouse?.CursorPosition;
                    if (pos is not null)
                    {
                        Raycast(pos.Value);
                        return;
                    }
                }
            }
        }

        lock (_raycastLock)
            _lastRaycast = null;
    }

    private Segment _lastRaycastSegment = new(Vector3.Zero, Vector3.Zero);
    private Vector3? _depthHitNormalizedViewportPoint = null;
    private Vector3? _lastDepthHitNormalizedViewportPoint = null;

    private void Raycast(Vector2 screenPoint)
    {
        if (World is null || Viewport is null)
            return;

        //This code only works on the render thread, and I honestly don't understand why the math fails here but not there lol
        screenPoint.Y = Viewport.Height - screenPoint.Y;
        _lastRaycastSegment = Viewport.GetWorldSegment(screenPoint);
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
                foreach (var x in _lastRaycast.Values)
                {
                    foreach (var (item, _) in x)
                    {
                        if (item is not RenderInfo3D ri)
                            continue;
                        if (ri.Owner is not XRComponent comp)
                            continue;
                        if (comp.SceneNode is null)
                            continue;
                        currentHits.Add(comp.SceneNode);
                    }
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
