using System.Numerics;
using XREngine.Components;
using XREngine.Input.Devices;
using XREngine.Rendering.Info;
using XREngine.Scene;

namespace XREngine.Editor;

public class EditorFlyingCameraPawnComponent : FlyingCameraPawnComponent
{
    protected override void MouseRotate(float x, float y)
    {
        //Rotate about selected nodes, if any
        if (Selection.SelectedNodes.Length > 0)
        {
            Vector3 avgPoint = Vector3.Zero;
            foreach (var node in Selection.SelectedNodes)
                avgPoint += node.Transform.WorldTranslation;
            avgPoint /= Selection.SelectedNodes.Length;
            ArcBallRotate(y, x, avgPoint);
        }
        else
            base.MouseRotate(x, y);
    }

    protected override void Tick()
    {
        base.Tick();
        DoHitSelection();
    }

    private List<SceneNode>? _lastHits = null;
    private int _lastHitIndex = 0;

    private void DoHitSelection()
    {
        var cam = GetCamera();
        if (cam is null)
            return;

        var input = LocalInput;
        if (input is null)
            return;

        var lc = input?.Mouse?.LeftClick;
        if (lc is null)
            return;

        if (!lc.GetState(EButtonInputType.Pressed))
            return;

        var pos = input?.Mouse?.CursorPosition;
        if (pos is null)
            return;

        var hits = World?.Raycast(cam, pos.Value);
        if (hits is null)
            return;

        List<SceneNode> currentHits = [];
        foreach (var hit in hits.Values)
        {
            if (hit is not RenderInfo3D ri)
                continue;
            if (ri.Owner is not XRComponent comp)
                continue;
            if (comp.SceneNode is null)
                continue;
            currentHits.Add(comp.SceneNode);
        }

        SceneNode? node = null;
        if (_lastHits is null)
        {
            _lastHits = currentHits;
            _lastHitIndex = 0;
            if (currentHits.Count >= 1)
                Selection.SelectedNodes = [currentHits[_lastHitIndex = 0]];
            else
                Selection.SelectedNodes = [];
            return;
        }

        //intersect with the last hit values to see if we are still hitting the same thing
        bool sameNodes = currentHits.Intersect(_lastHits).Count() == hits.Count;
        if (sameNodes)
        {
            //cycle the selection
            _lastHitIndex = (_lastHitIndex + 1) % currentHits.Count;
            node = currentHits[_lastHitIndex];
        }

        if (node is null)
            return;

        var kbd = input?.Keyboard;
        if (kbd is not null)
        {
            //control is toggle, alt is remove, shift is add

            if (kbd.GetKeyState(EKey.ControlLeft, EButtonInputType.Pressed) || 
                kbd.GetKeyState(EKey.ControlRight, EButtonInputType.Pressed))
            {
                if (Selection.SelectedNodes.Contains(node))
                    RemoveNode(node);
                else
                    AddNode(node);
                return;

            }
            else if (kbd.GetKeyState(EKey.AltLeft, EButtonInputType.Pressed) ||
                     kbd.GetKeyState(EKey.AltRight, EButtonInputType.Pressed))
            {
                RemoveNode(node);
                return;
            }
            else if (kbd.GetKeyState(EKey.ShiftLeft, EButtonInputType.Pressed) ||
                     kbd.GetKeyState(EKey.ShiftRight, EButtonInputType.Pressed))
            {
                AddNode(node);
                return;
            }
        }

        Selection.SelectedNodes = [node];
    }

    private static void AddNode(SceneNode node)
    {
        if (Selection.SelectedNodes.Contains(node))
            return;

        Selection.SelectedNodes = [.. Selection.SelectedNodes, node];
    }

    private static void RemoveNode(SceneNode node)
    {
        if (!Selection.SelectedNodes.Contains(node))
            return;

        Selection.SelectedNodes = Selection.SelectedNodes.Where(n => n != node).ToArray();
    }
}
