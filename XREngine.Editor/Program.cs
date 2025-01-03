﻿using Assimp;
using OpenVR.NET.Manifest;
using Silk.NET.Input;
using System.Collections.Concurrent;
using System.Numerics;
using XREngine;
using XREngine.Animation;
using XREngine.Components;
using XREngine.Components.Lights;
using XREngine.Components.Scene;
using XREngine.Components.Scene.Mesh;
using XREngine.Data;
using XREngine.Data.Colors;
using XREngine.Data.Components;
using XREngine.Data.Components.Scene;
using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Editor;
using XREngine.Editor.UI.Components;
using XREngine.Editor.UI.Toolbar;
using XREngine.Native;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Physics.Physx;
using XREngine.Rendering.UI;
using XREngine.Scene;
using XREngine.Scene.Components.Animation;
using XREngine.Scene.Components.Physics;
using XREngine.Scene.Transforms;
using XREngine.VRClient;
using static XREngine.Audio.AudioSource;
using static XREngine.Scene.Transforms.RigidBodyTransform;
using ActionType = OpenVR.NET.Manifest.ActionType;
using BlendMode = XREngine.Rendering.Models.Materials.BlendMode;
using Quaternion = System.Numerics.Quaternion;

internal class Program
{
    public const bool VisualizeOctree = false;
    public const bool VisualizeQuadtree = false;
    public const bool Physics = true;
    public const bool DirLight = true;
    public const bool SpotLight = false;
    public const bool DirLight2 = true;
    public const bool PointLight = false;
    public const bool SoundNode = false;
    public const bool LightProbe = true;
    public const bool Skybox = true;
    public const bool Spline = false;
    public const bool StaticModel = true;
    public const bool AnimatedModel = false;

    /// <summary>
    /// This project serves as a hardcoded game client for development purposes.
    /// This editor will autogenerate the client exe csproj to compile production games.
    /// </summary>
    /// <param name="args"></param>
    private static void Main(string[] args)
    {
        RenderInfo2D.ConstructorOverride = RenderInfo2DConstructor;
        RenderInfo3D.ConstructorOverride = RenderInfo3DConstructor;

        var startup =/* Engine.LoadOrGenerateGameSettings(() => */GetEngineSettings(new XRWorld());//);
        var world = CreateTestWorld();
        startup.StartupWindows[0].TargetWorld = world;

        Engine.Run(startup, Engine.LoadOrGenerateGameState());
    }

    static EditorRenderInfo2D RenderInfo2DConstructor(IRenderable owner, RenderCommand[] commands)
        => new(owner, commands);
    static EditorRenderInfo3D RenderInfo3DConstructor(IRenderable owner, RenderCommand[] commands)
        => new(owner, commands);

    static GameState GetGameState()
    {
        return new GameState()
        {

        };
    }

    static GameStartupSettings GetEngineSettings(XRWorld targetWorld)
    {
        int w = 1920;
        int h = 1080;
        float updateHz = 60.0f;
        float renderHz = 0.0f;
        float fixedHz = 30.0f;

        int primaryX = NativeMethods.GetSystemMetrics(0);
        int primaryY = NativeMethods.GetSystemMetrics(1);

        return new VRGameStartupSettings<EVRActionCategory, EVRGameAction>()
        {
            StartupWindows =
            [
                new()
                {
                    WindowTitle = "XRE Editor",
                    TargetWorld = targetWorld,
                    WindowState = EWindowState.Windowed,
                    X = primaryX / 2 - w / 2,
                    Y = primaryY / 2 - h / 2,
                    Width = w,
                    Height = h,
                }
            ],
            OutputVerbosity = EOutputVerbosity.Verbose,
            DefaultUserSettings = new UserSettings()
            {
                TargetFramesPerSecond = renderHz,
                VSync = EVSyncMode.Off,
            },
            TargetUpdatesPerSecond = updateHz,
            FixedFramesPerSecond = fixedHz,
            //ActionManifest = new ActionManifest<EVRActionCategory, EVRGameAction>()
            //{
            //    Actions = GetActions(),
            //},
            //VRManifest = new VrManifest()
            //{
            //    AppKey = "XRE.VR.Test",
            //    IsDashboardOverlay = false,
            //    WindowsPath = Environment.ProcessPath,
            //    WindowsArguments = "",
            //},
        };
    }

    static XRWorld CreateTestWorld()
    {
        string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        //UnityPackageExtractor.ExtractAsync(Path.Combine(desktopDir, "Animations.unitypackage"), Path.Combine(desktopDir, "Extracted"), true);

        //var anim = Engine.Assets.Load<Unity.UnityAnimationClip>(Path.Combine(desktopDir, "walk.anim"));
        //if (anim is not null)
        //{
        //    var anims = UnityConverter.ConvertFloatAnimation(anim);
        //}

        var world = new XRWorld() { Name = "TestWorld" };
        var scene = new XRScene() { Name = "TestScene" };
        world.Scenes.Add(scene);
        var rootNode = new SceneNode(scene) { Name = "TestRootNode" };

        if (VisualizeOctree)
            rootNode.AddComponent<DebugVisualizeOctreeComponent>();

        SceneNode cameraNode = CreateCamera(rootNode, out var camComp);
        var pawn = CreateDesktopViewerPawn(cameraNode);
        CreateUserInterface(rootNode, camComp, pawn);
        //SceneNode cameraNode = CreateDesktopCharacterPawn(rootNode);
        //CreateVRPawn(rootNode);

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
            string[] names = ["warm_restaurant_4k", "overcast_soil_puresky_4k", "studio_small_09_4k", "klippad_sunrise_2_4k"];
            Random r = new();
            XRTexture2D skyEquirect = Engine.Assets.LoadEngineAsset<XRTexture2D>("Textures", $"{names[r.Next(0, names.Length - 1)]}.exr");

            if (LightProbe)
                AddLightProbe(rootNode, skyEquirect);
            if (Skybox)
                AddSkybox(rootNode, skyEquirect);
        }
        if (Physics)
            AddPhysics(rootNode);
        if (Spline)
            AddSpline(rootNode);
        ImportModels(desktopDir, rootNode);
        return world;
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

    private static void CreateUserInterface(SceneNode parent, CameraComponent? camComp, EditorFlyingCameraPawnComponent pawn)
    {
        var rootCanvasNode = new SceneNode(parent) { Name = "TestUINode" };
        var canvas = rootCanvasNode.AddComponent<UICanvasComponent>()!;
        var canvasTfm = rootCanvasNode.GetTransformAs<UICanvasTransform>(true)!;
        canvasTfm.DrawSpace = ECanvasDrawSpace.Screen;
        canvasTfm.Width = 1920.0f;
        canvasTfm.Height = 1080.0f;
        canvasTfm.CameraDrawSpaceDistance = 100.0f;
        canvasTfm.Padding = new Vector4(0.0f);

        if (VisualizeQuadtree)
            rootCanvasNode.AddComponent<DebugVisualizeQuadtreeComponent>();

        if (camComp is not null)
            camComp.UserInterface = canvas;

        //AddFPSText(null, rootCanvasNode);

        //Add input handler
        var input = rootCanvasNode.AddComponent<UIInputComponent>()!;
        input.OwningPawn = pawn;

        //This will take care of editor UI arrangement operations for us
        var mainUINode = rootCanvasNode.NewChild<UIEditorComponent>(out var editorComp);
        editorComp.MenuOptions = GenerateRootMenu();
        var tfm = editorComp.UITransform as UIBoundableTransform;
        if (tfm is not null)
        {
            tfm.MinAnchor = new Vector2(0.0f, 0.0f);
            tfm.MaxAnchor = new Vector2(1.0f, 1.0f);
            tfm.NormalizedPivot = new Vector2(0.0f, 0.0f);
            tfm.Translation = new Vector2(0.0f, 0.0f);
            tfm.Width = null;
            tfm.Height = null;
        }
    }

    public static void TakeScreenshot(UIInteractableComponent comp)
    {
        //Debug.Out("Take Screenshot clicked");

        var camera = Engine.State.GetOrCreateLocalPlayer(ELocalPlayerIndex.One).ControlledPawn as EditorFlyingCameraPawnComponent;
        camera?.TakeScreenshot();
    }
    public static void LoadProject(UIInteractableComponent comp)
    {
        //Debug.Out("Load Project clicked");
    }
    public static async void SaveAll(UIInteractableComponent comp)
    {
        await Engine.Assets.SaveAllAsync();
    }

    //TODO: allow scripts to add menu options with attributes
    private static List<ToolbarButton> GenerateRootMenu()
    {
        return [
            new("File", [Key.ControlLeft, Key.F],
            [
                new("Save", SaveAll),
                new("Open", [
                    new ToolbarButton("Project", LoadProject),
                    ])
            ]),
            new("Edit"),
            new("Assets"),
            new("Tools", [Key.ControlLeft, Key.T],
            [
                new("Take_Screenshot", TakeScreenshot),
            ]),
            new("View"),
            new("Window"),
            new("Help"),
        ];
    }

    private static void CreateVRPawn(SceneNode rootNode)
    {
        SceneNode vrPlayspaceNode = new(rootNode) { Name = "VRPlayspaceNode" };
        var playspaceTfm = vrPlayspaceNode.SetTransform<Transform>();
        playspaceTfm.ApplyScale(new Vector3(10.0f));

        SceneNode vrHeadsetNode = new(vrPlayspaceNode) { Name = "VRHeadsetNode" };
        var hmdTfm = vrHeadsetNode.SetTransform<VRHeadsetTransform>();
        var hmdComp = vrHeadsetNode.AddComponent<VRHeadsetComponent>()!;

        SceneNode leftControllerNode = new(vrPlayspaceNode) { Name = "VRLeftControllerNode" };
        var leftControllerTfm = leftControllerNode.SetTransform<VRControllerTransform>();
        leftControllerTfm.LeftHand = true;
        //Add debug sphere to left controller
        var leftControllerModel = leftControllerNode.AddComponent<VRControllerModelComponent>()!;
        leftControllerModel.LeftHand = true;

        SceneNode rightControllerNode = new(vrPlayspaceNode) { Name = "VRRightControllerNode" };
        var rightControllerTfm = rightControllerNode.SetTransform<VRControllerTransform>();
        rightControllerTfm.LeftHand = false;
        //Add debug sphere to right controller
        var rightControllerModel = rightControllerNode.AddComponent<VRControllerModelComponent>()!;
        rightControllerModel.LeftHand = false;
    }

    private static void AddPhysics(SceneNode rootNode)
    {
        float ballRadius = 1.0f;

        var floor = new SceneNode(rootNode) { Name = "Floor" };
        var floorTfm = floor.SetTransform<RigidBodyTransform>();
        var floorComp = floor.AddComponent<StaticRigidBodyComponent>()!;

        PhysxMaterial floorPhysMat = new(0.5f, 0.5f, 0.7f);
        PhysxMaterial ballPhysMat = new(0.2f, 0.2f, 1.0f);

        var floorBody = PhysxStaticRigidBody.CreatePlane(Globals.Up, 0.0f, floorPhysMat);
            //new PhysxStaticRigidBody(floorMat, new PhysxGeometry.Box(new Vector3(100.0f, 2.0f, 100.0f)));
        floorBody.SetTransform(new Vector3(0.0f, -10.0f, 0.0f), Quaternion.CreateFromAxisAngle(Globals.Backward, XRMath.DegToRad(90.0f)), true);
        floorComp.RigidBody = floorBody;

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
        for (int i = 0; i < 10; i++)
            AddBall(rootNode, ballPhysMat, ballRadius, random);
    }

    private static void AddBall(SceneNode rootNode, PhysxMaterial ballPhysMat, float ballRadius, Random random)
    {
        var ballBody = new PhysxDynamicRigidBody(ballPhysMat, new PhysxGeometry.Sphere(ballRadius), 1.0f)
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

    private static SceneNode CreateCamera(SceneNode parentNode, out CameraComponent? camComp, bool smoothed = true)
    {
        var cameraNode = new SceneNode(parentNode) { Name = "TestCameraNode" };

        if (smoothed)
        {
            var laggedTransform = cameraNode.GetTransformAs<SmoothedTransform>(true)!;
            laggedTransform.RotationSmoothingSpeed = 30.0f;
            laggedTransform.TranslationSmoothingSpeed = 30.0f;
            laggedTransform.ScaleSmoothingSpeed = 30.0f;
        }

        if (cameraNode.TryAddComponent<CameraComponent>(out var cameraComp))
        {
            cameraComp!.Name = "TestCamera";
            cameraComp.Camera.Parameters = new XRPerspectiveCameraParameters(60.0f, null, 0.1f, 100000.0f);
            cameraComp.CullWithFrustum = false;
            camComp = cameraComp;
        }
        else
            camComp = null;

        return cameraNode;
    }

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

    private static EditorFlyingCameraPawnComponent CreateDesktopViewerPawn(SceneNode cameraNode)
    {
        var pawnComp = cameraNode.AddComponent<EditorFlyingCameraPawnComponent>();
        cameraNode.AddComponent<AudioListenerComponent>();

        pawnComp!.Name = "TestPawn";
        pawnComp.EnqueuePossessionByLocalPlayer(ELocalPlayerIndex.One);
        return pawnComp;
    }

    private static SceneNode CreateDesktopCharacterPawn(SceneNode rootNode)
    {
        SceneNode characterNode = new(rootNode) { Name = "TestPlayerNode" };
        var characterTfm = characterNode.SetTransform<RigidBodyTransform>();
        characterTfm.InterpolationMode = EInterpolationMode.Interpolate;

        SceneNode cameraNode = CreateCamera(characterNode, out CameraComponent? camComp);
        cameraNode.AddComponent<AudioListenerComponent>();

        var characterComp = characterNode.AddComponent<CharacterComponent>();
        characterComp!.CameraComponent = camComp;
        var movementComp = characterNode.AddComponent<CharacterMovement3DComponent>();
        movementComp!.StandingHeight = 1.89f;
        movementComp!.SpawnPosition = new Vector3(0.0f, 5.0f, 0.0f);
        movementComp.Velocity = new Vector3(0.0f, 0.1f, 0.0f);
        characterComp!.Name = "TestPawn";
        characterComp.EnqueuePossessionByLocalPlayer(ELocalPlayerIndex.One);
        return cameraNode;
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
        dirLightComp.Intensity = 1.0f;
        dirLightComp.Scale = new Vector3(1000.0f, 1000.0f, 1000.0f);
        dirLightComp.CastsShadows = false;
        dirLightComp.SetShadowMapResolution(2048, 2048);
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
        dirLightComp2.Intensity = 1.0f;
        dirLightComp2.Scale = new Vector3(1000.0f, 1000.0f, 1000.0f);
        dirLightComp2.CastsShadows = true;
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
        spotLightComp.Intensity = 10.0f;
        spotLightComp.Brightness = 1.0f;
        spotLightComp.Distance = 40.0f;
        spotLightComp.SetCutoffs(10, 40);
        spotLightComp.CastsShadows = true;
        spotLightComp.SetShadowMapResolution(256, 256);
    }

    private static void AddPointLight(SceneNode rootNode)
    {
        var pointLight = new SceneNode(rootNode) { Name = "TestPointLightNode" };
        var pointLightTransform = pointLight.SetTransform<Transform>();
        pointLightTransform.Translation = new Vector3(100.0f, 1.0f, 0.0f);
        if (!pointLight.TryAddComponent<PointLightComponent>(out var pointLightComp))
            return;
        
        pointLightComp!.Name = "TestPointLight";
        pointLightComp.Color = new Vector3(1.0f, 1.0f, 1.0f);
        pointLightComp.Intensity = 10.0f;
        pointLightComp.Brightness = 1.0f;
        pointLightComp.Radius = 1000.0f;
        pointLightComp.CastsShadows = true;
        pointLightComp.SetShadowMapResolution(256, 256);
    }

    private static void AddSoundNode(SceneNode rootNode)
    {
        var sound = new SceneNode(rootNode) { Name = "TestSoundNode" };
        var soundTransform = sound.SetTransform<Transform>();
        soundTransform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
        if (!sound.TryAddComponent<AudioSourceComponent>(out var soundComp))
            return;
        
        soundComp!.Name = "TestSound";
        var data = Engine.Assets.LoadEngineAsset<AudioData>("Audio", "test16bit.wav");
        data.ConvertToMono(); //Convert to mono for 3D audio - stereo will just play equally in both ears
        soundComp.RelativeToListener = false;
        soundComp.ReferenceDistance = 1.0f;
        soundComp.MaxDistance = 10.0f;
        soundComp.RolloffFactor = 1.0f;
        soundComp.Gain = 0.3f;
        soundComp.Loop = true;
        soundComp.Type = ESourceType.Static;
        soundComp.StaticBuffer = data;
        soundComp.PlayOnActivate = true;
    }

    private static void AddLightProbe(SceneNode rootNode, XRTexture2D skyEquirect)
    {
        var probe = new SceneNode(rootNode) { Name = "TestLightProbeNode" };
        var probeTransform = probe.SetTransform<Transform>();
        probeTransform.Translation = new Vector3(0.0f, 10.0f, 0.0f);
        if (!probe.TryAddComponent<LightProbeComponent>(out var probeComp))
            return;
        
        probeComp!.Name = "TestLightProbe";
        //probeComp.ColorResolution = 512;
        //probeComp.EnvironmentTextureEquirect = skyEquirect;
        //Engine.EnqueueMainThreadTask(probeComp.GenerateIrradianceMap);
        //Engine.EnqueueMainThreadTask(probeComp.GeneratePrefilterMap);

        probeComp.SetCaptureResolution(256, false, 256);
        probeComp.RealTimeCapture = false;
        probeComp.RealTimeCaptureUpdateInterval = TimeSpan.FromSeconds(1);

        //Task.Run(async () =>
        //{
        //    await Task.Delay(2000);
        //    Engine.EnqueueMainThreadTask(probeComp.Capture);
        //});
    }

    private static void ImportModels(string desktopDir, SceneNode rootNode)
    {
        var importedModelsNode = new SceneNode(rootNode) { Name = "TestImportedModelsNode" };
        //importedModelsNode.GetTransformAs<Transform>()?.ApplyScale(new Vector3(0.1f));

        //var orbitTransform2 = importedModelsNode.SetTransform<OrbitTransform>();
        //orbitTransform2.Radius = 0.0f;
        //orbitTransform2.IgnoreRotation = false;
        //orbitTransform2.RegisterAnimationTick<OrbitTransform>(t => t.Angle += Engine.DilatedDelta * 0.5f);

        string fbxPathDesktop = Path.Combine(desktopDir, "misc", "test.fbx");

        var flags =
            PostProcessSteps.Triangulate |
            PostProcessSteps.JoinIdenticalVertices |
            PostProcessSteps.CalculateTangentSpace;

        if (AnimatedModel)
            ModelImporter.ImportAsync(fbxPathDesktop, flags, null, MaterialFactory, importedModelsNode, 1, true).ContinueWith(OnFinishedAvatar);
        if (StaticModel)
            ModelImporter.ImportAsync(Path.Combine(Engine.Assets.EngineAssetsPath, "Models", "Sponza", "sponza.obj"), flags, null, MaterialFactory, importedModelsNode, 1, false).ContinueWith(OnFinishedWorld);
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
            XRMesh.Shapes.SolidBox(new Vector3(-10000), new Vector3(10000), true, XRMesh.Shapes.ECubemapTextureUVs.None),
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
        //comp.IsActive = false;

        //TransformTool3D.GetInstance(comp.Transform, ETransformType.Translate);

        //var knee = comp!.Right.Knee?.Node?.Transform;
        //var leg = comp!.Right.Leg?.Node?.Transform;

        //leg?.RegisterAnimationTick<Transform>(t => t.Rotation = Quaternion.CreateFromAxisAngle(Globals.Right, XRMath.DegToRad(180 - 90.0f * (MathF.Cos(Engine.ElapsedTime) * 0.5f + 0.5f))));
        //knee?.RegisterAnimationTick<Transform>(t => t.Rotation = Quaternion.CreateFromAxisAngle(Globals.Right, XRMath.DegToRad(90.0f * (MathF.Cos(Engine.ElapsedTime) * 0.5f + 0.5f))));

        var chest = comp!.Chest?.Node?.Transform;
        //Find breast bone
        var breast = chest!.FindChild(x =>
        (x.Name?.Contains("breast", StringComparison.InvariantCultureIgnoreCase) ?? false) ||
        (x.Name?.Contains("boob", StringComparison.InvariantCultureIgnoreCase) ?? false));
        if (breast?.SceneNode is not null)
            breast.SceneNode.AddComponent<PhysicsChainComponent>();
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

            bool transp = false;
            if (textureList.Length > 0)
            {
                if (textures.Any(x => (x.Flags & 0x2) != 0 || x.TextureType == TextureType.Opacity) || textureList.Any(x => x.HasAlphaChannel))
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
                Debug.Out($"Failed to load texture: {path}");
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
                Debug.Out($"Loaded texture: {path}");
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

    private static readonly Queue<float> _fpsAvg = new();
    private static void TickFPS(UITextComponent t)
    {
        _fpsAvg.Enqueue(1.0f / Engine.Time.Timer.Render.SmoothedDelta);
        if (_fpsAvg.Count > 60)
            _fpsAvg.Dequeue();
        t.Text = $"{MathF.Round(_fpsAvg.Sum() / _fpsAvg.Count)}hz";
    }
    private static List<OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>> GetActions() =>
    [
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Interact,
            Category = EVRActionCategory.Gameplay,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Jump,
            Category = EVRActionCategory.Gameplay,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.ToggleMute,
            Category = EVRActionCategory.Gameplay,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Grab,
            Category = EVRActionCategory.Gameplay,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.PlayspaceDragLeft,
            Category = EVRActionCategory.Gameplay,
            Type = ActionType.Boolean,
            Requirement = Requirement.Optional,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.PlayspaceDragRight,
            Category = EVRActionCategory.Gameplay,
            Type = ActionType.Boolean,
            Requirement = Requirement.Optional,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.ToggleMenu,
            Category = EVRActionCategory.Menus,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.ToggleMiniMenu,
            Category = EVRActionCategory.Menus,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.LeftHandPose,
            Category = EVRActionCategory.Controllers,
            Type = ActionType.Pose,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.RightHandPose,
            Category = EVRActionCategory.Controllers,
            Type = ActionType.Pose,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.LeftHandGrip,
            Category = EVRActionCategory.Controllers,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.RightHandGrip,
            Category = EVRActionCategory.Controllers,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.LeftHandTrigger,
            Category = EVRActionCategory.Controllers,
            Type = ActionType.Scalar,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.RightHandTrigger,
            Category = EVRActionCategory.Controllers,
            Type = ActionType.Scalar,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.LeftHandTrackpad,
            Category = EVRActionCategory.Controllers,
            Type = ActionType.Vector2,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.RightHandTrackpad,
            Category = EVRActionCategory.Controllers,
            Type = ActionType.Vector2,
            Requirement = Requirement.Mandatory,
        },
    ];
}