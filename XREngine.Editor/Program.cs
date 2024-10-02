using Silk.NET.Assimp;
using System.Numerics;
using XREngine;
using XREngine.Components;
using XREngine.Components.Scene.Mesh;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Editor;
using XREngine.Native;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;
using XREngine.Scene.Transforms;

internal class Program
{
    static EditorRenderInfo2D RenderInfo2DConstructor(IRenderable owner, RenderCommand[] commands)
        => new(owner, commands);

    static EditorRenderInfo3D RenderInfo3DConstructor(IRenderable owner, RenderCommand[] commands)
        => new(owner, commands);

    private static void Main(string[] args)
    {
        RenderInfo2D.ConstructorOverride = RenderInfo2DConstructor;
        RenderInfo3D.ConstructorOverride = RenderInfo3DConstructor;

        Engine.Initialize(GetEngineSettings(CreateTestWorld()), GetGameState());
        Engine.Run();
        Engine.ShutDown();
    }

    static void TickRotation(OrbitTransform t) 
        => t.Angle += Engine.DilatedDelta * 0.5f;

    static XRWorld CreateTestWorld()
    {
        string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        var world = new XRWorld() { Name = "TestWorld" };
        var scene = new XRScene() { Name = "TestScene" };
        var rootNode = new SceneNode(scene) { Name = "TestRootNode" };

        //Create a test cube
        var modelNode = new SceneNode(rootNode) { Name = "TestModelNode" };
        var modelTransform = modelNode.SetTransform<OrbitTransform>();
        modelTransform.Radius = 0.0f;
        modelTransform.RegisterAnimationTick<OrbitTransform>(TickRotation);
        if (modelNode.TryAddComponent<ModelComponent>(out var modelComp))
        {
            modelComp!.Name = "TestModel";
            var mat = XRMaterial.CreateUnlitColorMaterialForward(new ColorF4(1.0f, 0.0f, 0.0f, 1.0f));
            mat.RenderPass = (int)EDefaultRenderPass.OpaqueForward;
            mat.RenderOptions = new RenderingParameters()
            {
                CullMode = ECulling.None,
                DepthTest = new DepthTest()
                {
                    UpdateDepth = true,
                    Enabled = ERenderParamUsage.Enabled,
                    Function = EComparison.Less,
                },
                AlphaTest = new AlphaTest()
                {
                    Enabled = ERenderParamUsage.Disabled,
                },
                LineWidth = 5.0f,
            };
            var mesh = XRMesh.Shapes.SolidBox(-Vector3.One, Vector3.One);
            //Engine.Assets.SaveTo(mesh, desktopDir);
            modelComp!.Model = new Model([new SubMesh(mesh, mat)]);
        }

        //Create the camera
        var cameraNode = new SceneNode(rootNode) { Name = "TestCameraNode" };
        var cameraTransform = cameraNode.SetTransform<Transform>();
        cameraTransform.Translation = new Vector3(0.0f, 0.0f, 20.0f);
        cameraTransform.LookAt(Vector3.Zero);
        if (cameraNode.TryAddComponent<CameraComponent>(out var cameraComp))
        {
            cameraComp!.Name = "TestCamera";
            cameraComp.LocalPlayerIndex = ELocalPlayerIndex.One;
            cameraComp.Camera.Parameters = new XRPerspectiveCameraParameters(60.0f, null, 0.1f, 100000.0f);
            cameraComp.CullWithFrustum = true;
            cameraComp.RenderPipeline = new TestRenderPipeline();
        }

        //var dirLight = new SceneNode(rootNode) { Name = "TestDirectionalLightNode" };
        //var dirLightTransform = dirLight.SetTransform<Transform>();
        //dirLightTransform.Translation = new Vector3(20.0f, 10.0f, 20.0f);
        //dirLightTransform.LookAt(Vector3.Zero);
        //if (dirLight.TryAddComponent<DirectionalLightComponent>(out var dirLightComp))
        //{
        //    dirLightComp!.Name = "TestDirectionalLight";
        //    dirLightComp.Color = new Vector3(1.0f, 1.0f, 1.0f);
        //    dirLightComp.Intensity = 1.0f;
        //}

        //Pawn
        //cameraNode.TryAddComponent<PawnComponent>(out var pawnComp);
        //pawnComp!.Name = "TestPawn";
        //pawnComp!.CurrentCameraComponent = cameraComp;

        world.Scenes.Add(scene);

        Task.Run(() =>
        {
            var importedModelNode = ModelImporter.Import(Path.Combine(Engine.Assets.EngineAssetsPath, "Models", "Sponza", "sponza.obj"), PostProcessSteps.None, out _, out _, true, null);
            if (importedModelNode != null)
            {
                lock (modelNode.Transform.Children)
                {
                    modelNode.Transform.Children.Add(importedModelNode.Transform);
                }
            }
        });

        return world;
    }

    static GameState GetGameState()
    {
        return new GameState()
        {

        };
    }

    static GameStartupSettings GetEngineSettings(XRWorld? targetWorld = null)
    {
        int w = 1920;
        int h = 1080;
        float fps = 60.0f;

        int primaryX = NativeMethods.GetSystemMetrics(0);
        int primaryY = NativeMethods.GetSystemMetrics(1);

        //TODO: read from init file if it exists
        return new GameStartupSettings()
        {
            StartupWindows =
            [
                new()
                {
                    WindowTitle = "XREngine Editor",
                    TargetWorld = targetWorld ?? new XRWorld(),
                    WindowState = EWindowState.Windowed,
                    X = primaryX / 2 - w / 2,
                    Y = primaryY / 2 - h / 2,
                    Width = w,
                    Height = h,
                }
            ],
            OutputVerbosity = EOutputVerbosity.Verbose,
            UseIntegerWeightingIds = true,
            DefaultUserSettings = new UserSettings()
            {
                TargetFramesPerSecond = fps,
                TargetUpdatesPerSecond = fps,
                VSync = EVSyncMode.Off,
            }
        };
    }
}