using Assimp;
using Silk.NET.Input;
using System.Collections.Concurrent;
using System.Numerics;
using XREngine.Actors.Types;
using XREngine.Animation;
using XREngine.Components;
using XREngine.Components.Lights;
using XREngine.Components.Scene;
using XREngine.Components.Scene.Mesh;
using XREngine.Components.Scene.Transforms;
using XREngine.Data;
using XREngine.Data.Colors;
using XREngine.Data.Components;
using XREngine.Data.Components.Scene;
using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Editor.UI.Components;
using XREngine.Editor.UI.Toolbar;
using XREngine.Rendering;
using XREngine.Rendering.Models;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Physics.Physx;
using XREngine.Rendering.UI;
using XREngine.Scene;
using XREngine.Scene.Components.Animation;
using XREngine.Scene.Components.Physics;
using XREngine.Scene.Components.VR;
using XREngine.Scene.Transforms;
using static XREngine.Audio.AudioSource;
using static XREngine.Scene.Transforms.RigidBodyTransform;
using BlendMode = XREngine.Rendering.Models.Materials.BlendMode;
using Quaternion = System.Numerics.Quaternion;

namespace XREngine.Editor;

public static class EditorWorld
{
    //Unit testing toggles
    public const bool VisualizeOctree = false;
    public const bool VisualizeQuadtree = false;
    public const bool Physics = true;
    public const bool DirLight = true;
    public const bool SpotLight = false;
    public const bool DirLight2 = false;
    public const bool PointLight = false;
    public const bool SoundNode = true;
    public const bool LightProbe = true; //Adds a test light probe to the scene for PBR lighting.
    public const bool Skybox = true;
    public const bool Spline = false; //Adds a 3D spline to the scene.
    public const bool DeferredDecal = false; //Adds a deferred decal to the scene.
    public const bool StaticModel = false; //Imports a scene model to be rendered.
    public const bool AnimatedModel = true; //Imports a character model to be animated.
    public const bool AddEditorUI = true; //Adds the full editor UI to the camera. Probably don't use this one a character pawn.
    public const bool VRPawn = false; //Enables VR input and pawn.
    public const bool CharacterPawn = false; //Enables the player to physically locomote in the world. Requires a physical floor.
    public const bool ThirdPersonPawn = false; //If on desktop and character pawn is enabled, this will add a third person camera instead of first person.
    public const bool TestAnimation = true; //Adds test animations to the character pawn.
    public const bool PhysicsChain = false; //Adds a jiggle physics chain to the character pawn.
    public const bool TransformTool = false; //Adds the transform tool to the scene for testing dragging and rotating etc.
    public const bool AllowEditingInVR = false; //Allows the user to edit the scene from desktop in VR.

    /// <summary>
    /// Creates a test world with a variety of objects for testing purposes.
    /// </summary>
    /// <returns></returns>
    public static XRWorld CreateUnitTestWorld()
    {
        Engine.Rendering.Settings.AllowBlendshapes = true;
        Engine.Rendering.Settings.AllowSkinning = true;

        string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        //UnityPackageExtractor.ExtractAsync(Path.Combine(desktopDir, "Animations.unitypackage"), Path.Combine(desktopDir, "Extracted"), true);

        //var anim = Engine.Assets.Load<Unity.UnityAnimationClip>(Path.Combine(desktopDir, "walk.anim"));
        //if (anim is not null)
        //{
        //    var anims = UnityConverter.ConvertFloatAnimation(anim);
        //}

        var scene = new XRScene("Main Scene");
        var rootNode = new SceneNode("Root Node");
        scene.RootNodes.Add(rootNode);

        if (VisualizeOctree)
            rootNode.AddComponent<DebugVisualizeOctreeComponent>();

        SceneNode? characterPawnNode = null;
        if (VRPawn)
        {
            if (CharacterPawn)
            {
                characterPawnNode = CreateCharacterVRPawn(rootNode);
                if (AllowEditingInVR)
                {
                    SceneNode cameraNode = CreateCamera(rootNode, out var camComp);
                    var pawn = CreateDesktopViewerPawn(cameraNode);
                    CreateEditorUI(rootNode, camComp!, pawn);
                }
            }
            else
                CreateFlyingVRPawn(rootNode);
        }
        else if (CharacterPawn)
            characterPawnNode = CreateDesktopCharacterPawn(rootNode);
        else
        {
            SceneNode cameraNode = CreateCamera(rootNode, out var camComp);
            var pawn = CreateDesktopViewerPawn(cameraNode);
            CreateEditorUI(rootNode, camComp!, pawn);
        }

        if (DirLight)
            AddDirLight(rootNode);
        if (SpotLight)
            AddSpotLight(rootNode);
        if (DirLight2)
            AddDirLight2(rootNode);
        if (PointLight)
            AddPointLight(rootNode);
        if (SoundNode)
            AddSoundNode(rootNode);
        if (LightProbe || Skybox)
        {
            string[] names = [/*"warm_restaurant_4k",*/ "overcast_soil_puresky_4k", /*"studio_small_09_4k",*/ "klippad_sunrise_2_4k", "satara_night_4k"];
            Random r = new();
            XRTexture2D skyEquirect = Engine.Assets.LoadEngineAsset<XRTexture2D>("Textures", $"{names[r.Next(0, names.Length - 1)]}.exr");

            if (LightProbe)
                AddLightProbe(rootNode);
            if (Skybox)
                AddSkybox(rootNode, skyEquirect);
        }
        if (Physics)
            AddPhysics(rootNode);
        if (Spline)
            AddSpline(rootNode);
        if (DeferredDecal)
            AddDeferredDecal(rootNode);
        ImportModels(desktopDir, rootNode, characterPawnNode ?? rootNode);
        
        return new XRWorld("Default World", scene);
    }

    //Pawns are what the player controls in the game world.
    #region Pawns

    #region VR

    private static SceneNode CreateCharacterVRPawn(SceneNode rootNode)
    {
        SceneNode vrPlayspaceNode = new(rootNode) { Name = "VRPlayspaceNode" };
        var characterTfm = vrPlayspaceNode.SetTransform<RigidBodyTransform>();
        characterTfm.InterpolationMode = EInterpolationMode.Interpolate;

        var characterComp = vrPlayspaceNode.AddComponent<CharacterPawnComponent>("TestPawn")!;
        var movementComp = vrPlayspaceNode.AddComponent<CharacterMovement3DComponent>()!;
        InitMovement(movementComp);

        //TODO: divert VR input from player 1 to this pawn instead of the flying editor pawn when AllowEditingInVR is true.
        if (!AllowEditingInVR)
            characterComp.EnqueuePossessionByLocalPlayer(ELocalPlayerIndex.One);

        SceneNode localRotationNode = vrPlayspaceNode.NewChild();
        var localRotationTfm = localRotationNode.SetTransform<Transform>();
        characterComp.IgnoreViewTransformPitch = true;
        characterComp.RotationTransform = localRotationTfm;

        var headsetNode = AddHeadsetNode(localRotationNode, characterComp);
        characterComp.ViewTransform = headsetNode.Transform;

        AddHandControllerNode(localRotationNode, true);
        AddHandControllerNode(localRotationNode, false);
        AddTrackerCollectionNode(localRotationNode);

        return vrPlayspaceNode;
    }

    private static void InitMovement(CharacterMovement3DComponent movementComp)
    {
        movementComp.StandingHeight = 1.89f;
        movementComp.SpawnPosition = new Vector3(0.0f, 10.0f, 0.0f);
        movementComp.Velocity = new Vector3(0.0f, 0.0f, 0.0f);
        movementComp.JumpSpeed = 1.0f;
        movementComp.GravityOverride = new Vector3(0.0f, -1.0f, 0.0f);
        movementComp.InputLerpSpeed = 0.9f;
    }

    private static void AddHandControllerNode(SceneNode parentNode, bool left)
    {
        SceneNode leftControllerNode = new(parentNode) { Name = "VRLeftControllerNode" };
        var leftControllerTfm = leftControllerNode.SetTransform<VRControllerTransform>();
        leftControllerTfm.LeftHand = left;
        var leftControllerModel = leftControllerNode.AddComponent<VRControllerModelComponent>()!;
        leftControllerModel.LeftHand = left;
    }
    private static void CreateFlyingVRPawn(SceneNode rootNode)
    {
        SceneNode vrPlayspaceNode = new(rootNode) { Name = "VRPlayspaceNode" };
        var playspaceTfm = vrPlayspaceNode.SetTransform<Transform>();
        AddHeadsetNode(vrPlayspaceNode);
        AddHandControllerNode(vrPlayspaceNode, true);
        AddHandControllerNode(vrPlayspaceNode, false);
        AddTrackerCollectionNode(vrPlayspaceNode);
    }
    private static void AddTrackerCollectionNode(SceneNode vrPlayspaceNode)
    {
        SceneNode trackerNode = new(vrPlayspaceNode) { Name = "VRTrackerNode" };
        trackerNode.AddComponent<VRTrackerCollectionComponent>();
    }
    private static SceneNode AddHeadsetNode(SceneNode parentNode, PawnComponent? pawn = null)
    {
        SceneNode vrHeadsetNode = new(parentNode) { Name = "VRHeadsetNode" };
        vrHeadsetNode.AddComponent<AudioListenerComponent>();
        var hmdTfm = vrHeadsetNode.SetTransform<VRHeadsetTransform>();
        var hmdComp = vrHeadsetNode.AddComponent<VRHeadsetComponent>()!;

        if (!AllowEditingInVR)
        {
            SceneNode firstPersonViewNode = new(vrHeadsetNode) { Name = "FirstPersonViewNode" };
            var firstPersonViewTfm = firstPersonViewNode.SetTransform<SmoothedParentConstraintTransform>();
            firstPersonViewTfm.TranslationInterpolationSpeed = null;
            firstPersonViewTfm.ScaleInterpolationSpeed = null;
            firstPersonViewTfm.SplitYPR = true;
            firstPersonViewTfm.YawInterpolationSpeed = 5.0f;
            firstPersonViewTfm.PitchInterpolationSpeed = 5.0f;
            firstPersonViewTfm.IgnoreRoll = true;
            firstPersonViewTfm.UseLookAtYawPitch = true;
            var firstPersonCam = firstPersonViewNode.AddComponent<CameraComponent>()!;
            var persp = firstPersonCam.Camera.Parameters as XRPerspectiveCameraParameters;
            persp!.HorizontalFieldOfView = 40.0f;
            persp.NearZ = 0.1f;
            persp.FarZ = 100000.0f;
            firstPersonCam.CullWithFrustum = true;
            if (pawn is null)
                firstPersonCam.SetAsPlayerView(ELocalPlayerIndex.One);
            else
                pawn.CameraComponent = firstPersonCam;
            CreateEditorUI(vrHeadsetNode, firstPersonCam);
        }

        return vrHeadsetNode;
    }
    #endregion

    #region Desktop

    private static EditorFlyingCameraPawnComponent CreateDesktopViewerPawn(SceneNode cameraNode)
    {
        var pawnComp = cameraNode.AddComponent<EditorFlyingCameraPawnComponent>();
        var listener = cameraNode.AddComponent<AudioListenerComponent>()!;
        //listener.Gain = 1.0f;
        //listener.DistanceModel = DistanceModel.LinearDistanceClamped;

        pawnComp!.Name = "TestPawn";
        pawnComp.EnqueuePossessionByLocalPlayer(ELocalPlayerIndex.One);
        return pawnComp;
    }

    private static SceneNode CreateDesktopCharacterPawn(SceneNode rootNode)
    {
        SceneNode characterNode = new(rootNode) { Name = "TestPlayerNode" };
        var characterTfm = characterNode.SetTransform<RigidBodyTransform>();
        characterTfm.InterpolationMode = EInterpolationMode.Interpolate;

        //create node to translate camera up half the height of the character
        SceneNode cameraOffsetNode = new(characterNode) { Name = "TestCameraOffsetNode" };
        var cameraOffsetTfm = cameraOffsetNode.SetTransform<Transform>();
        cameraOffsetTfm.Translation = new Vector3(0.0f, 1.0f, 0.0f);

        SceneNode cameraParentNode;
        if (ThirdPersonPawn)
        {
            //Create camera boom with sphere shapecast
            SceneNode cameraViewTransformNode = new(cameraOffsetNode) { Name = "TestCameraViewTransformNode" };
            var cameraViewTransformTfm = cameraViewTransformNode.SetTransform<Transform>();
            SceneNode cameraBoomNode = new(cameraViewTransformNode) { Name = "TestCameraBoomNode" };
            var cameraBoomTfm = cameraBoomNode.SetTransform<BoomTransform>();
            cameraParentNode = cameraBoomNode;
        }
        else
            cameraParentNode = cameraOffsetNode;

        SceneNode cameraNode = CreateCamera(cameraParentNode, out CameraComponent? camComp, false);
        cameraNode.AddComponent<AudioListenerComponent>();

        var characterComp = characterNode.AddComponent<CharacterPawnComponent>("TestPawn")!;
        characterComp.CameraComponent = camComp;

        if (ThirdPersonPawn)
            characterComp.ViewTransform = cameraParentNode.Parent.GetTransformAs<Transform>()!;

        var movementComp = characterNode.AddComponent<CharacterMovement3DComponent>()!;
        InitMovement(movementComp);
        characterComp.EnqueuePossessionByLocalPlayer(ELocalPlayerIndex.One);
        if (camComp is not null)
            CreateEditorUI(characterNode, camComp);
        return characterNode;
    }

    #endregion

    private static SceneNode CreateCamera(SceneNode parentNode, out CameraComponent? camComp, bool smoothed = true)
    {
        var cameraNode = new SceneNode(parentNode, "TestCameraNode");

        if (smoothed)
        {
            var laggedTransform = cameraNode.GetTransformAs<SmoothedTransform>(true)!;
            laggedTransform.RotationSmoothingSpeed = 30.0f;
            laggedTransform.TranslationSmoothingSpeed = 30.0f;
            laggedTransform.ScaleSmoothingSpeed = 30.0f;
        }

        if (cameraNode.TryAddComponent(out camComp, "TestCamera"))
            camComp!.SetPerspective(60.0f, 0.1f, 100000.0f, null);
        else
            camComp = null;

        return cameraNode;
    }

    #endregion

    //All tests pertaining to shading the scene.
    #region Shading Tests

    //Code for lighting the scene.
    #region Lights

    private static void AddLightProbe(SceneNode rootNode)
    {
        var probe = new SceneNode(rootNode) { Name = "TestLightProbeNode" };
        var probeTransform = probe.SetTransform<Transform>();
        probeTransform.Translation = new Vector3(0.0f, 20.0f, 0.0f);
        if (!probe.TryAddComponent<LightProbeComponent>(out var probeComp))
            return;

        probeComp!.Name = "TestLightProbe";
        probeComp.SetCaptureResolution(128, false);
        probeComp.RealtimeCapture = true;
        probeComp.PreviewDisplay = LightProbeComponent.ERenderPreview.Irradiance;
        probeComp.RealTimeCaptureUpdateInterval = TimeSpan.FromSeconds(1.0f);
    }

    private static void AddDirLight(SceneNode rootNode)
    {
        var dirLightNode = new SceneNode(rootNode) { Name = "TestDirectionalLightNode" };
        var dirLightTransform = dirLightNode.SetTransform<Transform>();
        dirLightTransform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
        //Face the light directly down
        dirLightTransform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, XRMath.DegToRad(-70.0f));
        //dirLightTransform.RegisterAnimationTick<Transform>(t => t.Rotation *= Quaternion.CreateFromAxisAngle(Globals.Backward, Engine.DilatedDelta));
        if (!dirLightNode.TryAddComponent<DirectionalLightComponent>(out var dirLightComp))
            return;

        dirLightComp!.Name = "TestDirectionalLight";
        dirLightComp.Color = new Vector3(1, 1, 1);
        dirLightComp.DiffuseIntensity = 1.0f;
        dirLightComp.Scale = new Vector3(1000.0f, 1000.0f, 1000.0f);
        dirLightComp.CastsShadows = true;
        dirLightComp.SetShadowMapResolution(4096, 4096);
    }

    private static void AddDirLight2(SceneNode rootNode)
    {
        var dirLightNode2 = new SceneNode(rootNode) { Name = "TestDirectionalLightNode2" };
        var dirLightTransform2 = dirLightNode2.SetTransform<Transform>();
        dirLightTransform2.Translation = new Vector3(0.0f, 10.0f, 0.0f);
        dirLightTransform2.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2.0f);
        if (!dirLightNode2.TryAddComponent<DirectionalLightComponent>(out var dirLightComp2))
            return;

        dirLightComp2!.Name = "TestDirectionalLight2";
        dirLightComp2.Color = new Vector3(1.0f, 0.8f, 0.8f);
        dirLightComp2.DiffuseIntensity = 1.0f;
        dirLightComp2.Scale = new Vector3(1000.0f, 1000.0f, 1000.0f);
        dirLightComp2.CastsShadows = false;
    }

    private static void AddSpotLight(SceneNode rootNode)
    {
        var spotLightNode = new SceneNode(rootNode) { Name = "TestSpotLightNode" };
        var spotLightTransform = spotLightNode.SetTransform<Transform>();
        spotLightTransform.Translation = new Vector3(0.0f, 10.0f, 0.0f);
        spotLightTransform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, XRMath.DegToRad(-90.0f));
        if (!spotLightNode.TryAddComponent<SpotLightComponent>(out var spotLightComp))
            return;

        spotLightComp!.Name = "TestSpotLight";
        spotLightComp.Color = new Vector3(1.0f, 1.0f, 1.0f);
        spotLightComp.DiffuseIntensity = 10.0f;
        spotLightComp.Brightness = 5.0f;
        spotLightComp.Distance = 40.0f;
        spotLightComp.SetCutoffs(10, 40);
        spotLightComp.CastsShadows = true;
        spotLightComp.SetShadowMapResolution(2048, 2048);
    }

    private static void AddPointLight(SceneNode rootNode)
    {
        var pointLight = new SceneNode(rootNode) { Name = "TestPointLightNode" };
        var pointLightTransform = pointLight.SetTransform<Transform>();
        pointLightTransform.Translation = new Vector3(0.0f, 2.0f, 0.0f);
        if (!pointLight.TryAddComponent<PointLightComponent>(out var pointLightComp))
            return;

        pointLightComp!.Name = "TestPointLight";
        pointLightComp.Color = new Vector3(1.0f, 1.0f, 1.0f);
        pointLightComp.DiffuseIntensity = 10.0f;
        pointLightComp.Brightness = 10.0f;
        pointLightComp.Radius = 10000.0f;
        pointLightComp.CastsShadows = true;
        pointLightComp.SetShadowMapResolution(1024, 1024);
    }

    #endregion

    //Deferred decals are textures that are projected onto the scene geometry before the lighting pass using the GBuffer.
    private static void AddDeferredDecal(SceneNode rootNode)
    {
        var decalNode = new SceneNode(rootNode) { Name = "TestDecalNode" };
        var decalTfm = decalNode.SetTransform<Transform>();
        decalTfm.Translation = new Vector3(0.0f, 5.0f, 0.0f);
        decalTfm.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, XRMath.DegToRad(70.0f));
        decalTfm.Scale = new Vector3(7.0f);
        var decalComp = decalNode.AddComponent<DeferredDecalComponent>()!;
        decalComp.Name = "TestDecal";
        decalComp.SetTexture(Engine.Assets.LoadEngineAsset<XRTexture2D>("Textures", "decal guide.png"));
    }

    //private static void AddPBRTestOrbs(SceneNode rootNode, float y)
    //{
    //    for (int metallic = 0; metallic < 10; metallic++)
    //        for (int roughness = 0; roughness < 10; roughness++)
    //            AddPBRTestOrb(rootNode, metallic / 10.0f, roughness / 10.0f, 0.5f, 0.5f, 10, 10, y);
    //}

    //private static void AddPBRTestOrb(SceneNode rootNode, float metallic, float roughness, float radius, float padding, int metallicCount, int roughnessCount, float y)
    //{
    //    var orb1 = new SceneNode(rootNode) { Name = "TestOrb1" };
    //    var orb1Transform = orb1.SetTransform<Transform>();

    //    //arrange in grid using metallic and roughness
    //    orb1Transform.Translation = new Vector3(
    //        (metallic * 2.0f - 1.0f) * (radius + padding) * metallicCount,
    //        y + padding + radius,
    //        (roughness * 2.0f - 1.0f) * (radius + padding) * roughnessCount);

    //    var orb1Model = orb1.AddComponent<ModelComponent>()!;
    //    var mat = XRMaterial.CreateLitColorMaterial(ColorF4.Red);
    //    mat.RenderPass = (int)EDefaultRenderPass.OpaqueDeferredLit;
    //    mat.Parameter<ShaderFloat>("Roughness")!.Value = roughness;
    //    mat.Parameter<ShaderFloat>("Metallic")!.Value = metallic;
    //    orb1Model.Model = new Model([new SubMesh(XRMesh.Shapes.SolidSphere(Vector3.Zero, radius, 32), mat)]);
    //}

    #endregion

    //User interface overlay code.
    #region UI

    private static readonly Queue<float> _fpsAvg = new();
    private static void TickFPS(UITextComponent t)
    {
        _fpsAvg.Enqueue(1.0f / Engine.Time.Timer.Render.SmoothedDelta);
        if (_fpsAvg.Count > 60)
            _fpsAvg.Dequeue();
        t.Text = $"{MathF.Round(_fpsAvg.Sum() / _fpsAvg.Count)}hz";
    }

    //Simple FPS counter in the bottom right for debugging.
    private static UITextComponent AddFPSText(FontGlyphSet? font, SceneNode parentNode)
    {
        SceneNode textNode = new(parentNode) { Name = "TestTextNode" };
        UITextComponent text = textNode.AddComponent<UITextComponent>()!;
        text.Font = font;
        text.FontSize = 22;
        text.RegisterAnimationTick<UITextComponent>(TickFPS);
        var textTransform = textNode.GetTransformAs<UIBoundableTransform>(true)!;
        textTransform.MinAnchor = new Vector2(1.0f, 0.0f);
        textTransform.MaxAnchor = new Vector2(1.0f, 0.0f);
        textTransform.NormalizedPivot = new Vector2(1.0f, 0.0f);
        textTransform.Width = null;
        textTransform.Height = null;
        textTransform.Margins = new Vector4(10.0f, 10.0f, 10.0f, 10.0f);
        textTransform.Scale = new Vector3(1.0f);
        return text;
    }

    //The full editor UI - includes a toolbar, inspector, viewport and scene hierarchy.
    private static void CreateEditorUI(SceneNode parent, CameraComponent camComp, PawnComponent? pawnForInput = null)
    {
        var rootCanvasNode = new SceneNode(parent) { Name = "TestUINode" };
        var canvas = rootCanvasNode.AddComponent<UICanvasComponent>()!;
        var canvasTfm = rootCanvasNode.GetTransformAs<UICanvasTransform>(true)!;
        canvasTfm.DrawSpace = ECanvasDrawSpace.Screen;
        canvasTfm.Width = 1920.0f;
        canvasTfm.Height = 1080.0f;
        canvasTfm.CameraDrawSpaceDistance = 10.0f;
        canvasTfm.Padding = new Vector4(0.0f);

        if (VisualizeQuadtree)
            rootCanvasNode.AddComponent<DebugVisualizeQuadtreeComponent>();

        if (camComp is not null)
            camComp.UserInterface = canvas;

        AddFPSText(null, rootCanvasNode);

        if (AddEditorUI)
        {
            //Add input handler
            var input = rootCanvasNode.AddComponent<UIInputComponent>()!;
            input.OwningPawn = pawnForInput;

            //This will take care of editor UI arrangement operations for us
            var mainUINode = rootCanvasNode.NewChild<UIEditorComponent>(out var editorComp);
            editorComp.MenuOptions = GenerateRootMenu();
            if (editorComp.UITransform is not UIBoundableTransform tfm)
                return;
            tfm.MinAnchor = new Vector2(0.0f, 0.0f);
            tfm.MaxAnchor = new Vector2(1.0f, 1.0f);
            tfm.NormalizedPivot = new Vector2(0.0f, 0.0f);
            tfm.Translation = new Vector2(0.0f, 0.0f);
            tfm.Width = null;
            tfm.Height = null;
        }
    }

    //Signals the camera to take a picture of the current view.
    public static void TakeScreenshot(UIInteractableComponent comp)
    {
        //Debug.Out("Take Screenshot clicked");

        var camera = Engine.State.GetOrCreateLocalPlayer(ELocalPlayerIndex.One).ControlledPawn as EditorFlyingCameraPawnComponent;
        camera?.TakeScreenshot();
    }
    //Loads a project from the file system.
    public static void LoadProject(UIInteractableComponent comp)
    {
        //Debug.Out("Load Project clicked");
    }
    //Saves all modified assets in the project.
    public static async void SaveAll(UIInteractableComponent comp)
    {
        await Engine.Assets.SaveAllAsync();
    }

    //Generates the root menu for the editor UI.
    //TODO: allow scripts to add menu options with attributes
    private static List<ToolbarButton> GenerateRootMenu()
    {
        return [
            new("File", [Key.ControlLeft, Key.F],
            [
                new("Save All", SaveAll),
                new("Open", [
                    new ToolbarButton("Project", LoadProject),
                    ])
            ]),
            new("Edit"),
            new("Assets"),
            new("Tools", [Key.ControlLeft, Key.T],
            [
                new("Take Screenshot", TakeScreenshot),
            ]),
            new("View"),
            new("Window"),
            new("Help"),
        ];
    }

    #endregion

    //All tests pertaining to lighting the scene.
    #region Physics Tests

    //Creates a floor and a bunch of balls that fall onto it.
    private static void AddPhysics(SceneNode rootNode, int ballCount = 100)
    {
        float ballRadius = 0.5f;

        var floor = new SceneNode(rootNode) { Name = "Floor" };
        var floorTfm = floor.SetTransform<RigidBodyTransform>();
        var floorComp = floor.AddComponent<StaticRigidBodyComponent>()!;

        PhysxMaterial floorPhysMat = new(0.5f, 0.5f, 0.7f);
        PhysxMaterial ballPhysMat = new(0.2f, 0.2f, 1.0f);

        var floorBody = PhysxStaticRigidBody.CreatePlane(Globals.Up, 0.0f, floorPhysMat);
        //new PhysxStaticRigidBody(floorMat, new PhysxGeometry.Box(new Vector3(100.0f, 2.0f, 100.0f)));
        floorBody.SetTransform(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.CreateFromAxisAngle(Globals.Backward, XRMath.DegToRad(90.0f)), true);
        //floorBody.CollisionGroup = 1;
        //floorBody.GroupsMask = new MagicPhysX.PxGroupsMask() { bits0 = 0, bits1 = 0, bits2 = 0, bits3 = 1 };
        floorComp.RigidBody = floorBody;
        //floorBody.AddedToScene += x =>
        //{
        //    var shapes = floorBody.GetShapes();
        //    var shape = shapes[0];
        //    //shape.QueryFilterData = new MagicPhysX.PxFilterData() { word0 = 0, word1 = 0, word2 = 0, word3 = 1 };
        //};

        //var floorShader = ShaderHelper.LoadEngineShader("Misc\\TestFloor.frag");
        //ShaderVar[] floorUniforms =
        //[
        //    new ShaderVector4(new ColorF4(0.9f, 0.9f, 0.9f, 1.0f), "MatColor"),
        //    new ShaderFloat(10.0f, "BlurStrength"),
        //    new ShaderInt(20, "SampleCount"),
        //    new ShaderVector3(Globals.Up, "PlaneNormal"),
        //];
        //XRTexture2D grabTex = XRTexture2D.CreateGrabPassTextureResized(0.2f);
        //XRMaterial floorMat = new(floorUniforms, [grabTex], floorShader);
        XRMaterial floorMat = XRMaterial.CreateLitColorMaterial(ColorF4.Gray);
        floorMat.RenderOptions.CullMode = ECullMode.None;
        //floorMat.RenderOptions.RequiredEngineUniforms = EUniformRequirements.Camera;
        floorMat.RenderPass = (int)EDefaultRenderPass.OpaqueDeferredLit;
        //floorMat.EnableTransparency();

        var floorModel = floor.AddComponent<ModelComponent>()!;
        floorModel.Model = new Model([new SubMesh(XRMesh.Create(VertexQuad.PosY(10000.0f)), floorMat)]);

        Random random = new();
        for (int i = 0; i < ballCount; i++)
            AddBall(rootNode, ballPhysMat, ballRadius, random);
    }

    //Spawns a ball with a random position, velocity and angular velocity.
    private static void AddBall(SceneNode rootNode, PhysxMaterial ballPhysMat, float ballRadius, Random random)
    {
        var ballBody = new PhysxDynamicRigidBody(ballPhysMat, new IAbstractPhysicsGeometry.Sphere(ballRadius), 1.0f)
        {
            Transform = (new Vector3(
                random.NextSingle() * 100.0f,
                random.NextSingle() * 100.0f,
                random.NextSingle() * 100.0f), Quaternion.Identity),
            AngularDamping = 0.2f,
            LinearDamping = 0.2f,
        };

        ballBody.SetAngularVelocity(new Vector3(
            random.NextSingle() * 100.0f,
            random.NextSingle() * 100.0f,
            random.NextSingle() * 100.0f));

        ballBody.SetLinearVelocity(new Vector3(
            random.NextSingle() * 10.0f,
            random.NextSingle() * 10.0f,
            random.NextSingle() * 10.0f));

        var ball = new SceneNode(rootNode) { Name = "Ball" };
        var ballTfm = ball.SetTransform<RigidBodyTransform>();
        ballTfm.InterpolationMode = EInterpolationMode.Interpolate;
        var ballComp = ball.AddComponent<DynamicRigidBodyComponent>()!;
        ballComp.RigidBody = ballBody;
        var ballModel = ball.AddComponent<ModelComponent>()!;

        ColorF4 color = new(
            random.NextSingle(),
            random.NextSingle(),
            random.NextSingle());

        var ballMat = XRMaterial.CreateLitColorMaterial(color);
        ballMat.RenderPass = (int)EDefaultRenderPass.OpaqueDeferredLit;
        ballMat.Parameter<ShaderFloat>("Roughness")!.Value = random.NextSingle();
        ballMat.Parameter<ShaderFloat>("Metallic")!.Value = random.NextSingle();
        ballModel.Model = new Model([new SubMesh(XRMesh.Shapes.SolidSphere(Vector3.Zero, ballRadius, 32), ballMat)]);
    }

    #endregion

    //Tests for transforming scene nodes via animations and methods.
    #region Animation Tests

    private static void AddSpline(SceneNode rootNode)
    {
        var spline = rootNode.AddComponent<Spline3DPreviewComponent>();
        PropAnimVector3 anim = new();
        Random r = new();
        float len = r.NextSingle() * 10.0f;
        int frameCount = r.Next(2, 10);
        for (int i = 0; i < frameCount; i++)
        {
            float t = i / (float)frameCount;
            Vector3 value = new(r.NextSingle() * 10.0f, r.NextSingle() * 10.0f, r.NextSingle() * 10.0f);
            Vector3 tangent = new(r.NextSingle() * 10.0f, r.NextSingle() * 10.0f, r.NextSingle() * 10.0f);
            anim.Keyframes.Add(new Vector3Keyframe(t, value, tangent, EVectorInterpType.Smooth));
        }
        anim.LengthInSeconds = len;
        spline!.Spline = anim;
    }

    #endregion

    //Tests for audio sources and listeners.
    #region Sound Tests

    private static void AddSoundNode(SceneNode rootNode)
    {
        var sound = new SceneNode(rootNode) { Name = "TestSoundNode" };
        //var soundTransform = sound.SetTransform<Transform>();
        //soundTransform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
        if (!sound.TryAddComponent<AudioSourceComponent>(out var soundComp))
            return;

        soundComp!.Name = "TestSound";
        var data = Engine.Assets.LoadEngineAsset<AudioData>("Audio", "test16bit.wav");
        data.ConvertToMono(); //Convert to mono for 3D audio - stereo will just play equally in both ears
        soundComp.RelativeToListener = false;
        soundComp.ReferenceDistance = 1.0f;
        soundComp.MaxDistance = 100.0f;
        soundComp.RolloffFactor = 1.0f;
        soundComp.Gain = 1.0f;
        soundComp.Loop = true;
        soundComp.Type = ESourceType.Static;
        soundComp.StaticBuffer = data;
        soundComp.PlayOnActivate = true;
    }

    #endregion

    //Tests for importing models and animations.
    #region Models

    private static void ImportModels(string desktopDir, SceneNode rootNode, SceneNode characterParentNode)
    {
        var importedModelsNode = new SceneNode(rootNode) { Name = "TestImportedModelsNode" };
        //importedModelsNode.GetTransformAs<Transform>()?.ApplyScale(new Vector3(0.1f));

        //var orbitTransform2 = importedModelsNode.SetTransform<OrbitTransform>();
        //orbitTransform2.Radius = 0.0f;
        //orbitTransform2.IgnoreRotation = false;
        //orbitTransform2.RegisterAnimationTick<OrbitTransform>(t => t.Angle += Engine.DilatedDelta * 0.5f);

        string fbxPathDesktop = Path.Combine(desktopDir, "misc", "jax.fbx");

        var flags = 
        PostProcessSteps.Triangulate |
        PostProcessSteps.JoinIdenticalVertices |
        PostProcessSteps.GenerateNormals |
        PostProcessSteps.CalculateTangentSpace |
        PostProcessSteps.OptimizeGraph |
        PostProcessSteps.OptimizeMeshes |
        PostProcessSteps.SortByPrimitiveType |
        PostProcessSteps.ImproveCacheLocality |
        PostProcessSteps.RemoveRedundantMaterials;

        //TODO: skinned models don't propogate world matrix changed to the skinned matrix buffers per mesh
        if (AnimatedModel)
            ModelImporter.ImportAsync(fbxPathDesktop, flags, null, MaterialFactory, characterParentNode, 1.0f, true).ContinueWith(OnFinishedAvatar);
        if (StaticModel)
        {
            string path = Path.Combine(Engine.Assets.EngineAssetsPath, "Models", "Sponza", "sponza.obj");

            //string path2 = Path.Combine(Engine.Assets.EngineAssetsPath, "Models", "main1_sponza", "NewSponza_Main_Yup_003.fbx");
            //var task2 = ModelImporter.ImportAsync(path2, flags, null, MaterialFactory, importedModelsNode, 1, false).ContinueWith(OnFinishedWorld);

            //string path = Path.Combine(Engine.Assets.EngineAssetsPath, "Models", "pkg_a_curtains", "NewSponza_Curtains_FBX_YUp.fbx");
            var task1 = ModelImporter.ImportAsync(path, flags, null, MaterialFactory, importedModelsNode, 1, false).ContinueWith(OnFinishedWorld);

            //await Task.WhenAll(task1, task2);
        }
    }

    private static void AddSkybox(SceneNode rootNode, XRTexture2D skyEquirect)
    {
        var skybox = new SceneNode(rootNode) { Name = "TestSkyboxNode" };
        //var skyboxTransform = skybox.SetTransform<Transform>();
        //skyboxTransform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
        if (!skybox.TryAddComponent<ModelComponent>(out var skyboxComp))
            return;

        skyboxComp!.Name = "TestSkybox";
        skyboxComp.Model = new Model([new SubMesh(
            XRMesh.Shapes.SolidBox(new Vector3(-9000), new Vector3(9000), true, XRMesh.Shapes.ECubemapTextureUVs.None),
            new XRMaterial([skyEquirect], Engine.Assets.LoadEngineAsset<XRShader>("Shaders", "Scene3D", "Equirect.fs"))
            {
                RenderPass = (int)EDefaultRenderPass.Background,
                RenderOptions = new RenderingParameters()
                {
                    CullMode = ECullMode.Back,
                    DepthTest = new DepthTest()
                    {
                        UpdateDepth = false,
                        Enabled = ERenderParamUsage.Enabled,
                        Function = EComparison.Less,
                    },
                    LineWidth = 1.0f,
                }
            })]);
    }

    private static void OnFinishedWorld(Task<(SceneNode? rootNode, IReadOnlyCollection<XRMaterial> materials, IReadOnlyCollection<XRMesh> meshes)> task)
    {
        (SceneNode? rootNode, IReadOnlyCollection<XRMaterial> materials, IReadOnlyCollection<XRMesh> meshes) = task.Result;
        rootNode?.GetTransformAs<Transform>()?.ApplyScale(new Vector3(0.01f));
    }
    private static void OnFinishedAvatar(Task<(SceneNode? rootNode, IReadOnlyCollection<XRMaterial> materials, IReadOnlyCollection<XRMesh> meshes)> x)
    {
        (SceneNode? rootNode, IReadOnlyCollection<XRMaterial> materials, IReadOnlyCollection<XRMesh> meshes) = x.Result;
        OnFinishedImportingAvatar(rootNode, materials, meshes);
    }
    static void OnFinishedImportingAvatar(SceneNode? rootNode, IReadOnlyCollection<XRMaterial> materials, IReadOnlyCollection<XRMesh> meshes)
    {
        if (rootNode is null)
            return;

        //rootNode.GetTransformAs<Transform>()?.ApplyTranslation(new Vector3(5.0f, 0.0f, 0.0f));

        var comp = rootNode.AddComponent<HumanoidComponent>()!;
        if (VRPawn)
        {
            var calib = rootNode.AddComponent<VRCharacterCalibrationComponent>()!;
            calib.BeginCalibration();
        }
        //comp.IsActive = false;

        if (TestAnimation)
        {
            var knee = comp!.Right.Knee?.Node?.Transform;
            var leg = comp!.Right.Leg?.Node?.Transform;

            leg?.RegisterAnimationTick<Transform>(t => t.Rotation = Quaternion.CreateFromAxisAngle(Globals.Right, XRMath.DegToRad(180 - 90.0f * (MathF.Cos(Engine.ElapsedTime) * 0.5f + 0.5f))));
            knee?.RegisterAnimationTick<Transform>(t => t.Rotation = Quaternion.CreateFromAxisAngle(Globals.Right, XRMath.DegToRad(90.0f * (MathF.Cos(Engine.ElapsedTime) * 0.5f + 0.5f))));

            //var rootTfm = rootNode.FirstChild.GetTransformAs<Transform>(true)!;
            ////rotate the root node in a circle, but still facing forward
            //rootTfm.RegisterAnimationTick<Transform>(t =>
            //{
            //    t.Translation = new Vector3(0, MathF.Sin(Engine.ElapsedTime), 0);
            //});

            //For testing blendshape morphing
            //Technically we should only register animation tick on scene node if any lods have blendshapes,
            //but at this point the model's meshes are still loading. So we'll just be lazy and check in the animation tick since it's just for testing.
            rootNode.IterateComponents<ModelComponent>(comp =>
            {
                comp.SceneNode.RegisterAnimationTick<SceneNode>(t =>
                {
                    var meshes = comp.Meshes.SelectMany(x => x.LODs).Select(x => x.Renderer.Mesh).Where(x => x?.HasBlendshapes ?? false);
                    foreach (var xrMesh in meshes)
                    {
                        //for (int r = 0; r < xrMesh!.BlendshapeCount; r++)
                        int r = 0;
                        xrMesh?.BlendshapeWeights?.Set((uint)r, MathF.Sin(Engine.ElapsedTime) * 0.5f + 0.5f);
                    }
                });
            }, true);
        }

        if (PhysicsChain)
        {
            //Add physics chain to the breast bone
            var chest = comp!.Chest?.Node?.Transform;
            //Find breast bone
            if (chest is not null)
            {
                var breast = chest.FindChild(x =>
                    (x.Name?.Contains("breast", StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (x.Name?.Contains("boob", StringComparison.InvariantCultureIgnoreCase) ?? false));
                if (breast?.SceneNode is not null)
                {
                    var phys = breast.SceneNode.AddComponent<PhysicsChainComponent>()!;
                    phys._updateMode = PhysicsChainComponent.EUpdateMode.Normal;
                    phys._updateRate = 60;
                    phys._damping = 0.01f;
                    phys._stiffness = 0.0f;
                    phys._force = new Vector3(0.0f, 0.0f, 0.0f);
                    phys._elasticity = 0.07f;
                }
            }
        }

        if (TransformTool)
        {
            //Put the transform tool on the head for testing
            var head = comp!.Head?.Node?.Transform;
            if (head is null)
                return;
            
            //we have to wait for the scene node to be activated in the instance of the world before we can attach the transform tool
            static void Edit(SceneNode x)
            {
                TransformTool3D.GetInstance(x.Transform, ETransformType.Translate);
                x.Activated -= Edit;
            }
            if (head.SceneNode is not null)
            {
                if (head.SceneNode.IsActiveInHierarchy && head.SceneNode.World is not null)
                    TransformTool3D.GetInstance(head, ETransformType.Translate);
                else
                    head.SceneNode.Activated += Edit;
            }
        }
    }
    private static readonly ConcurrentDictionary<string, XRTexture2D> _textureCache = new();

    public static XRMaterial MaterialFactory(string modelFilePath, string name, List<TextureSlot> textures, TextureFlags flags, ShadingMode mode, Dictionary<string, List<MaterialProperty>> properties)
    {
        //Random r = new();

        XRTexture[] textureList = new XRTexture[textures.Count];
        XRMaterial mat = new(textureList);
        Task.Run(() => Parallel.For(0, textures.Count, i => LoadTexture(modelFilePath, textures, textureList, i))).ContinueWith(x =>
        {
            for (int i = 0; i < textureList.Length; i++)
            {
                XRTexture? tex = textureList[i];
                if (tex is not null)
                    mat.Textures[i] = tex;
            }

            bool transp = textures.Any(x => (x.Flags & 0x2) != 0 || x.TextureType == TextureType.Opacity);
            bool normal = textures.Any(x => x.TextureType == TextureType.Normals);
            if (textureList.Length > 0)
            {
                if (transp || textureList.Any(x => x is not null && x.HasAlphaChannel))
                {
                    transp = true;
                    mat.Shaders.Add(ShaderHelper.UnlitTextureFragForward()!);
                }
                else
                {
                    mat.Shaders.Add(ShaderHelper.TextureFragDeferred()!);
                    mat.Parameters =
                    [
                        new ShaderFloat(1.0f, "Opacity"),
                        new ShaderFloat(1.0f, "Specular"),
                        new ShaderFloat(0.9f, "Roughness"),
                        new ShaderFloat(0.0f, "Metallic"),
                        new ShaderFloat(1.0f, "IndexOfRefraction"),
                    ];
                }
            }
            else
            {
                //Show the material as magenta if no textures are present
                mat.Shaders.Add(ShaderHelper.LitColorFragDeferred()!);
                mat.Parameters =
                [
                    new ShaderVector3(ColorF3.Magenta, "BaseColor"),
                    new ShaderFloat(1.0f, "Opacity"),
                    new ShaderFloat(1.0f, "Specular"),
                    new ShaderFloat(1.0f, "Roughness"),
                    new ShaderFloat(0.0f, "Metallic"),
                    new ShaderFloat(1.0f, "IndexOfRefraction"),
                ];
            }

            mat.RenderPass = transp ? (int)EDefaultRenderPass.TransparentForward : (int)EDefaultRenderPass.OpaqueDeferredLit;
            mat.Name = name;
            mat.RenderOptions = new RenderingParameters()
            {
                CullMode = ECullMode.Back,
                DepthTest = new DepthTest()
                {
                    UpdateDepth = true,
                    Enabled = ERenderParamUsage.Enabled,
                    Function = EComparison.Less,
                },
                LineWidth = 5.0f,
                BlendModeAllDrawBuffers = transp ? BlendMode.EnabledTransparent() : BlendMode.Disabled(),
            };
        });

        return mat;
    }

    private static void LoadTexture(string modelFilePath, List<TextureSlot> textures, XRTexture[] textureList, int i)
    {
        string path = textures[i].FilePath;
        if (string.IsNullOrWhiteSpace(path))
            return;

        path = path.Replace("/", "\\");
        bool rooted = Path.IsPathRooted(path);
        if (!rooted)
        {
            string? dir = Path.GetDirectoryName(modelFilePath);
            if (dir is not null)
                path = Path.Combine(dir, path);
        }

        XRTexture2D TextureFactory(string x)
        {
            var tex = Engine.Assets.Load<XRTexture2D>(path);
            if (tex is null)
            {
                //Debug.Out($"Failed to load texture: {path}");
                tex = new XRTexture2D()
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    MagFilter = ETexMagFilter.Linear,
                    MinFilter = ETexMinFilter.Linear,
                    UWrap = ETexWrapMode.Repeat,
                    VWrap = ETexWrapMode.Repeat,
                    AlphaAsTransparency = true,
                    AutoGenerateMipmaps = true,
                    Resizable = true,
                };
            }
            else
            {
                //Debug.Out($"Loaded texture: {path}");
                tex.MagFilter = ETexMagFilter.Linear;
                tex.MinFilter = ETexMinFilter.Linear;
                tex.UWrap = ETexWrapMode.Repeat;
                tex.VWrap = ETexWrapMode.Repeat;
                tex.AlphaAsTransparency = true;
                tex.AutoGenerateMipmaps = true;
                tex.Resizable = false;
                tex.SizedInternalFormat = ESizedInternalFormat.Rgba8;
            }
            return tex;
        }

        textureList[i] = _textureCache.GetOrAdd(path, TextureFactory);
    }

    #endregion
}