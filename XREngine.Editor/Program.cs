using Extensions;
using Silk.NET.Assimp;
using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using XREngine;
using XREngine.Components;
using XREngine.Components.Lights;
using XREngine.Components.Scene;
using XREngine.Components.Scene.Mesh;
using XREngine.Data;
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
using static XREngine.Audio.AudioSource;

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

        ////Create a test cube
        //var modelNode = new SceneNode(rootNode) { Name = "TestModelNode" };
        //if (modelNode.TryAddComponent<ModelComponent>(out var modelComp))
        //{
        //    modelComp!.Name = "TestModel";
        //    var mat = XRMaterial.CreateUnlitColorMaterialForward(new ColorF4(1.0f, 0.0f, 0.0f, 1.0f));
        //    mat.RenderPass = (int)EDefaultRenderPass.OpaqueForward;
        //    mat.RenderOptions = new RenderingParameters()
        //    {
        //        CullMode = ECulling.None,
        //        DepthTest = new DepthTest()
        //        {
        //            UpdateDepth = true,
        //            Enabled = ERenderParamUsage.Enabled,
        //            Function = EComparison.Less,
        //        },
        //        AlphaTest = new AlphaTest()
        //        {
        //            Enabled = ERenderParamUsage.Disabled,
        //        },
        //        LineWidth = 5.0f,
        //    };
        //    var mesh = XRMesh.Shapes.WireframeBox(-Vector3.One, Vector3.One);
        //    //Engine.Assets.SaveTo(mesh, desktopDir);
        //    modelComp!.Model = new Model([new SubMesh(mesh, mat)]);
        //}

        //Create the camera
        var cameraNode = new SceneNode(rootNode) { Name = "TestCameraNode" };
        var orbitTransform = cameraNode.SetTransform<OrbitTransform>();
        orbitTransform.Radius = 10.0f;
        orbitTransform.IgnoreRotation = false;
        orbitTransform.RegisterAnimationTick<OrbitTransform>(TickRotation);
        if (cameraNode.TryAddComponent<CameraComponent>(out var cameraComp))
        {
            cameraComp!.Name = "TestCamera";
            cameraComp.LocalPlayerIndex = ELocalPlayerIndex.One;
            cameraComp.CullWithFrustum = true;

            cameraComp.Camera.Parameters = new XRPerspectiveCameraParameters(60.0f, null, 0.1f, 100000.0f);
            cameraComp.Camera.RenderPipeline = new DefaultRenderPipeline();
        }

        var dirLight = new SceneNode(rootNode) { Name = "TestDirectionalLightNode" };
        var dirLightTransform = dirLight.SetTransform<Transform>();
        dirLightTransform.Translation = new Vector3(20.0f, 10.0f, 20.0f);
        dirLightTransform.Rotation = Quaternion.CreateFromYawPitchRoll(MathF.PI / 4.0f, MathF.PI / 4.0f, 0.0f);
        if (dirLight.TryAddComponent<DirectionalLightComponent>(out var dirLightComp))
        {
            dirLightComp!.Name = "TestDirectionalLight";
            dirLightComp.Color = new Vector3(1.0f, 1.0f, 1.0f);
            dirLightComp.Intensity = 1.0f;
            dirLightComp.Scale = new Vector3(100.0f, 100.0f, 100.0f);
        }

        //var listener = new SceneNode(cameraNode) { Name = "TestListenerNode" };
        //var listenerTransform = listener.SetTransform<Transform>();
        //listenerTransform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
        //if (listener.TryAddComponent<AudioListenerComponent>(out var listenerComp))
        //    listenerComp!.Name = "TestListener";

        //var sound = new SceneNode(rootNode) { Name = "TestSoundNode" };
        //var soundTransform = sound.SetTransform<Transform>();
        //soundTransform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
        //if (sound.TryAddComponent<AudioSourceComponent>(out var soundComp))
        //{
        //    soundComp!.Name = "TestSound";
        //    var data = Engine.Assets.LoadEngineAsset<AudioData>("Audio", "test16bit.wav");
        //    data.ConvertToMono(); //Convert to mono for 3D audio - stereo will just play equally in both ears
        //    soundComp.RelativeToListener = false;
        //    soundComp.ReferenceDistance = 1.0f;
        //    soundComp.MaxDistance = 10.0f;
        //    soundComp.RolloffFactor = 1.0f;
        //    soundComp.Gain = 0.3f;
        //    soundComp.Loop = true;
        //    soundComp.Type = ESourceType.Static;
        //    soundComp.StaticBuffer = data;
        //    soundComp.PlayOnActivate = true;
        //}

        var probe = new SceneNode(rootNode) { Name = "TestLightProbeNode" };
        var probeTransform = probe.SetTransform<Transform>();
        probeTransform.Translation = new Vector3(0.0f, 15.0f, 0.0f);
        if (probe.TryAddComponent<LightProbeComponent>(out var probeComp))
        {
            probeComp!.Name = "TestLightProbe";
            probeComp.SetCaptureResolution(256, false, 256);
        }

        //var skybox = new SceneNode(rootNode) { Name = "TestSkyboxNode" };
        //var skyboxTransform = skybox.SetTransform<Transform>();
        //skyboxTransform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
        //if (skybox.TryAddComponent<ModelComponent>(out var skyboxComp))
        //{
        //    skyboxComp!.Name = "TestSkybox";
        //    skyboxComp.Model = new Model([new SubMesh(
        //        XRMesh.Shapes.SolidBox(new Vector3(-1000.0f), new Vector3(1000.0f), true, XRMesh.Shapes.ECubemapTextureUVs.None), 
        //        new XRMaterial(
        //            [Engine.Assets.LoadEngineAsset<XRTexture2D>("Textures", "overcast_soil_puresky_4k.hdr")],
        //            Engine.Assets.LoadEngineAsset<XRShader>("Shaders", "Scene3D", "Skybox.fs"))
        //        {
        //            RenderPass = (int)EDefaultRenderPass.Background,
        //            RenderOptions = new RenderingParameters()
        //            {
        //                CullMode = ECulling.None,
        //                DepthTest = new DepthTest()
        //                {
        //                    UpdateDepth = true,
        //                    Enabled = ERenderParamUsage.Enabled,
        //                    Function = EComparison.Less,
        //                },
        //                AlphaTest = new AlphaTest()
        //                {
        //                    Enabled = ERenderParamUsage.Disabled,
        //                },
        //                LineWidth = 5.0f,
        //            }
        //        })]);
        //}

        //Pawn
        //cameraNode.TryAddComponent<PawnComponent>(out var pawnComp);
        //pawnComp!.Name = "TestPawn";
        //pawnComp!.CurrentCameraComponent = cameraComp;

        var importedModelsNode = new SceneNode(rootNode) { Name = "TestImportedModelsNode" };
        importedModelsNode.GetTransformAs<Transform>()?.ApplyScale(new Vector3(0.1f));

        world.Scenes.Add(scene);

        //string fbxPathDesktop = Path.Combine(desktopDir, "test.fbx");
        //_ = ModelImporter.ImportAsync(fbxPathDesktop, PostProcessSteps.None, MaterialFactory, rootNode);

        string sponzaPath = Path.Combine(Engine.Assets.EngineAssetsPath, "Models", "Sponza", "sponza.obj");
        _ = ModelImporter.ImportAsync(sponzaPath, PostProcessSteps.None, MaterialFactory, importedModelsNode);

        return world;
    }

    private static readonly ConcurrentDictionary<string, XRTexture2D> _textureCache = new();

    public static XRMaterial MaterialFactory(string modelFilePath, string name, List<TextureInfo> textures, TextureFlags flags, ShadingMode mode, Dictionary<string, List<object>> properties)
    {
        XRMaterial mat = textures.Count > 0 ?
            new XRMaterial(new XRTexture?[textures.Count], ShaderHelper.TextureFragDeferred()!) :
            XRMaterial.CreateLitColorMaterial(new ColorF4(1.0f, 1.0f, 0.0f, 1.0f));
        mat.RenderPass = (int)EDefaultRenderPass.OpaqueDeferredLit;
        mat.Name = name;
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
                Enabled = ERenderParamUsage.Enabled,
                Comp = EComparison.Greater,
                Ref = 0.5f,
                Comp1 = EComparison.Greater,
                Ref1 = 0.5f,
                LogicGate = ELogicGate.Or,
                UseAlphaToCoverage = false,
                UseConstantAlpha = false
            },
            LineWidth = 5.0f,
        };

        Task.Run(() => Parallel.For(0, textures.Count, i => LoadTexture(modelFilePath, textures, mat, i)));

        //for (int i = 0; i < mat.Textures.Count; i++)
        //    LoadTexture(modelFilePath, textures, mat, i);

        return mat;
    }

    private static void LoadTexture(string modelFilePath, List<TextureInfo> textures, XRMaterial mat, int i)
    {
        string path = textures[i].path;
        if (path is null)
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

        mat.Textures[i] = _textureCache.GetOrAdd(path, TextureFactory);
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