namespace XREngine.Actors.Types
{
    //    public enum ESpace
    //    {
    //        /// <summary>
    //        /// Relative to the world.
    //        /// </summary>
    //        World,
    //        /// <summary>
    //        /// Relative to the parent transform (or world if no parent).
    //        /// </summary>
    //        Parent,
    //        /// <summary>
    //        /// Relative to the current transform.
    //        /// </summary>
    //        Local,
    //        /// <summary>
    //        /// Relative to the camera transform.
    //        /// </summary>
    //        Screen,
    //    }
    //    public enum TransformType
    //    {
    //        Scale,
    //        Rotate,
    //        Translate,
    //        DragDrop,
    //    }
    //    [RequireComponents(typeof(SkeletalMeshComponent))]
    //    public class TransformTool3D : XRComponent, I3DRenderable
    //    {
    //        public static TransformTool3D Instance => _currentInstance.Value;
    //        private static readonly Lazy<TransformTool3D> _currentInstance = new Lazy<TransformTool3D>(() =>
    //        {
    //            var node = new TransformTool3D();
    //            node.EditorState.DisplayInActorTree = false;
    //            return node;
    //        });

    //        public IRenderInfo3D RenderInfo { get; } = new RenderInfo3D(true, true);

    //        public TransformTool3D() : base()
    //        {
    //            TransformSpace = ESpace.Local;
    //            _rc = new RenderCommandMethod3D(ERenderPass.OnTopForward, Render);
    //        }

    //        private readonly XRMaterial[] _axisMat = new XRMaterial[3];
    //        private readonly XRMaterial[] _transPlaneMat = new XRMaterial[6];
    //        private readonly XRMaterial[] _scalePlaneMat = new XRMaterial[3];
    //        private XRMaterial _screenMat;

    //        private ESpace _transformSpace;

    //        protected override SkeletalMeshComponent OnConstructRoot()
    //        {
    //            #region Mesh Generation

    //            SkeletalModel mesh = new("TransformTool");

    //            //Skeleton
    //            RenderBone root = new()
    //            {
    //                ScaleByDistance = true,
    //                DistanceScaleScreenSize = _orbRadius,
    //            };
    //            RenderBone screen = new()
    //            {
    //                BillboardType = EBillboardType.OrthographicXYZ
    //            };
    //            root.ChildBones.Add(screen);
    //            Skeleton skel = new Skeleton(root);

    //            _screenMat = XRMaterial.CreateUnlitColorMaterialForward(Color.LightGray);
    //            _screenMat.RenderParamsRef.File.DepthTest.Enabled = ERenderParamUsage.Disabled;
    //            _screenMat.RenderParamsRef.File.LineWidth = 1.0f;

    //            bool isTranslate = TransformMode == TransformType.Translate;
    //            bool isRotate = TransformMode == TransformType.Rotate;
    //            bool isScale = TransformMode == TransformType.Scale;

    //            for (int normalAxis = 0; normalAxis < 3; ++normalAxis)
    //            {
    //                int planeAxis1 = normalAxis + 1 - (normalAxis >> 1) * 3; //0 = 1, 1 = 2, 2 = 0
    //                int planeAxis2 = planeAxis1 + 1 - (normalAxis  & 1) * 3; //0 = 2, 1 = 0, 2 = 1

    //                Vector3 unit = Vector3.Zero;
    //                unit[normalAxis] = 1.0f;

    //                Vector3 unit1 = Vector3.Zero;
    //                unit1[planeAxis1] = 1.0f;

    //                Vector3 unit2 = Vector3.Zero;
    //                unit2[planeAxis2] = 1.0f;

    //                XRMaterial axisMat = XRMaterial.CreateUnlitColorMaterialForward(unit);
    //                axisMat.RenderOptions.DepthTest.Enabled = ERenderParamUsage.Disabled;
    //                axisMat.RenderOptions.LineWidth = 1.0f;
    //                _axisMat[normalAxis] = axisMat;

    //                XRMaterial planeMat1 = XRMaterial.CreateUnlitColorMaterialForward(unit1);
    //                planeMat1.RenderOptions.DepthTest.Enabled = ERenderParamUsage.Disabled;
    //                planeMat1.RenderOptions.LineWidth = 1.0f;
    //                _transPlaneMat[(normalAxis << 1) + 0] = planeMat1;

    //                XRMaterial planeMat2 = XRMaterial.CreateUnlitColorMaterialForward(unit2);
    //                planeMat2.RenderOptions.DepthTest.Enabled = ERenderParamUsage.Disabled;
    //                planeMat2.RenderOptions.LineWidth = 1.0f;
    //                _transPlaneMat[(normalAxis << 1) + 1] = planeMat2;

    //                XRMaterial scalePlaneMat = XRMaterial.CreateUnlitColorMaterialForward(unit);
    //                scalePlaneMat.RenderOptions.DepthTest.Enabled = ERenderParamUsage.Disabled;
    //                scalePlaneMat.RenderOptions.LineWidth = 1.0f;
    //                _scalePlaneMat[normalAxis] = scalePlaneMat;

    //                VertexLine axisLine = new(Vector3.Zero, unit * _axisLength);
    //                Vector3 halfUnit = unit * _axisHalfLength;

    //                VertexLine transLine1 = new(halfUnit, halfUnit + unit1 * _axisHalfLength);
    //                transLine1.Vertex0.Color = unit1;
    //                transLine1.Vertex1.Color = unit1;

    //                VertexLine transLine2 = new(halfUnit, halfUnit + unit2 * _axisHalfLength);
    //                transLine2.Vertex0.Color = unit2;
    //                transLine2.Vertex1.Color = unit2;

    //                VertexLine scaleLine1 = new(unit1 * _scaleHalf1LDist, unit2 * _scaleHalf1LDist);
    //                scaleLine1.Vertex0.Color = unit;
    //                scaleLine1.Vertex1.Color = unit;

    //                VertexLine scaleLine2 = new(unit1 * _scaleHalf2LDist, unit2 * _scaleHalf2LDist);
    //                scaleLine2.Vertex0.Color = unit;
    //                scaleLine2.Vertex1.Color = unit;

    //                string axis = ((char)('X' + normalAxis)).ToString();

    //                XRMesh axisPrim = XRMesh.Create(VertexShaderDesc.Positions(), axisLine)!;
    //                axisPrim.SingleBindBone = rootBoneName;
    //                mesh.RigidChildren.Add(new SkeletalRigidSubMesh(axis + "Axis", new RenderInfo3D(!isRotate, true), ERenderPass.OnTopForward, axisPrim, axisMat));

    //                float coneHeight = _axisLength - _coneDistance;
    //                XRMesh arrowPrim = Cone.SolidMesh(unit * (_coneDistance + coneHeight * 0.5f), unit, coneHeight, _coneRadius, 6, false);
    //                arrowPrim.SingleBindBone = rootBoneName;
    //                mesh.RigidChildren.Add(new SkeletalRigidSubMesh(axis + "Arrow", new RenderInfo3D(!isRotate, true), ERenderPass.OnTopForward, arrowPrim, axisMat));

    //                XRMesh transPrim1 = XRMesh.Create(VertexShaderDesc.Positions(), transLine1);
    //                transPrim1.SingleBindBone = rootBoneName;
    //                mesh.RigidChildren.Add(new SkeletalRigidSubMesh(axis + "TransPlane1", new RenderInfo3D(isTranslate, true), ERenderPass.OnTopForward, transPrim1, planeMat1));

    //                XRMesh transPrim2 = XRMesh.Create(VertexShaderDesc.Positions(), transLine2);
    //                transPrim2.SingleBindBone = rootBoneName;
    //                mesh.RigidChildren.Add(new SkeletalRigidSubMesh(axis + "TransPlane2", new RenderInfo3D(isTranslate, true), ERenderPass.OnTopForward, transPrim2, planeMat2));

    //                XRMesh scalePrim = XRMesh.Create(VertexShaderDesc.Positions(), scaleLine1, scaleLine2);
    //                scalePrim.SingleBindBone = rootBoneName;
    //                mesh.RigidChildren.Add(new SkeletalRigidSubMesh(axis + "ScalePlane", new RenderInfo3D(isScale, true), ERenderPass.OnTopForward, scalePrim, scalePlaneMat));

    //                XRMesh rotPrim = Circle3D.WireframeMesh(_orbRadius, unit, Vector3.Zero, _circlePrecision);
    //                rotPrim.SingleBindBone = rootBoneName;
    //                mesh.RigidChildren.Add(new SkeletalRigidSubMesh(axis + "Rotation", new RenderInfo3D(isRotate, true), ERenderPass.OnTopForward, rotPrim, axisMat));
    //            }

    //            //Screen-aligned rotation
    //            XRMesh screenRotPrim = Circle3D.WireframeMesh(_circRadius, Vector3.UnitZ, Vector3.Zero, _circlePrecision);
    //            screenRotPrim.SingleBindBone = screenBoneName;

    //            mesh.RigidChildren.Add(new SkeletalRigidSubMesh("ScreenRotation", new RenderInfo3D(isRotate, true), ERenderPass.OnTopForward, screenRotPrim, _screenMat));

    //            //Screen-aligned translation
    //            Vertex v1 = new Vector3(-_screenTransExtent, -_screenTransExtent, 0.0f);
    //            Vertex v2 = new Vector3(_screenTransExtent, -_screenTransExtent, 0.0f);
    //            Vertex v3 = new Vector3(_screenTransExtent, _screenTransExtent, 0.0f);
    //            Vertex v4 = new Vector3(-_screenTransExtent, _screenTransExtent, 0.0f);
    //            VertexLineStrip strip = new(true, v1, v2, v3, v4);
    //            XRMesh screenTransPrim = XRMesh.Create(VertexShaderDesc.Positions(), strip);
    //            screenTransPrim.SingleBindBone = screenBoneName;

    //            mesh.RigidChildren.Add(new SkeletalRigidSubMesh("ScreenTranslation", new RenderInfo3D(isTranslate, true), ERenderPass.OnTopForward, screenTransPrim, _screenMat));

    //            XRMaterial sphereMat = XRMaterial.CreateUnlitColorMaterialForward(Color.Orange);
    //            sphereMat.RenderOptions.DepthTest.Enabled = ERenderParamUsage.Enabled;
    //            sphereMat.RenderOptions.DepthTest.UpdateDepth = true;
    //            sphereMat.RenderOptions.DepthTest.Function = EComparison.Lequal;
    //            sphereMat.RenderOptions.LineWidth = 1.0f;
    //            sphereMat.RenderOptions.WriteRed = false;
    //            sphereMat.RenderOptions.WriteGreen = false;
    //            sphereMat.RenderOptions.WriteBlue = false;
    //            sphereMat.RenderOptions.WriteAlpha = false;

    //            XRMesh spherePrim = Sphere.SolidMesh(Vector3.Zero, _orbRadius, 10, 10);
    //            spherePrim.SingleBindBone = rootBoneName;
    //            mesh.RigidChildren.Add(new SkeletalRigidSubMesh("RotationSphere", new RenderInfo3D(isRotate, true), ERenderPass.OnTopForward, spherePrim, sphereMat));

    //            return new SkeletalMeshComponent(mesh, skel);

    //            #endregion
    //        }

    //        private TransformType _mode = TransformType.Translate;
    //        private TransformBase? _targetSocket = null;

    //        [Category("Transform Tool 3D")]
    //        public ESpace TransformSpace
    //        {
    //            get => _transformSpace;
    //            set
    //            {
    //                if (_transformSpace == value)
    //                    return;

    //                _transformSpace = value;

    //                RootComponent.SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
    //                //_dragMatrix = RootComponent.WorldMatrix;
    //                //_invDragMatrix = RootComponent.InverseWorldMatrix;

    //                if (_transformSpace == ESpace.Screen)
    //                    RegisterTick(ETickGroup.DuringPhysics, ETickOrder.Logic, UpdateScreenSpace);
    //                else
    //                    UnregisterTick(ETickGroup.DuringPhysics, ETickOrder.Logic, UpdateScreenSpace);
    //            }
    //        }

    //        private void UpdateScreenSpace(float delta)
    //        {
    //            if (_targetSocket != null)
    //                TransformChanged(null);
    //        }

    //        [Category("Transform Tool 3D")]
    //        public TransformType TransformMode
    //        {
    //            get => _mode;
    //            set
    //            {
    //                _mode = value;
    //                switch (_mode)
    //                {
    //                    case TransformType.Rotate:
    //                        _highlight = HighlightRotation;
    //                        _drag = DragRotation;
    //                        _mouseDown = MouseDownRotation;
    //                        _mouseUp = MouseUpRotation;
    //                        break;
    //                    case TransformType.Translate:
    //                        _highlight = HighlightTranslation;
    //                        _drag = DragTranslation;
    //                        _mouseDown = MouseDownTranslation;
    //                        _mouseUp = MouseUpTranslation;
    //                        break;
    //                    case TransformType.Scale:
    //                        _highlight = HighlightScale;
    //                        _drag = DragScale;
    //                        _mouseDown = MouseDownScale;
    //                        _mouseUp = MouseUpScale;
    //                        break;
    //                }
    //                int x = 0;

    //                if (RootComponent.Meshes is null)
    //                    return;

    //                for (int i = 0; i < 3; ++i)
    //                {
    //                    RootComponent.Meshes[x++].RenderInfo.IsVisible = _mode != TransformType.Rotate;
    //                    RootComponent.Meshes[x++].RenderInfo.IsVisible = _mode != TransformType.Rotate;
    //                    RootComponent.Meshes[x++].RenderInfo.IsVisible = _mode == TransformType.Translate;
    //                    RootComponent.Meshes[x++].RenderInfo.IsVisible = _mode == TransformType.Translate;
    //                    RootComponent.Meshes[x++].RenderInfo.IsVisible = _mode == TransformType.Scale;
    //                    RootComponent.Meshes[x++].RenderInfo.IsVisible = _mode == TransformType.Rotate;
    //                }
    //                RootComponent.Meshes[x++].RenderInfo.IsVisible = _mode == TransformType.Rotate;
    //                RootComponent.Meshes[x++].RenderInfo.IsVisible = _mode == TransformType.Translate;
    //                RootComponent.Meshes[x++].RenderInfo.IsVisible = _mode == TransformType.Rotate;

    //                GetDependentColors();
    //            }
    //        }

    //        private void MouseUpScale()
    //        {

    //        }

    //        private void MouseDownScale()
    //        {

    //        }

    //        private void MouseUpTranslation()
    //        {

    //        }

    //        private void MouseDownTranslation()
    //        {

    //        }

    //        private void MouseUpRotation()
    //        {

    //        }

    //        private void MouseDownRotation()
    //        {

    //        }

    //        /// <summary>
    //        /// The socket transform that is being manipulated by this transform tool.
    //        /// </summary>
    //        [Browsable(false)]
    //        public TransformBase TargetSocket
    //        {
    //            get => _targetSocket;
    //            set
    //            {
    //                if (_targetSocket != null)
    //                {
    ////#if EDITOR
    ////                    _targetSocket.Selected = false;
    ////#endif
    //                    _targetSocket.SocketTransformChanged -= TransformChanged;
    //                }
    //                _targetSocket = value;
    //                if (_targetSocket != null)
    //                {
    ////#if EDITOR
    ////                    _targetSocket.Selected = true;
    ////#endif

    //                    RootComponent.SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
    //                    _targetSocket.SocketTransformChanged += TransformChanged;
    //                }
    //                else
    //                    RootComponent.SetWorldMatrices(Matrix4.Identity, Matrix4.Identity);

    //                //_dragMatrix = RootComponent.WorldMatrix;
    //                //_invDragMatrix = RootComponent.InverseWorldMatrix;
    //            }
    //        }

    //        private Matrix4x4 GetSocketSpacialTransform()
    //        {
    //            if (_targetSocket is null)
    //                return Matrix4x4.Identity;

    //            switch (TransformSpace)
    //            {
    //                case ESpace.Local:
    //                    {
    //                        return _targetSocket.WorldMatrix.Value.ClearScale();
    //                    }
    //                case ESpace.Parent:
    //                    {
    //                        if (_targetSocket.ParentSocket != null)
    //                            return _targetSocket.ParentSocket.WorldMatrix.Value.ClearScale();
    //                        else
    //                            return _targetSocket.WorldMatrix.Translation.AsTranslationMatrix();
    //                    }
    //                case ESpace.Screen:
    //                    {
    //                        Vector3 point = _targetSocket.WorldMatrix.Translation;
    //                        var localPlayers = OwningWorld.GameMode.LocalPlayers;
    //                        if (localPlayers.Count > 0)
    //                        {
    //                            XRCamera c = localPlayers[0].ViewportCamera;
    //                            if (c != null)
    //                            {
    //                                //Rotator angles = (c.WorldPoint - point).LookatAngles();
    //                                //Matrix4 angleMatrix = angles.GetMatrix();
    //                                //return point.AsTranslationMatrix() * angleMatrix;
    //                                Vector3 fwd = (c.WorldPoint - point).NormalizedFast();
    //                                Vector3 up = c.UpVector;
    //                                Vector3 right = up ^ fwd;
    //                                return Matrix4x4.CreateSpacialTransform(point, right, up, fwd);
    //                            }
    //                        }

    //                        return Matrix4x4.Identity;
    //                    }
    //                case ESpace.World:
    //                default:
    //                    {
    //                        return _targetSocket.WorldMatrix.Translation.AsTranslationMatrix();
    //                    }
    //            }
    //        }
    //        private Matrix4x4 GetSocketSpacialTransformInverse()
    //        {
    //            if (_targetSocket is null)
    //                return Matrix4x4.Identity;

    //            switch (TransformSpace)
    //            {
    //                case ESpace.Local:
    //                    {
    //                        return _targetSocket.InverseWorldMatrix.Value.ClearScale();
    //                    }
    //                case ESpace.Parent:
    //                    {
    //                        if (_targetSocket.ParentSocket != null)
    //                            return _targetSocket.ParentSocket.InverseWorldMatrix.Value.ClearScale();
    //                        else
    //                            return _targetSocket.InverseWorldMatrix.Translation.AsTranslationMatrix();
    //                    }
    //                case ESpace.Screen:
    //                    {
    //                        ICamera c = OwningWorld.GameMode.LocalPlayers[0].ViewportCamera;
    //                        Matrix4 mtx = c.CameraToWorldSpaceMatrix;
    //                        mtx.Translation = _targetSocket.InverseWorldMatrix.Translation;
    //                        return mtx;
    //                    }
    //                case ESpace.World:
    //                default:
    //                    {
    //                        return _targetSocket.InverseWorldMatrix.Translation.AsTranslationMatrix();
    //                    }
    //            }
    //        }

    //        public static void DestroyInstance()
    //        {
    //            Instance.Despawn();
    //        }
    //        /// <summary>
    //        /// 
    //        /// </summary>
    //        /// <param name="world">The world to spawn the transform tool in.</param>
    //        /// <param name="comp"></param>
    //        /// <param name="transformType"></param>
    //        /// <returns></returns>
    //        public static TransformTool3D GetInstance(TransformComponent comp, TransformType transformType)
    //        {
    //            IWorld world = comp?.OwningWorld;
    //            if (world is null)
    //                return null;

    //            if (!Instance.IsSpawnedIn(world))
    //            {
    //                Instance.Despawn();
    //                world.SpawnActor(Instance);
    //            }

    //            Instance.TargetSocket = comp;
    //            Instance.TransformMode = transformType;

    //            return Instance;
    //        }

    //        private void TransformChanged(ISocket socket)
    //        {
    //            if (!_pressed)
    //            {
    //                _pressed = true;
    //                RootComponent.SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
    //                //_dragMatrix = RootComponent.WorldMatrix;
    //                //_invDragMatrix = RootComponent.InverseWorldMatrix;
    //                _pressed = false;
    //            }
    //        }

    //        //public override void OnSpawnedPreComponentSetup()
    //        //{
    //        //    //OwningWorld.Scene.Add(this);
    //        //}
    //        //public override void OnDespawned()
    //        //{
    //        //    //OwningWorld.Scene.Remove(this);
    //        //}

    //        private BoolVector3 _hiAxis;
    //        private bool _hiCam, _hiSphere;
    //        private const int _circlePrecision = 20;
    //        private const float _orbRadius = 1.0f;
    //        private const float _circRadius = _orbRadius * _circOrbScale;
    //        private const float _screenTransExtent = _orbRadius * 0.1f;
    //        private const float _axisSnapRange = 7.0f;
    //        private const float _selectRange = 0.03f; //Selection error range for orb and circ
    //        private const float _axisSelectRange = 0.1f; //Selection error range for axes
    //        private const float _selectOrbScale = _selectRange / _orbRadius;
    //        private const float _circOrbScale = 1.2f;
    //        private const float _axisLength = _orbRadius * 2.0f;
    //        private const float _axisHalfLength = _orbRadius * 0.75f;
    //        private const float _coneRadius = _orbRadius * 0.1f;
    //        private const float _coneDistance = _orbRadius * 1.5f;
    //        private const float _scaleHalf1LDist = _orbRadius * 0.8f;
    //        private const float _scaleHalf2LDist = _orbRadius * 1.2f;

    //        Vector3 _lastPointWorld;
    //        Vector3 _localDragPlaneNormal;

    //        private Action _mouseUp, _mouseDown;
    //        private DelDrag _drag;
    //        private DelHighlight _highlight;
    //        private delegate bool DelHighlight(ICamera camera, Ray localRay);
    //        private delegate void DelDrag(Vector3 dragPoint);
    //        private delegate void DelDragRot(Quat dragPoint);

    //        #region Drag
    //        private bool 
    //            _snapRotations = false,
    //            _snapTranslations = false,
    //            _snapScale = false;
    //        private float _rotationSnapBias = 0.0f;
    //        private float _rotationSnapInterval = 5.0f;
    //        private float _translationSnapBias = 0.0f;
    //        private float _translationSnapInterval = 30.0f;
    //        private float _scaleSnapBias = 0.0f;
    //        private float _scaleSnapInterval = 0.25f;
    //        private void DragRotation(Vector3 dragPointWorld)
    //        {
    //            TMath.AxisAngleBetween(_lastPointWorld, dragPointWorld, out Vector3 axis, out float angle);

    //            //if (angle == 0.0f)
    //            //    return;

    //            //if (_snapRotations)
    //            //    angle = angle.RoundToNearest(_rotationSnapBias, _rotationSnapInterval);

    //            Quat worldDelta = Quat.FromAxisAngleDeg(axis, angle);

    //            //TODO: convert to socket space

    //            _targetSocket.Rotation *= worldDelta;

    //            RootComponent.SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
    //        }

    //        private void DragTranslation(Vector3 dragPointWorld)
    //        {
    //            Vector3 worldDelta = dragPointWorld - _lastPointWorld;

    //            //Matrix4 m = _targetSocket.InverseWorldMatrix.ClearScale();
    //            //m = m.ClearTranslation();
    //            //Vector3 worldTrans = m * delta;

    //            //if (_snapTranslations)
    //            //{
    //            //    //Modify delta to move resulting world point to nearest snap
    //            //    Vector3 worldPoint = _targetSocket.WorldMatrix.Translation;
    //            //    Vector3 resultPoint = worldPoint + worldTrans;

    //            //    resultPoint.X = resultPoint.X.RoundToNearest(_translationSnapBias, _translationSnapInterval);
    //            //    resultPoint.Y = resultPoint.Y.RoundToNearest(_translationSnapBias, _translationSnapInterval);
    //            //    resultPoint.Z = resultPoint.Z.RoundToNearest(_translationSnapBias, _translationSnapInterval);

    //            //    worldTrans = resultPoint - worldPoint;
    //            //}

    //            //TODO: convert world delta to local socket delta
    //            if (_targetSocket != null)
    //                _targetSocket.Transform.Translation.Value += worldDelta;

    //            RootComponent.SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
    //        }
    //        private void DragScale(Vector3 dragPointWorld)
    //        {
    //            Vector3 worldDelta = dragPointWorld - _lastPointWorld;

    //            //TODO: better method for scaling

    //            _targetSocket.Transform.Scale.Value += worldDelta;

    //            RootComponent.SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
    //        }
    //        /// <summary>
    //        /// Returns a point relative to the local space of the target socket (origin at 0,0,0), clamped to the highlighted drag plane.
    //        /// </summary>
    //        /// <param name="camera">The camera viewing this tool, used for camera space drag clamping.</param>
    //        /// <param name="localRay">The mouse ray, transformed into the socket's local space.</param>
    //        /// <returns></returns>
    //        private Vector3 GetLocalDragPoint(ICamera camera, Ray localRay)
    //        {
    //            //Convert all coordinates to local space

    //            Vector3 localCamPoint = camera.WorldPoint * RootComponent.InverseWorldMatrix;
    //            Vector3 localDragPoint, unit;

    //            switch (_mode)
    //            {
    //                case TransformType.Scale:
    //                case TransformType.Translate:
    //                    {
    //                        if (_hiCam)
    //                        {
    //                            _localDragPlaneNormal = localCamPoint;
    //                            _localDragPlaneNormal.Normalize();
    //                        }
    //                        else if (_hiAxis.X)
    //                        {
    //                            if (_hiAxis.Y)
    //                            {
    //                                _localDragPlaneNormal = Vector3.UnitZ;
    //                            }
    //                            else if (_hiAxis.Z)
    //                            {
    //                                _localDragPlaneNormal = Vector3.UnitY;
    //                            }
    //                            else
    //                            {
    //                                unit = Vector3.UnitX;
    //                                Vector3 perpPoint = Ray.GetClosestColinearPoint(Vector3.Zero, unit, localCamPoint);
    //                                _localDragPlaneNormal = localCamPoint - perpPoint;
    //                                _localDragPlaneNormal.Normalize();

    //                                if (!Collision.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
    //                                    return _lastPointWorld;

    //                                return Ray.GetClosestColinearPoint(Vector3.Zero, unit, localDragPoint);
    //                            }
    //                        }
    //                        else if (_hiAxis.Y)
    //                        {
    //                            if (_hiAxis.X)
    //                            {
    //                                _localDragPlaneNormal = Vector3.UnitZ;
    //                            }
    //                            else if (_hiAxis.Z)
    //                            {
    //                                _localDragPlaneNormal = Vector3.UnitX;
    //                            }
    //                            else
    //                            {
    //                                unit = Vector3.UnitY;
    //                                Vector3 perpPoint = Ray.GetClosestColinearPoint(Vector3.Zero, unit, localCamPoint);
    //                                _localDragPlaneNormal = localCamPoint - perpPoint;
    //                                _localDragPlaneNormal.Normalize();

    //                                if (!Collision.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
    //                                    return _lastPointWorld;

    //                                return Ray.GetClosestColinearPoint(Vector3.Zero, unit, localDragPoint);
    //                            }
    //                        }
    //                        else if (_hiAxis.Z)
    //                        {
    //                            if (_hiAxis.X)
    //                            {
    //                                _localDragPlaneNormal = Vector3.UnitY;
    //                            }
    //                            else if (_hiAxis.Y)
    //                            {
    //                                _localDragPlaneNormal = Vector3.UnitX;
    //                            }
    //                            else
    //                            {
    //                                unit = Vector3.UnitZ;
    //                                Vector3 perpPoint = Ray.GetClosestColinearPoint(Vector3.Zero, unit, localCamPoint);
    //                                _localDragPlaneNormal = localCamPoint - perpPoint;
    //                                _localDragPlaneNormal.Normalize();

    //                                if (!Collision.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
    //                                    return _lastPointWorld;

    //                                return Ray.GetClosestColinearPoint(Vector3.Zero, unit, localDragPoint);
    //                            }
    //                        }

    //                        if (Collision.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
    //                            return localDragPoint;
    //                    }
    //                    break;
    //                case TransformType.Rotate:
    //                    {
    //                        if (_hiCam)
    //                        {
    //                            _localDragPlaneNormal = localCamPoint;
    //                            _localDragPlaneNormal.Normalize();

    //                            if (Collision.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
    //                                return localDragPoint;
    //                        }
    //                        else if (_hiAxis.Any)
    //                        {
    //                            if (_hiAxis.X)
    //                                unit = Vector3.UnitX;
    //                            else if (_hiAxis.Y)
    //                                unit = Vector3.UnitY;
    //                            else// if (_hiAxis.Z)
    //                                unit = Vector3.UnitZ;

    //                            _localDragPlaneNormal = unit;
    //                            _localDragPlaneNormal.Normalize();

    //                            if (Collision.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, _localDragPlaneNormal, out localDragPoint))
    //                                return localDragPoint;
    //                        }
    //                        else if (_hiSphere)
    //                        {
    //                            Vector3 worldPoint = RootComponent.WorldPoint;
    //                            float radius = camera.DistanceScale(worldPoint, _orbRadius);

    //                            if (Collision.RayIntersectsSphere(localRay.StartPoint, localRay.Direction, Vector3.Zero, radius * _circOrbScale, out localDragPoint))
    //                            {
    //                                _localDragPlaneNormal = localDragPoint.Normalized();
    //                                return localDragPoint;
    //                            }
    //                        }
    //                    }
    //                    break;
    //            }

    //            return _lastPointWorld;
    //        }
    //#endregion

    //        #region Highlighting
    //        private bool HighlightRotation(ICamera camera, Ray localRay)
    //        {
    //            Vector3 worldPoint = RootComponent.WorldMatrix.Translation;
    //            float radius = camera.DistanceScale(worldPoint, _orbRadius);

    //            if (!Collision.RayIntersectsSphere(localRay.StartPoint, localRay.Direction, Vector3.Zero, radius * _circOrbScale, out Vector3 point))
    //            {
    //                //If no intersect is found, project the ray through the plane perpendicular to the camera.
    //                //localRay.LinePlaneIntersect(Vector3.Zero, (camera.WorldPoint - worldPoint).Normalized(), out point);
    //                Collision.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, (camera.WorldPoint - worldPoint) * RootComponent.InverseWorldMatrix, out point);

    //                //Clamp the point to edge of the sphere
    //                point = Ray.PointAtLineDistance(Vector3.Zero, point, radius);

    //                //Point lies on circ line?
    //                float distance = point.LengthFast;
    //                if (Math.Abs(distance - radius * _circOrbScale) < radius * _selectOrbScale)
    //                    _hiCam = true;
    //            }
    //            else
    //            {
    //                point.NormalizeFast();

    //                _hiSphere = true;

    //                float x = point.Dot(Vector3.UnitX);
    //                float y = point.Dot(Vector3.UnitY);
    //                float z = point.Dot(Vector3.UnitZ);

    //                if (Math.Abs(x) < 0.3f)
    //                {
    //                    _hiAxis.X = true;
    //                }
    //                else if (Math.Abs(y) < 0.3f)
    //                {
    //                    _hiAxis.Y = true;
    //                }
    //                else if (Math.Abs(z) < 0.3f)
    //                {
    //                    _hiAxis.Z = true;
    //                }
    //            }

    //            return _hiAxis.Any || _hiCam || _hiSphere;
    //        }
    //        private bool HighlightTranslation(ICamera camera, Ray localRay)
    //        {
    //            Vector3 worldPoint = RootComponent.WorldMatrix.Translation;
    //            float radius = camera.DistanceScale(worldPoint, _orbRadius);

    //            List<Vector3> intersectionPoints = new List<Vector3>(3);

    //            bool snapFound = false;
    //            for (int normalAxis = 0; normalAxis < 3; ++normalAxis)
    //            {
    //                Vector3 unit = Vector3.Zero;
    //                unit[normalAxis] = localRay.StartPoint[normalAxis] < 0.0f ? -1.0f : 1.0f;

    //                //Get plane intersection point for cursor ray and each drag plane
    //                if (Collision.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, unit, out Vector3 point))
    //                    intersectionPoints.Add(point);
    //            }

    //            //_intersectionPoints.Sort((l, r) => l.DistanceToSquared(camera.WorldPoint).CompareTo(r.DistanceToSquared(camera.WorldPoint)));

    //            foreach (Vector3 v in intersectionPoints)
    //            {
    //                Vector3 diff = v / radius;
    //                //int planeAxis1 = normalAxis + 1 - (normalAxis >> 1) * 3;    //0 = 1, 1 = 2, 2 = 0
    //                //int planeAxis2 = planeAxis1 + 1 - (normalAxis  & 1) * 3;    //0 = 2, 1 = 0, 2 = 1

    //                if (diff.X > -_axisSelectRange && diff.X <= _axisLength &&
    //                    diff.Y > -_axisSelectRange && diff.Y <= _axisLength &&
    //                    diff.Z > -_axisSelectRange && diff.Z <= _axisLength)
    //                {
    //                    float errorRange = _axisSelectRange;

    //                    _hiAxis.X = diff.X > _axisHalfLength && Math.Abs(diff.Y) < errorRange && Math.Abs(diff.Z) < errorRange;
    //                    _hiAxis.Y = diff.Y > _axisHalfLength && Math.Abs(diff.X) < errorRange && Math.Abs(diff.Z) < errorRange;
    //                    _hiAxis.Z = diff.Z > _axisHalfLength && Math.Abs(diff.X) < errorRange && Math.Abs(diff.Y) < errorRange;

    //                    if (snapFound = _hiAxis.Any)
    //                        break;

    //                    if (diff.X < _axisHalfLength &&
    //                        diff.Y < _axisHalfLength &&
    //                        diff.Z < _axisHalfLength)
    //                    {
    //                        //Point lies inside the double drag areas
    //                        _hiAxis.X = diff.X > _axisSelectRange;
    //                        _hiAxis.Y = diff.Y > _axisSelectRange;
    //                        _hiAxis.Z = diff.Z > _axisSelectRange;
    //                        _hiCam = _hiAxis.None;

    //                        snapFound = true;
    //                        break;
    //                    }
    //                }
    //            }

    //            return snapFound;
    //        }
    //        private bool HighlightScale(ICamera camera, Ray localRay)
    //        {
    //            Vector3 worldPoint = RootComponent.WorldMatrix.Translation;
    //            float radius = camera.DistanceScale(worldPoint, _orbRadius);

    //            List<Vector3> intersectionPoints = new List<Vector3>(3);

    //            bool snapFound = false;
    //            for (int normalAxis = 0; normalAxis < 3; ++normalAxis)
    //            {
    //                Vector3 unit = Vector3.Zero;
    //                unit[normalAxis] = localRay.StartPoint[normalAxis] < 0.0f ? -1.0f : 1.0f;

    //                //Get plane intersection point for cursor ray and each drag plane
    //                if (Collision.RayIntersectsPlane(localRay.StartPoint, localRay.Direction, Vector3.Zero, unit, out Vector3 point))
    //                    intersectionPoints.Add(point);
    //            }

    //            //_intersectionPoints.Sort((l, r) => l.DistanceToSquared(camera.WorldPoint).CompareTo(r.DistanceToSquared(camera.WorldPoint)));

    //            foreach (Vector3 v in intersectionPoints)
    //            {
    //                Vector3 diff = v / radius;
    //                //int planeAxis1 = normalAxis + 1 - (normalAxis >> 1) * 3;    //0 = 1, 1 = 2, 2 = 0
    //                //int planeAxis2 = planeAxis1 + 1 - (normalAxis  & 1) * 3;    //0 = 2, 1 = 0, 2 = 1

    //                if (diff.X > -_axisSelectRange && diff.X <= _axisLength &&
    //                    diff.Y > -_axisSelectRange && diff.Y <= _axisLength &&
    //                    diff.Z > -_axisSelectRange && diff.Z <= _axisLength)
    //                {
    //                    float errorRange = _axisSelectRange;

    //                    _hiAxis.X = diff.X > _axisHalfLength && Math.Abs(diff.Y) < errorRange && Math.Abs(diff.Z) < errorRange;
    //                    _hiAxis.Y = diff.Y > _axisHalfLength && Math.Abs(diff.X) < errorRange && Math.Abs(diff.Z) < errorRange;
    //                    _hiAxis.Z = diff.Z > _axisHalfLength && Math.Abs(diff.X) < errorRange && Math.Abs(diff.Y) < errorRange;

    //                    if (snapFound = _hiAxis.Any)
    //                        break;

    //                    //Determine if the point is in the double or triple drag triangles
    //                    float halfDist = _scaleHalf2LDist;
    //                    float centerDist = _scaleHalf1LDist;
    //                    if (diff.IsInTriangle(new Vector3(), new Vector3(halfDist, 0, 0), new Vector3(0, halfDist, 0)))
    //                        if (diff.IsInTriangle(new Vector3(), new Vector3(centerDist, 0, 0), new Vector3(0, centerDist, 0)))
    //                            _hiAxis.X = _hiAxis.Y = _hiAxis.Z = true;
    //                        else
    //                            _hiAxis.X = _hiAxis.Y = true;
    //                    else if (diff.IsInTriangle(new Vector3(), new Vector3(halfDist, 0, 0), new Vector3(0, 0, halfDist)))
    //                        if (diff.IsInTriangle(new Vector3(), new Vector3(centerDist, 0, 0), new Vector3(0, 0, centerDist)))
    //                            _hiAxis.X = _hiAxis.Y = _hiAxis.Z = true;
    //                        else
    //                            _hiAxis.X = _hiAxis.Y = true;
    //                    else if (diff.IsInTriangle(new Vector3(), new Vector3(0, halfDist, 0), new Vector3(0, 0, halfDist)))
    //                        if (diff.IsInTriangle(new Vector3(), new Vector3(0, centerDist, 0), new Vector3(0, 0, centerDist)))
    //                            _hiAxis.X = _hiAxis.Y = _hiAxis.Z = true;
    //                        else
    //                            _hiAxis.Y = _hiAxis.Z = true;

    //                    snapFound = _hiAxis.Any;

    //                    if (snapFound)
    //                        break;
    //                }
    //            }

    //            return snapFound;
    //        }
    //        #endregion

    //        private bool _pressed = false;
    //        //private Matrix4 _dragMatrix, _invDragMatrix;

    //        /// <summary>
    //        /// Returns true if intersecting one of the transform tool's various parts.
    //        /// </summary>
    //        public bool MouseMove(Ray cursor, ICamera camera, bool pressed)
    //        {
    //            bool snapFound = true;
    //            if (pressed)
    //            {
    //                if (_hiAxis.None && !_hiCam && !_hiSphere)
    //                    return false;

    //                if (!_pressed)
    //                    OnPressed();

    //                Ray localRay = cursor.TransformedBy(RootComponent.InverseWorldMatrix);
    //                Vector3 localDragPoint = GetLocalDragPoint(camera, localRay);
    //                Vector3 worldDragPoint = Vector3.TransformPosition(localDragPoint, RootComponent.WorldMatrix);
    //                _drag(worldDragPoint);

    //                _lastPointWorld = worldDragPoint;
    //            }
    //            else
    //            {
    //                if (_pressed)
    //                    OnReleased();

    //                Ray localRay = cursor.TransformedBy(RootComponent.InverseWorldMatrix);

    //                _hiAxis.X = _hiAxis.Y = _hiAxis.Z = false;
    //                _hiCam = _hiSphere = false;

    //                snapFound = _highlight?.Invoke(camera, localRay) ?? false;

    //                _axisMat[0].Parameter<ShaderVector4>(0).Value = _hiAxis.X ? (ColorF4)Color.Yellow : Color.Red;
    //                _axisMat[1].Parameter<ShaderVector4>(0).Value = _hiAxis.Y ? (ColorF4)Color.Yellow : Color.Green;
    //                _axisMat[2].Parameter<ShaderVector4>(0).Value = _hiAxis.Z ? (ColorF4)Color.Yellow : Color.Blue;
    //                _screenMat.Parameter<ShaderVector4>(0).Value = _hiCam ? (ColorF4)Color.Yellow : Color.LightGray;

    //                GetDependentColors();

    //                Vector3 localDragPoint = GetLocalDragPoint(camera, localRay);
    //                _lastPointWorld = Vector3.TransformPosition(localDragPoint, RootComponent.WorldMatrix);
    //            }
    //            return snapFound;
    //        }
    //        private void GetDependentColors()
    //        {
    //            if (TransformMode != TransformType.Rotate)
    //            {
    //                if (TransformMode == TransformType.Translate)
    //                {
    //                    _transPlaneMat[0].Parameter<ShaderVector4>(0).Value = _hiAxis.X && _hiAxis.Y ? (ColorF4)Color.Yellow : Color.Red;
    //                    _transPlaneMat[1].Parameter<ShaderVector4>(0).Value = _hiAxis.X && _hiAxis.Z ? (ColorF4)Color.Yellow : Color.Red;
    //                    _transPlaneMat[2].Parameter<ShaderVector4>(0).Value = _hiAxis.Y && _hiAxis.Z ? (ColorF4)Color.Yellow : Color.Green;
    //                    _transPlaneMat[3].Parameter<ShaderVector4>(0).Value = _hiAxis.Y && _hiAxis.X ? (ColorF4)Color.Yellow : Color.Green;
    //                    _transPlaneMat[4].Parameter<ShaderVector4>(0).Value = _hiAxis.Z && _hiAxis.X ? (ColorF4)Color.Yellow : Color.Blue;
    //                    _transPlaneMat[5].Parameter<ShaderVector4>(0).Value = _hiAxis.Z && _hiAxis.Y ? (ColorF4)Color.Yellow : Color.Blue;
    //                }
    //                else
    //                {
    //                    _scalePlaneMat[0].Parameter<ShaderVector4>(0).Value = _hiAxis.Y && _hiAxis.Z ? (ColorF4)Color.Yellow : Color.Red;
    //                    _scalePlaneMat[1].Parameter<ShaderVector4>(0).Value = _hiAxis.X && _hiAxis.Z ? (ColorF4)Color.Yellow : Color.Green;
    //                    _scalePlaneMat[2].Parameter<ShaderVector4>(0).Value = _hiAxis.X && _hiAxis.Y ? (ColorF4)Color.Yellow : Color.Blue;
    //                }
    //            }
    //        }

    //        [Browsable(false)]
    //        public Matrix4 PrevRootWorldMatrix { get; private set; } = Matrix4.Identity;
    //        private void OnPressed()
    //        {
    //            if (_targetSocket != null)
    //            {
    //                RootComponent.SetWorldMatrices(GetSocketSpacialTransform(), GetSocketSpacialTransformInverse());
    //                PrevRootWorldMatrix = _targetSocket.WorldMatrix;
    //            }
    //            else
    //            {
    //                RootComponent.SetWorldMatrices(Matrix4.Identity, Matrix4.Identity);
    //                PrevRootWorldMatrix = Matrix4.Identity;
    //            }

    //            _pressed = true;
    //            MouseDown?.Invoke();
    //        }
    //        private void OnReleased()
    //        {
    //            _pressed = false;
    //            MouseUp?.Invoke();
    //        }

    //        public event Action MouseDown, MouseUp;

    //        //UIString2D _xText, _yText, _zText;

    //        private void Render(bool shadowPass)
    //        {
    //            //if (_hiCam || _hiSphere || _hiAxis.Any)
    //            //{
    //            //    Api.RenderPoint(_lastPointWorld, Color.Black, false);
    //            //    Vector3 worldNormal = Vector3.TransformVector(_localDragPlaneNormal, RootComponent.WorldMatrix);
    //            //    Api.RenderLine(_lastPointWorld, _lastPointWorld + worldNormal * Api.CurrentCamera.DistanceScale(RootComponent.WorldPoint, 2.0f), Color.Black, false);
    //            //}
    //        }

    //        private readonly RenderCommandMethod3D _rc;
    //        public void AddRenderables(RenderCommandCollection passes, ICamera camera) => passes.Add(_rc);
    //    }
}
