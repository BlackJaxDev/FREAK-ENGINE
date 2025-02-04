using Extensions;
using System.Numerics;
using XREngine.Components;
using XREngine.Components.Scene.Mesh;
using XREngine.Core.Attributes;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;
using XREngine.Scene.Transforms;

namespace XREngine.Actors.Types
{
    [RequireComponents(typeof(ModelComponent))]
    public class TransformTool3D : XRComponent, IRenderable
    {
        public RenderInfo3D RenderInfo { get; }

        private static SceneNode? _instanceNode;

        public static void DestroyInstance()
        {
            _instanceNode?.Destroy();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="world">The world to spawn the transform tool in.</param>
        /// <param name="comp"></param>
        /// <param name="transformType"></param>
        /// <returns></returns>
        public static TransformTool3D? GetInstance(TransformBase comp, ETransformType transformType)
        {
            XRWorldInstance? world = comp?.World;
            if (world is null)
                return null;

            if (_instanceNode?.World != world)
            {
                _instanceNode?.Destroy();
                _instanceNode = new SceneNode(world);
            }

            TransformTool3D tool = _instanceNode.GetOrAddComponent<TransformTool3D>(out _)!;
            tool.TargetSocket = comp;
            tool.TransformMode = transformType;

            return tool;
        }

        public TransformTool3D() : base()
        {
            TransformSpace = ETransformSpace.World;
            _rc = new RenderCommandMethod3D((int)EDefaultRenderPass.OnTopForward, Render);
            RenderInfo = RenderInfo3D.New(this, _rc);
            RenderedObjects = [RenderInfo];
            UpdateModelComponent();
        }

        public event Action? MouseDown, MouseUp;

        private readonly RenderCommandMethod3D _rc;

        //UIString2D _xText, _yText, _zText;

        private readonly XRMaterial[] _axisMat = new XRMaterial[3];
        private readonly XRMaterial[] _transPlaneMat = new XRMaterial[6];
        private readonly XRMaterial[] _scalePlaneMat = new XRMaterial[3];
        private XRMaterial? _screenMat;

        private ETransformSpace _transformSpace = ETransformSpace.World;

        public Matrix4x4 PrevRootWorldMatrix { get; private set; } = Matrix4x4.Identity;
        public RenderInfo[] RenderedObjects { get; }

        protected void UpdateModelComponent()
        {
            List<SubMesh> subMeshes = [];
            List<SubMesh> screenSubMeshes = [];

            _screenMat = XRMaterial.CreateUnlitColorMaterialForward(ColorF4.LightGray);
            _screenMat.RenderOptions.DepthTest.Enabled = ERenderParamUsage.Disabled;
            _screenMat.RenderOptions.LineWidth = 1.0f;

            GetSphere(subMeshes);

            for (int normalAxis = 0; normalAxis < 3; ++normalAxis)
            {
                GetUnits(
                    normalAxis,
                    out Vector3 unit,
                    out Vector3 unit1,
                    out Vector3 unit2);

                GetMaterials(
                    normalAxis,
                    unit,
                    unit1,
                    unit2,
                    out XRMaterial axisMat,
                    out XRMaterial planeMat1,
                    out XRMaterial planeMat2,
                    out XRMaterial scalePlaneMat);

                GetLines(
                    unit,
                    unit1,
                    unit2,
                    out VertexLine axisLine,
                    out VertexLine transLine1,
                    out VertexLine transLine2,
                    out VertexLine scaleLine1,
                    out VertexLine scaleLine2);

                GetMeshes(
                    unit,
                    axisLine,
                    transLine1,
                    transLine2,
                    scaleLine1,
                    scaleLine2,
                    out XRMesh axisPrim,
                    out XRMesh arrowPrim,
                    out XRMesh transPrim1,
                    out XRMesh transPrim2,
                    out XRMesh scalePrim,
                    out XRMesh rotPrim);

                //isRotate = false
                subMeshes.Add(new SubMesh(axisPrim, axisMat));
                subMeshes.Add(new SubMesh(arrowPrim, axisMat));

                //isTranslate = true
                subMeshes.Add(new SubMesh(transPrim1, planeMat1));
                subMeshes.Add(new SubMesh(transPrim2, planeMat2));

                //isScale = true
                subMeshes.Add(new SubMesh(scalePrim, scalePlaneMat));

                //isRotate = true
                subMeshes.Add(new SubMesh(rotPrim, axisMat));
            }

            //Screen-aligned rotation
            XRMesh screenRotPrim = XRMesh.Shapes.WireframeCircle(_circRadius, Vector3.UnitZ, Vector3.Zero, _circlePrecision);

            //Screen-aligned translation
            Vertex v1 = new Vector3(-_screenTransExtent, -_screenTransExtent, 0.0f);
            Vertex v2 = new Vector3(_screenTransExtent, -_screenTransExtent, 0.0f);
            Vertex v3 = new Vector3(_screenTransExtent, _screenTransExtent, 0.0f);
            Vertex v4 = new Vector3(-_screenTransExtent, _screenTransExtent, 0.0f);
            VertexLineStrip strip = new(true, v1, v2, v3, v4);
            XRMesh screenTransPrim = XRMesh.Create(strip);

            //isRotate = true
            screenSubMeshes.Add(new SubMesh(screenRotPrim, _screenMat));
            //isTranslate = true
            screenSubMeshes.Add(new SubMesh(screenTransPrim, _screenMat));

            //Skeleton
            var rootBillboard = SceneNode.SetTransform<BillboardTransform>();
            rootBillboard.Perspective = false;
            rootBillboard.ScaleByDistance = true;
            rootBillboard.ScaleReferenceDistance = _orbRadius;
            SceneNode screen = new(SceneNode)
            {
                //BillboardType = EBillboardType.OrthographicXYZ
            };
            var screenBillboard = screen.SetTransform<BillboardTransform>();
            screenBillboard.Perspective = false;

            ModelComponent modelComp = GetSiblingComponent<ModelComponent>(true)!;
            modelComp.Model = new Model(subMeshes);

            ModelComponent screenModelComp = screen.GetOrAddComponent<ModelComponent>(out _)!;
            screenModelComp.Model = new Model(screenSubMeshes);

            ModeChanged();
        }

        private static void GetMeshes(Vector3 unit, VertexLine axisLine, VertexLine transLine1, VertexLine transLine2, VertexLine scaleLine1, VertexLine scaleLine2, out XRMesh axisPrim, out XRMesh arrowPrim, out XRMesh transPrim1, out XRMesh transPrim2, out XRMesh scalePrim, out XRMesh rotPrim)
        {
            //string axis = ((char)('X' + normalAxis)).ToString();

            float coneHeight = _axisLength - _coneDistance;

            axisPrim = XRMesh.Create(axisLine)!;
            arrowPrim = XRMesh.Shapes.SolidCone(unit * (_coneDistance + coneHeight * 0.5f), unit, coneHeight, _coneRadius, 6, false);
            transPrim1 = XRMesh.Create(transLine1);
            transPrim2 = XRMesh.Create(transLine2);
            scalePrim = XRMesh.Create(scaleLine1, scaleLine2);
            rotPrim = XRMesh.Shapes.WireframeCircle(_orbRadius, unit, Vector3.Zero, _circlePrecision);
        }

        private static void GetLines(Vector3 unit, Vector3 unit1, Vector3 unit2, out VertexLine axisLine, out VertexLine transLine1, out VertexLine transLine2, out VertexLine scaleLine1, out VertexLine scaleLine2)
        {
            axisLine = new(Vector3.Zero, unit * _axisLength);
            Vector3 halfUnit = unit * _axisHalfLength;

            transLine1 = new(halfUnit, halfUnit + unit1 * _axisHalfLength);
            transLine1.Vertex0.ColorSets.Add(new Vector4(unit1, 1.0f));
            transLine1.Vertex1.ColorSets.Add(new Vector4(unit1, 1.0f));

            transLine2 = new(halfUnit, halfUnit + unit2 * _axisHalfLength);
            transLine2.Vertex0.ColorSets.Add(new Vector4(unit2, 1.0f));
            transLine2.Vertex1.ColorSets.Add(new Vector4(unit2, 1.0f));

            scaleLine1 = new(unit1 * _scaleHalf1LDist, unit2 * _scaleHalf1LDist);
            scaleLine1.Vertex0.ColorSets.Add(new Vector4(unit, 1.0f));
            scaleLine1.Vertex1.ColorSets.Add(new Vector4(unit, 1.0f));

            scaleLine2 = new(unit1 * _scaleHalf2LDist, unit2 * _scaleHalf2LDist);
            scaleLine2.Vertex0.ColorSets.Add(new Vector4(unit, 1.0f));
            scaleLine2.Vertex1.ColorSets.Add(new Vector4(unit, 1.0f));
        }

        private void GetMaterials(int normalAxis, Vector3 unit, Vector3 unit1, Vector3 unit2, out XRMaterial axisMat, out XRMaterial planeMat1, out XRMaterial planeMat2, out XRMaterial scalePlaneMat)
        {
            axisMat = XRMaterial.CreateUnlitColorMaterialForward(unit);
            axisMat.RenderOptions.DepthTest.Enabled = ERenderParamUsage.Disabled;
            axisMat.RenderOptions.LineWidth = 1.0f;
            _axisMat[normalAxis] = axisMat;

            planeMat1 = XRMaterial.CreateUnlitColorMaterialForward(unit1);
            planeMat1.RenderOptions.DepthTest.Enabled = ERenderParamUsage.Disabled;
            planeMat1.RenderOptions.LineWidth = 1.0f;
            _transPlaneMat[(normalAxis << 1) + 0] = planeMat1;

            planeMat2 = XRMaterial.CreateUnlitColorMaterialForward(unit2);
            planeMat2.RenderOptions.DepthTest.Enabled = ERenderParamUsage.Disabled;
            planeMat2.RenderOptions.LineWidth = 1.0f;
            _transPlaneMat[(normalAxis << 1) + 1] = planeMat2;

            scalePlaneMat = XRMaterial.CreateUnlitColorMaterialForward(unit);
            scalePlaneMat.RenderOptions.DepthTest.Enabled = ERenderParamUsage.Disabled;
            scalePlaneMat.RenderOptions.LineWidth = 1.0f;
            _scalePlaneMat[normalAxis] = scalePlaneMat;
        }

        private static void GetUnits(int normalAxis, out Vector3 unit, out Vector3 unit1, out Vector3 unit2)
        {
            int planeAxis1 = normalAxis + 1 - (normalAxis >> 1) * 3; //0 = 1, 1 = 2, 2 = 0
            int planeAxis2 = planeAxis1 + 1 - (normalAxis & 1) * 3; //0 = 2, 1 = 0, 2 = 1

            unit = Vector3.Zero;
            unit[normalAxis] = 1.0f;

            unit1 = Vector3.Zero;
            unit1[planeAxis1] = 1.0f;

            unit2 = Vector3.Zero;
            unit2[planeAxis2] = 1.0f;
        }

        private static void GetSphere(List<SubMesh> subMeshes)
        {
            XRMaterial sphereMat = XRMaterial.CreateUnlitColorMaterialForward(ColorF4.Orange);
            sphereMat.RenderOptions.DepthTest.Enabled = ERenderParamUsage.Enabled;
            sphereMat.RenderOptions.DepthTest.UpdateDepth = true;
            sphereMat.RenderOptions.DepthTest.Function = EComparison.Lequal;
            sphereMat.RenderOptions.LineWidth = 1.0f;
            sphereMat.RenderOptions.WriteRed = false;
            sphereMat.RenderOptions.WriteGreen = false;
            sphereMat.RenderOptions.WriteBlue = false;
            sphereMat.RenderOptions.WriteAlpha = false;

            XRMesh spherePrim = XRMesh.Shapes.SolidSphere(Vector3.Zero, _orbRadius, 10, 10);
            //isRotate = true
            subMeshes.Add(new SubMesh(spherePrim, sphereMat));
        }

        private ETransformType _mode = ETransformType.Translate;
        private TransformBase? _targetSocket = null;

        public ETransformSpace TransformSpace
        {
            get => _transformSpace;
            set
            {
                if (_transformSpace == value)
                    return;

                _transformSpace = value;

                SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
                //_dragMatrix = RootComponent.WorldMatrix;
                //_invDragMatrix = RootComponent.InverseWorldMatrix;

                if (_transformSpace == ETransformSpace.Screen)
                    RegisterTick(ETickGroup.PostPhysics, ETickOrder.Logic, UpdateScreenSpace);
                else
                    UnregisterTick(ETickGroup.PostPhysics, ETickOrder.Logic, UpdateScreenSpace);
            }
        }

        private void UpdateScreenSpace()
        {
            if (_targetSocket != null)
                SocketTransformChanged(null);
        }

        public ETransformType TransformMode
        {
            get => _mode;
            set => SetField(ref _mode, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(TransformMode):
                    ModeChanged();
                    break;
            }
        }

        private void ModeChanged()
        {
            SetMethods();
            UpdateVisibility();
            GetDependentColors();
        }

        private void SetMethods()
        {
            switch (_mode)
            {
                case ETransformType.Rotate:
                    _highlight = HighlightRotation;
                    _drag = DragRotation;
                    _mouseDown = MouseDownRotation;
                    _mouseUp = MouseUpRotation;
                    break;
                case ETransformType.Translate:
                    _highlight = HighlightTranslation;
                    _drag = DragTranslation;
                    _mouseDown = MouseDownTranslation;
                    _mouseUp = MouseUpTranslation;
                    break;
                case ETransformType.Scale:
                    _highlight = HighlightScale;
                    _drag = DragScale;
                    _mouseDown = MouseDownScale;
                    _mouseUp = MouseUpScale;
                    break;
            }
        }

        private void UpdateVisibility()
        {
            int x = 0;

            ModelComponent modelComp = GetSiblingComponent<ModelComponent>(true)!;
            SceneNode? screen = SceneNode.Transform.TryGetChildAt(0)?.SceneNode;
            ModelComponent? screenModelComp = screen?.GetOrAddComponent<ModelComponent>(out _);

            var meshes = modelComp.Meshes;
            var screenMeshes = screenModelComp?.Meshes;

            if (meshes.Count != 21)
                return;

            meshes[x++].RenderInfo.IsVisible = _mode == ETransformType.Rotate;

            for (int i = 0; i < 3; ++i)
            {
                meshes[x++].RenderInfo.IsVisible = _mode != ETransformType.Rotate;
                meshes[x++].RenderInfo.IsVisible = _mode != ETransformType.Rotate;
                meshes[x++].RenderInfo.IsVisible = _mode == ETransformType.Translate;
                meshes[x++].RenderInfo.IsVisible = _mode == ETransformType.Translate;
                meshes[x++].RenderInfo.IsVisible = _mode == ETransformType.Scale;
                meshes[x++].RenderInfo.IsVisible = _mode == ETransformType.Rotate;
            }

            if (screenMeshes != null)
            {
                screenMeshes[0].RenderInfo.IsVisible = _mode == ETransformType.Rotate;
                screenMeshes[1].RenderInfo.IsVisible = _mode == ETransformType.Translate;
            }
        }

        private void MouseUpScale()
        {

        }

        private void MouseDownScale()
        {

        }

        private void MouseUpTranslation()
        {

        }

        private void MouseDownTranslation()
        {

        }

        private void MouseUpRotation()
        {

        }

        private void MouseDownRotation()
        {

        }

        /// <summary>
        /// The socket transform that is being manipulated by this transform tool.
        /// </summary>
        public TransformBase? TargetSocket
        {
            get => _targetSocket;
            set
            {
                if (_targetSocket != null)
                {
                    _targetSocket.WorldMatrixChanged -= SocketTransformChanged;
                }
                _targetSocket = value;
                if (_targetSocket != null)
                {
                    SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
                    _targetSocket.WorldMatrixChanged += SocketTransformChanged;
                }
                else
                    SetWorldMatrices(Matrix4x4.Identity, Matrix4x4.Identity);

                //_dragMatrix = RootComponent.WorldMatrix;
                //_invDragMatrix = RootComponent.InverseWorldMatrix;
            }
        }

        private Matrix4x4 GetSocketSpacialTransform()
        {
            if (_targetSocket is null)
                return Matrix4x4.Identity;

            switch (TransformSpace)
            {
                case ETransformSpace.Local:
                    {
                        return _targetSocket.WorldMatrix; //TODO: clear scale
                    }
                case ETransformSpace.Parent:
                    {
                        if (_targetSocket.Parent != null)
                            return _targetSocket.Parent.WorldMatrix; //TODO: clear scale
                        else
                            return Matrix4x4.CreateTranslation(_targetSocket.WorldMatrix.Translation);
                    }
                case ETransformSpace.Screen:
                    {
                        Vector3 point = _targetSocket.WorldMatrix.Translation;
                        XRCamera? camera = Engine.State.MainPlayer.Viewport?.ActiveCamera;
                        if (camera != null)
                        {
                            Vector3 forward = (camera.Transform.WorldTranslation - point).Normalized();
                            Vector3 up = camera.Transform.WorldUp;
                            return Matrix4x4.CreateWorld(point, forward, up);
                        }
                        return Matrix4x4.Identity;
                    }
                case ETransformSpace.World:
                default:
                    {
                        return Matrix4x4.CreateTranslation(_targetSocket.WorldMatrix.Translation);
                    }
            }
        }
        private Matrix4x4 GetSocketSpacialTransformInverse()
        {
            if (_targetSocket is null)
                return Matrix4x4.Identity;

            switch (TransformSpace)
            {
                case ETransformSpace.Local:
                    {
                        return _targetSocket.InverseWorldMatrix; //TODO: clear scale
                    }
                case ETransformSpace.Parent:
                    {
                        if (_targetSocket.Parent != null)
                            return _targetSocket.Parent.InverseWorldMatrix; //TODO: clear scale
                        else
                            return Matrix4x4.CreateTranslation(_targetSocket.InverseWorldMatrix.Translation);
                    }
                case ETransformSpace.Screen:
                    {
                        Vector3 point = _targetSocket.WorldMatrix.Translation;
                        XRCamera? camera = Engine.State.MainPlayer.Viewport?.ActiveCamera;
                        if (camera != null)
                        {
                            Vector3 forward = (camera.Transform.WorldTranslation - point).Normalized();
                            Vector3 up = camera.Transform.WorldUp;
                            return Matrix4x4.CreateWorld(point, forward, up).Inverted();
                        }
                        return Matrix4x4.Identity;
                    }
                case ETransformSpace.World:
                default:
                    {
                        return Matrix4x4.CreateTranslation(_targetSocket.InverseWorldMatrix.Translation);
                    }
            }
        }

        private void SocketTransformChanged(TransformBase? socket)
        {
            if (_pressed)
                return;
            
            _pressed = true;
            SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
            //_dragMatrix = RootComponent.WorldMatrix;
            //_invDragMatrix = RootComponent.InverseWorldMatrix;
            _pressed = false;
        }

        //public override void OnSpawnedPreComponentSetup()
        //{
        //    //OwningWorld.Scene.Add(this);
        //}
        //public override void OnDespawned()
        //{
        //    //OwningWorld.Scene.Remove(this);
        //}

        private BoolVector3 _hiAxis;
        private bool _hiCam, _hiSphere;
        private const int _circlePrecision = 20;
        private const float _orbRadius = 1.0f;
        private const float _circRadius = _orbRadius * _circOrbScale;
        private const float _screenTransExtent = _orbRadius * 0.1f;
        private const float _axisSnapRange = 7.0f;
        private const float _selectRange = 0.03f; //Selection error range for orb and circ
        private const float _axisSelectRange = 0.1f; //Selection error range for axes
        private const float _selectOrbScale = _selectRange / _orbRadius;
        private const float _circOrbScale = 1.2f;
        private const float _axisLength = _orbRadius * 2.0f;
        private const float _axisHalfLength = _orbRadius * 0.75f;
        private const float _coneRadius = _orbRadius * 0.1f;
        private const float _coneDistance = _orbRadius * 1.5f;
        private const float _scaleHalf1LDist = _orbRadius * 0.8f;
        private const float _scaleHalf2LDist = _orbRadius * 1.2f;

        Vector3 _lastPointWorld;
        Vector3 _localDragPlaneNormal;

        private Action? _mouseUp, _mouseDown;
        private DelDrag? _drag;
        private DelHighlight? _highlight;
        private delegate bool DelHighlight(XRCamera camera, Ray localRay);
        private delegate void DelDrag(Vector3 dragPoint);
        private delegate void DelDragRot(Quaternion dragPoint);

        #region Drag
        private bool
            _snapRotations = false,
            _snapTranslations = false,
            _snapScale = false;
        private float _rotationSnapBias = 0.0f;
        private float _rotationSnapInterval = 5.0f;
        private float _translationSnapBias = 0.0f;
        private float _translationSnapInterval = 30.0f;
        private float _scaleSnapBias = 0.0f;
        private float _scaleSnapInterval = 0.25f;
        private void DragRotation(Vector3 dragPointWorld)
        {
            XRMath.AxisAngleBetween(_lastPointWorld, dragPointWorld, out Vector3 axis, out float angle);

            //if (angle == 0.0f)
            //    return;

            //if (_snapRotations)
            //    angle = angle.RoundToNearest(_rotationSnapBias, _rotationSnapInterval);

            Quaternion worldDelta = Quaternion.CreateFromAxisAngle(axis, angle);

            //TODO: convert to socket space
            //_targetSocket.Rotation *= worldDelta;

            SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
        }

        private void DragTranslation(Vector3 dragPointWorld)
        {
            Vector3 worldDelta = dragPointWorld - _lastPointWorld;

            //Matrix4 m = _targetSocket.InverseWorldMatrix.ClearScale();
            //m = m.ClearTranslation();
            //Vector3 worldTrans = m * delta;

            //if (_snapTranslations)
            //{
            //    //Modify delta to move resulting world point to nearest snap
            //    Vector3 worldPoint = _targetSocket.WorldMatrix.Translation;
            //    Vector3 resultPoint = worldPoint + worldTrans;

            //    resultPoint.X = resultPoint.X.RoundToNearest(_translationSnapBias, _translationSnapInterval);
            //    resultPoint.Y = resultPoint.Y.RoundToNearest(_translationSnapBias, _translationSnapInterval);
            //    resultPoint.Z = resultPoint.Z.RoundToNearest(_translationSnapBias, _translationSnapInterval);

            //    worldTrans = resultPoint - worldPoint;
            //}

            //TODO: convert world delta to local socket delta
            //if (_targetSocket != null)
            //    _targetSocket.Translation.Value += worldDelta;

            SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
        }
        private void DragScale(Vector3 dragPointWorld)
        {
            Vector3 worldDelta = dragPointWorld - _lastPointWorld;

            //TODO: better method for scaling

            //_targetSocket.Scale += worldDelta;

            SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
        }
        /// <summary>
        /// Returns a point relative to the local space of the target socket (origin at 0,0,0), clamped to the highlighted drag plane.
        /// </summary>
        /// <param name="camera">The camera viewing this tool, used for camera space drag clamping.</param>
        /// <param name="localRay">The mouse ray, transformed into the socket's local space.</param>
        /// <returns></returns>
        private Vector3 GetLocalDragPoint(XRCamera camera, Ray localRay)
        {
            //Convert all coordinates to local space

            Vector3 localCamPoint = Vector3.Transform(camera.Transform.WorldTranslation, Transform.InverseWorldMatrix);
            Vector3 localDragPoint, unit;

            switch (_mode)
            {
                case ETransformType.Scale:
                case ETransformType.Translate:
                    {
                        if (_hiCam)
                        {
                            _localDragPlaneNormal = localCamPoint;
                            _localDragPlaneNormal.Normalized();
                        }
                        else if (_hiAxis.X)
                        {
                            if (_hiAxis.Y)
                            {
                                _localDragPlaneNormal = Vector3.UnitZ;
                            }
                            else if (_hiAxis.Z)
                            {
                                _localDragPlaneNormal = Vector3.UnitY;
                            }
                            else
                            {
                                unit = Vector3.UnitX;
                                Vector3 perpPoint = Ray.GetClosestColinearPoint(Vector3.Zero, unit, localCamPoint);
                                _localDragPlaneNormal = localCamPoint - perpPoint;
                                _localDragPlaneNormal.Normalized();

                                if (!GeoUtil.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
                                    return _lastPointWorld;

                                return Ray.GetClosestColinearPoint(Vector3.Zero, unit, localDragPoint);
                            }
                        }
                        else if (_hiAxis.Y)
                        {
                            if (_hiAxis.X)
                            {
                                _localDragPlaneNormal = Vector3.UnitZ;
                            }
                            else if (_hiAxis.Z)
                            {
                                _localDragPlaneNormal = Vector3.UnitX;
                            }
                            else
                            {
                                unit = Vector3.UnitY;
                                Vector3 perpPoint = Ray.GetClosestColinearPoint(Vector3.Zero, unit, localCamPoint);
                                _localDragPlaneNormal = localCamPoint - perpPoint;
                                _localDragPlaneNormal.Normalized();

                                if (!GeoUtil.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
                                    return _lastPointWorld;

                                return Ray.GetClosestColinearPoint(Vector3.Zero, unit, localDragPoint);
                            }
                        }
                        else if (_hiAxis.Z)
                        {
                            if (_hiAxis.X)
                            {
                                _localDragPlaneNormal = Vector3.UnitY;
                            }
                            else if (_hiAxis.Y)
                            {
                                _localDragPlaneNormal = Vector3.UnitX;
                            }
                            else
                            {
                                unit = Vector3.UnitZ;
                                Vector3 perpPoint = Ray.GetClosestColinearPoint(Vector3.Zero, unit, localCamPoint);
                                _localDragPlaneNormal = localCamPoint - perpPoint;
                                _localDragPlaneNormal.Normalized();

                                if (!GeoUtil.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
                                    return _lastPointWorld;

                                return Ray.GetClosestColinearPoint(Vector3.Zero, unit, localDragPoint);
                            }
                        }

                        if (GeoUtil.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
                            return localDragPoint;
                    }
                    break;
                case ETransformType.Rotate:
                    {
                        if (_hiCam)
                        {
                            _localDragPlaneNormal = localCamPoint;
                            _localDragPlaneNormal.Normalized();

                            if (GeoUtil.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
                                return localDragPoint;
                        }
                        else if (_hiAxis.Any)
                        {
                            if (_hiAxis.X)
                                unit = Vector3.UnitX;
                            else if (_hiAxis.Y)
                                unit = Vector3.UnitY;
                            else// if (_hiAxis.Z)
                                unit = Vector3.UnitZ;

                            _localDragPlaneNormal = unit;
                            _localDragPlaneNormal.Normalized();

                            if (GeoUtil.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
                                return localDragPoint;
                        }
                        else if (_hiSphere)
                        {
                            Vector3 worldPoint = Transform.WorldTranslation;
                            float radius = camera.DistanceScaleOrthographic(worldPoint, _orbRadius);

                            if (GeoUtil.RayIntersectsSphere(localRay.StartPoint, localRay.Direction, Vector3.Zero, radius * _circOrbScale, out localDragPoint))
                            {
                                _localDragPlaneNormal = localDragPoint.Normalized();
                                return localDragPoint;
                            }
                        }
                    }
                    break;
            }

            return _lastPointWorld;
        }
        #endregion

        #region Highlighting
        private bool HighlightRotation(XRCamera camera, Ray localRay)
        {
            Vector3 worldPoint = Transform.WorldTranslation;
            float radius = camera.DistanceScaleOrthographic(worldPoint, _orbRadius);

            if (!GeoUtil.RayIntersectsSphere(localRay.StartPoint, localRay.Direction, Vector3.Zero, radius * _circOrbScale, out Vector3 point))
            {
                //If no intersect is found, project the ray through the plane perpendicular to the camera.
                //localRay.LinePlaneIntersect(Vector3.Zero, (camera.WorldPoint - worldPoint).Normalized(), out point);
                GeoUtil.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, Vector3.Transform((camera.Transform.WorldTranslation - worldPoint), Transform.InverseWorldMatrix), out point);

                //Clamp the point to edge of the sphere
                point = Ray.PointAtLineDistance(Vector3.Zero, point, radius);

                //Point lies on circ line?
                float distance = point.Length();
                if (Math.Abs(distance - radius * _circOrbScale) < radius * _selectOrbScale)
                    _hiCam = true;
            }
            else
            {
                point = point.Normalized();

                _hiSphere = true;

                float x = point.Dot(Vector3.UnitX);
                float y = point.Dot(Vector3.UnitY);
                float z = point.Dot(Vector3.UnitZ);

                if (Math.Abs(x) < 0.3f)
                    _hiAxis.X = true;
                else if (Math.Abs(y) < 0.3f)
                    _hiAxis.Y = true;
                else if (Math.Abs(z) < 0.3f)
                    _hiAxis.Z = true;
            }

            return _hiAxis.Any || _hiCam || _hiSphere;
        }
        private bool HighlightTranslation(XRCamera camera, Ray localRay)
        {
            Vector3 worldPoint = Transform.WorldTranslation;
            float radius = camera.DistanceScaleOrthographic(worldPoint, _orbRadius);

            List<Vector3> intersectionPoints = new(3);

            bool snapFound = false;
            for (int normalAxis = 0; normalAxis < 3; ++normalAxis)
            {
                Vector3 unit = Vector3.Zero;
                unit[normalAxis] = localRay.StartPoint[normalAxis] < 0.0f ? -1.0f : 1.0f;

                //Get plane intersection point for cursor ray and each drag plane
                if (GeoUtil.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, unit, out Vector3 point))
                    intersectionPoints.Add(point);
            }

            //_intersectionPoints.Sort((l, r) => l.DistanceToSquared(camera.WorldPoint).CompareTo(r.DistanceToSquared(camera.WorldPoint)));

            foreach (Vector3 v in intersectionPoints)
            {
                Vector3 diff = v / radius;
                //int planeAxis1 = normalAxis + 1 - (normalAxis >> 1) * 3;    //0 = 1, 1 = 2, 2 = 0
                //int planeAxis2 = planeAxis1 + 1 - (normalAxis  & 1) * 3;    //0 = 2, 1 = 0, 2 = 1

                if (diff.X > -_axisSelectRange && diff.X <= _axisLength &&
                    diff.Y > -_axisSelectRange && diff.Y <= _axisLength &&
                    diff.Z > -_axisSelectRange && diff.Z <= _axisLength)
                {
                    float errorRange = _axisSelectRange;

                    _hiAxis.X = diff.X > _axisHalfLength && Math.Abs(diff.Y) < errorRange && Math.Abs(diff.Z) < errorRange;
                    _hiAxis.Y = diff.Y > _axisHalfLength && Math.Abs(diff.X) < errorRange && Math.Abs(diff.Z) < errorRange;
                    _hiAxis.Z = diff.Z > _axisHalfLength && Math.Abs(diff.X) < errorRange && Math.Abs(diff.Y) < errorRange;

                    if (snapFound = _hiAxis.Any)
                        break;

                    if (diff.X < _axisHalfLength &&
                        diff.Y < _axisHalfLength &&
                        diff.Z < _axisHalfLength)
                    {
                        //Point lies inside the double drag areas
                        _hiAxis.X = diff.X > _axisSelectRange;
                        _hiAxis.Y = diff.Y > _axisSelectRange;
                        _hiAxis.Z = diff.Z > _axisSelectRange;
                        _hiCam = _hiAxis.None;

                        snapFound = true;
                        break;
                    }
                }
            }

            return snapFound;
        }
        private bool HighlightScale(XRCamera camera, Ray localRay)
        {
            Vector3 worldPoint = Transform.WorldTranslation;
            float radius = camera.DistanceScaleOrthographic(worldPoint, _orbRadius);

            List<Vector3> intersectionPoints = new(3);

            bool snapFound = false;
            for (int normalAxis = 0; normalAxis < 3; ++normalAxis)
            {
                Vector3 unit = Vector3.Zero;
                unit[normalAxis] = localRay.StartPoint[normalAxis] < 0.0f ? -1.0f : 1.0f;

                //Get plane intersection point for cursor ray and each drag plane
                if (GeoUtil.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, unit, out Vector3 point))
                    intersectionPoints.Add(point);
            }

            //_intersectionPoints.Sort((l, r) => l.DistanceToSquared(camera.WorldPoint).CompareTo(r.DistanceToSquared(camera.WorldPoint)));

            foreach (Vector3 v in intersectionPoints)
            {
                Vector3 diff = v / radius;
                //int planeAxis1 = normalAxis + 1 - (normalAxis >> 1) * 3;    //0 = 1, 1 = 2, 2 = 0
                //int planeAxis2 = planeAxis1 + 1 - (normalAxis  & 1) * 3;    //0 = 2, 1 = 0, 2 = 1

                if (diff.X > -_axisSelectRange && diff.X <= _axisLength &&
                    diff.Y > -_axisSelectRange && diff.Y <= _axisLength &&
                    diff.Z > -_axisSelectRange && diff.Z <= _axisLength)
                {
                    float errorRange = _axisSelectRange;

                    _hiAxis.X = diff.X > _axisHalfLength && Math.Abs(diff.Y) < errorRange && Math.Abs(diff.Z) < errorRange;
                    _hiAxis.Y = diff.Y > _axisHalfLength && Math.Abs(diff.X) < errorRange && Math.Abs(diff.Z) < errorRange;
                    _hiAxis.Z = diff.Z > _axisHalfLength && Math.Abs(diff.X) < errorRange && Math.Abs(diff.Y) < errorRange;

                    if (snapFound = _hiAxis.Any)
                        break;

                    //Determine if the point is in the double or triple drag triangles
                    float halfDist = _scaleHalf2LDist;
                    float centerDist = _scaleHalf1LDist;

                    if (XRMath.IsInTriangle(diff, new Vector3(), new Vector3(halfDist, 0, 0), new Vector3(0, halfDist, 0)))
                    {
                        if (XRMath.IsInTriangle(diff, new Vector3(), new Vector3(centerDist, 0, 0), new Vector3(0, centerDist, 0)))
                            _hiAxis.X = _hiAxis.Y = _hiAxis.Z = true;
                        else
                            _hiAxis.X = _hiAxis.Y = true;
                    }
                    else if (XRMath.IsInTriangle(diff, new Vector3(), new Vector3(halfDist, 0, 0), new Vector3(0, 0, halfDist)))
                    {
                        if (XRMath.IsInTriangle(diff, new Vector3(), new Vector3(centerDist, 0, 0), new Vector3(0, 0, centerDist)))
                            _hiAxis.X = _hiAxis.Y = _hiAxis.Z = true;
                        else
                            _hiAxis.X = _hiAxis.Y = true;
                    }
                    else if (XRMath.IsInTriangle(diff, new Vector3(), new Vector3(0, halfDist, 0), new Vector3(0, 0, halfDist)))
                    {
                        if (XRMath.IsInTriangle(diff, new Vector3(), new Vector3(0, centerDist, 0), new Vector3(0, 0, centerDist)))
                            _hiAxis.X = _hiAxis.Y = _hiAxis.Z = true;
                        else
                            _hiAxis.Y = _hiAxis.Z = true;
                    }

                    snapFound = _hiAxis.Any;

                    if (snapFound)
                        break;
                }
            }

            return snapFound;
        }
        #endregion

        private bool _pressed = false;
        //private Matrix4 _dragMatrix, _invDragMatrix;

        /// <summary>
        /// Returns true if intersecting one of the transform tool's various parts.
        /// </summary>
        public bool MouseMove(Ray cursor, XRCamera camera, bool pressed)
        {
            bool snapFound = true;
            if (pressed)
            {
                if (_hiAxis.None && !_hiCam && !_hiSphere)
                    return false;

                if (!_pressed)
                    OnPressed();

                Ray localRay = cursor.TransformedBy(Transform.InverseWorldMatrix);
                Vector3 localDragPoint = GetLocalDragPoint(camera, localRay);
                Vector3 worldDragPoint = Vector3.Transform(localDragPoint, Transform.WorldMatrix);
                _drag?.Invoke(worldDragPoint);

                _lastPointWorld = worldDragPoint;
            }
            else
            {
                if (_pressed)
                    OnReleased();

                Ray localRay = cursor.TransformedBy(Transform.InverseWorldMatrix);

                _hiAxis.X = _hiAxis.Y = _hiAxis.Z = false;
                _hiCam = _hiSphere = false;

                snapFound = _highlight?.Invoke(camera, localRay) ?? false;

                _axisMat[0].Parameter<ShaderVector4>(0)!.Value = _hiAxis.X ? ColorF4.Yellow : ColorF4.Red;
                _axisMat[1].Parameter<ShaderVector4>(0)!.Value = _hiAxis.Y ? ColorF4.Yellow : ColorF4.Green;
                _axisMat[2].Parameter<ShaderVector4>(0)!.Value = _hiAxis.Z ? ColorF4.Yellow : ColorF4.Blue;
                _screenMat!.Parameter<ShaderVector4>(0)!.Value = _hiCam ? ColorF4.Yellow : ColorF4.LightGray;

                GetDependentColors();

                Vector3 localDragPoint = GetLocalDragPoint(camera, localRay);
                _lastPointWorld = Vector3.Transform(localDragPoint, Transform.WorldMatrix);
            }
            return snapFound;
        }
        private void GetDependentColors()
        {
            if (_transPlaneMat.Any(m => m == null) || _scalePlaneMat.Any(m => m == null) || TransformMode == ETransformType.Rotate)
                return;

            if (TransformMode == ETransformType.Translate)
            {
                _transPlaneMat[0].Parameter<ShaderVector4>(0)!.Value = _hiAxis.X && _hiAxis.Y ? (ColorF4)ColorF4.Yellow : ColorF4.Red;
                _transPlaneMat[1].Parameter<ShaderVector4>(0)!.Value = _hiAxis.X && _hiAxis.Z ? (ColorF4)ColorF4.Yellow : ColorF4.Red;
                _transPlaneMat[2].Parameter<ShaderVector4>(0)!.Value = _hiAxis.Y && _hiAxis.Z ? (ColorF4)ColorF4.Yellow : ColorF4.Green;
                _transPlaneMat[3].Parameter<ShaderVector4>(0)!.Value = _hiAxis.Y && _hiAxis.X ? (ColorF4)ColorF4.Yellow : ColorF4.Green;
                _transPlaneMat[4].Parameter<ShaderVector4>(0)!.Value = _hiAxis.Z && _hiAxis.X ? (ColorF4)ColorF4.Yellow : ColorF4.Blue;
                _transPlaneMat[5].Parameter<ShaderVector4>(0)!.Value = _hiAxis.Z && _hiAxis.Y ? (ColorF4)ColorF4.Yellow : ColorF4.Blue;
            }
            else
            {
                _scalePlaneMat[0].Parameter<ShaderVector4>(0)!.Value = _hiAxis.Y && _hiAxis.Z ? (ColorF4)ColorF4.Yellow : ColorF4.Red;
                _scalePlaneMat[1].Parameter<ShaderVector4>(0)!.Value = _hiAxis.X && _hiAxis.Z ? (ColorF4)ColorF4.Yellow : ColorF4.Green;
                _scalePlaneMat[2].Parameter<ShaderVector4>(0)!.Value = _hiAxis.X && _hiAxis.Y ? (ColorF4)ColorF4.Yellow : ColorF4.Blue;
            }
        }

        private void OnPressed()
        {
            if (_targetSocket != null)
            {
                SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
                PrevRootWorldMatrix = _targetSocket.WorldMatrix;
            }
            else
            {
                SetWorldMatrices(Matrix4x4.Identity, Matrix4x4.Identity);
                PrevRootWorldMatrix = Matrix4x4.Identity;
            }

            _pressed = true;
            MouseDown?.Invoke();
        }

        private void SetWorldMatrices(Matrix4x4 transform, Matrix4x4 invTransform)
        {
            SceneNode.GetTransformAs<DrivenWorldTransform>(true)!.SetWorldMatrix(transform);
        }

        private void OnReleased()
        {
            _pressed = false;
            MouseUp?.Invoke();
        }

        private void Render(bool shadowPass)
        {
            if (!_hiCam && !_hiSphere && !_hiAxis.Any)
                return;
            
            Engine.Rendering.Debug.RenderPoint(_lastPointWorld, ColorF4.Black, false);
            Vector3 worldNormal = Vector3.TransformNormal(_localDragPlaneNormal, Transform.WorldMatrix);
            var camera = Engine.Rendering.State.RenderingCamera;
            if (camera != null)
                Engine.Rendering.Debug.RenderLine(_lastPointWorld, _lastPointWorld + worldNormal * camera.DistanceScaleOrthographic(Transform.WorldTranslation, 2.0f), ColorF4.Black, false);
        }
    }
}
