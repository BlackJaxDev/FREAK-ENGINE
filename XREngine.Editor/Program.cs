using System.Numerics;
using XREngine;
using XREngine.Components;
using XREngine.Components.Scene.Mesh;
using XREngine.Editor;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models;
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

        XRWorld CreateTestWorld()
        {
            var world = new XRWorld() { Name = "TestWorld" };
            var scene = new XRScene() { Name = "TestScene" };
            var rootNode = new SceneNode(scene) { Name = "TestRootNode" };

            //Create a test cube
            var modelNode = new SceneNode(rootNode) { Name = "TestModelNode" };
            var modelTransform = modelNode.SetTransform<OrbitTransform>();
            modelTransform.Radius = 0.0f;
            modelTransform.RegisterAnimationTick<OrbitTransform>(t => t.Angle += Engine.Delta * 10.0f);
            if (rootNode.TryAddComponent<ModelComponent>(out var modelComp))
            {
                modelComp!.Name = "TestModel";
                modelComp!.Model = new Model([new SubMesh(XRMesh.Shapes.SolidBox(-Vector3.One, Vector3.One, false, XRMesh.Shapes.ECubemapTextureUVs.WidthLarger), XRMaterial.CreateColorMaterialDeferred())]);
            }

            //Create the camera
            var cameraNode = new SceneNode(rootNode) { Name = "TestCameraNode" };
            var cameraTransform = cameraNode.SetTransform<Transform>();
            cameraTransform.Translation = new Vector3(2.0f, 10.0f, 10.0f);
            cameraTransform.LookAt(Vector3.Zero);
            if (cameraNode.TryAddComponent<CameraComponent>(out var cameraComp))
            {
                cameraComp!.Name = "TestCamera";
                cameraComp.LocalPlayerIndex = ELocalPlayerIndex.One;
            }

            //Pawn
            cameraNode.TryAddComponent<PawnComponent>(out var pawnComp);
            pawnComp!.Name = "TestPawn";
            pawnComp!.CurrentCameraComponent = cameraComp;

            world.Scenes.Add(scene);
            return world;
        }

        GameState GetGameState()
        {
            return new GameState()
            {

            };
        }

        GameStartupSettings GetEngineSettings(XRWorld? targetWorld = null)
        {
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
                    Width = 1920,
                    Height = 1080,
                }
                ],
                OutputVerbosity = EOutputVerbosity.Normal,
                UseIntegerWeightingIds = true,
                DefaultUserSettings = new UserSettings()
                {
                    TargetFramesPerSecond = 90.0f,
                    TargetUpdatesPerSecond = 90.0f,
                    VSync = EVSyncMode.Off,
                }
            };
        }
    }
}