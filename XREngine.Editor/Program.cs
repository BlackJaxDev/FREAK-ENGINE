using Assimp;
using System.Collections.Concurrent;
using System.Numerics;
using XREngine;
using XREngine.Components;
using XREngine.Components.Lights;
using XREngine.Components.Scene;
using XREngine.Components.Scene.Mesh;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Editor;
using XREngine.Native;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.UI;
using XREngine.Scene;
using XREngine.Scene.Components.Animation;
using XREngine.Scene.Transforms;
using Quaternion = System.Numerics.Quaternion;

internal class Program
{
    static EditorRenderInfo2D RenderInfo2DConstructor(IRenderable owner, RenderCommand[] commands)
        => new(owner, commands);
    static EditorRenderInfo3D RenderInfo3DConstructor(IRenderable owner, RenderCommand[] commands)
        => new(owner, commands);

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

    static XRWorld CreateTestWorld()
    {
        FontGlyphSet font = Engine.Assets.LoadEngineAsset<FontGlyphSet>("Fonts", "Calligraphy.ttf");
        string? dir = Path.GetDirectoryName(font.OriginalPath);
        Engine.Assets.SaveTo(font, dir!);

        string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        //UnityPackageExtractor.ExtractAsync(Path.Combine(desktopDir, "Animations.unitypackage"), Path.Combine(desktopDir, "Extracted"), true);

        var world = new XRWorld() { Name = "TestWorld" };
        var scene = new XRScene() { Name = "TestScene" };
        world.Scenes.Add(scene);
        var rootNode = new SceneNode(scene) { Name = "TestRootNode" };

        //Create the camera
        var cameraNode = new SceneNode(rootNode) { Name = "TestCameraNode" };

        //var orbitTransform = cameraNode.SetTransform<OrbitTransform>();
        //orbitTransform.Radius = 5.0f;
        //orbitTransform.IgnoreRotation = false;
        //orbitTransform.RegisterAnimationTick<OrbitTransform>(t => t.Angle += Engine.DilatedDelta * 0.5f);

        var laggedTransform = cameraNode.GetTransformAs<SmoothedTransform>(true)!;
        laggedTransform.Translation = new Vector3(0.0f, 0.0f, 5.0f);
        laggedTransform.RotationSmoothingSpeed = 15.0f;
        laggedTransform.TranslationSmoothingSpeed = 15.0f;
        laggedTransform.ScaleSmoothingSpeed = 15.0f;

        if (cameraNode.TryAddComponent<CameraComponent>(out var cameraComp))
        {
            cameraComp!.Name = "TestCamera";
            cameraComp.Camera.Parameters = new XRPerspectiveCameraParameters(60.0f, null, 0.1f, 99999.0f);
            cameraComp.Camera.RenderPipeline = new DefaultRenderPipeline();
            cameraComp.CullWithFrustum = false;
        }

        SceneNode textNode = new(rootNode) { Name = "TestTextNode" };
        TextComponent text = textNode.AddComponent<TextComponent>()!;
        text.Font = font;
        //text.Text = "Hello, World!";
        text.RegisterAnimationTick<TextComponent>(t => t.Text = $"{MathF.Round(1.0f / Engine.Time.Timer.Render.SmoothedDelta)} fps");
        var textTransform = textNode.GetTransformAs<Transform>()!;
        textTransform.Translation = new Vector3(0.0f, 0.0f, -5.0f);
        textTransform.Scale = new Vector3(10.0f);

        //Pawn
        cameraNode.TryAddComponent<EditorFlyingCameraPawnComponent>(out var pawnComp);
        pawnComp!.Name = "TestPawn";
        //pawnComp!.Camera = cameraComp;
        pawnComp.EnqueuePossessionByLocalPlayer(ELocalPlayerIndex.One);

        var listener = new SceneNode(cameraNode) { Name = "TestListenerNode" };
        var listenerTransform = listener.SetTransform<Transform>();
        listenerTransform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
        if (listener.TryAddComponent<AudioListenerComponent>(out var listenerComp))
            listenerComp!.Name = "TestListener";

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

        var dirLightNode = new SceneNode(rootNode) { Name = "TestDirectionalLightNode" };
        var dirLightTransform = dirLightNode.SetTransform<Transform>();
        dirLightTransform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
        //Face the light directly down
        dirLightTransform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, XRMath.DegToRad(-70.0f));
        //dirLightTransform.RegisterAnimationTick<Transform>(t => t.Rotation *= Quaternion.CreateFromAxisAngle(Globals.Backward, Engine.DilatedDelta));
        if (dirLightNode.TryAddComponent<DirectionalLightComponent>(out var dirLightComp))
        {
            dirLightComp!.Name = "TestDirectionalLight";
            dirLightComp.Color = new Vector3(0.8f, 0.8f, 0.8f);
            dirLightComp.Intensity = 0.1f;
            dirLightComp.Scale = new Vector3(1000.0f, 1000.0f, 1000.0f);
            dirLightComp.CastsShadows = true;
            dirLightComp.SetShadowMapResolution(2048, 2048);
        }

        //var spotLightNode = new SceneNode(rootNode) { Name = "TestSpotLightNode" };
        //var spotLightTransform = spotLightNode.SetTransform<Transform>();
        //spotLightTransform.Translation = new Vector3(0.0f, 10.0f, 0.0f);
        //spotLightTransform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, XRMath.DegToRad(-90.0f));
        //if (spotLightNode.TryAddComponent<SpotLightComponent>(out var spotLightComp))
        //{
        //    spotLightComp!.Name = "TestSpotLight";
        //    spotLightComp.Color = new Vector3(1.0f, 1.0f, 1.0f);
        //    spotLightComp.Intensity = 10.0f;
        //    spotLightComp.Brightness = 1.0f;
        //    spotLightComp.Distance = 40.0f;
        //    spotLightComp.SetCutoffs(10, 40);
        //    spotLightComp.CastsShadows = true;
        //    spotLightComp.SetShadowMapResolution(256, 256);
        //}

        //var dirLightNode2 = new SceneNode(rootNode) { Name = "TestDirectionalLightNode2" };
        //var dirLightTransform2 = dirLightNode2.SetTransform<Transform>();
        //dirLightTransform2.Translation = new Vector3(0.0f, 10.0f, 0.0f);
        //dirLightTransform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2.0f);
        //if (dirLightNode2.TryAddComponent<DirectionalLightComponent>(out var dirLightComp2))
        //{
        //    dirLightComp!.Name = "TestDirectionalLight2";
        //    dirLightComp.Color = new Vector3(1.0f, 0.8f, 0.8f);
        //    dirLightComp.Intensity = 1.0f;
        //    dirLightComp.Scale = new Vector3(1000.0f, 1000.0f, 1000.0f);
        //    dirLightComp.CastsShadows = true;
        //}

        //var pointLight = new SceneNode(rootNode) { Name = "TestPointLightNode" };
        //var pointLightTransform = pointLight.SetTransform<Transform>();
        //pointLightTransform.Translation = new Vector3(100.0f, 1.0f, 0.0f);
        //if (pointLight.TryAddComponent<PointLightComponent>(out var pointLightComp))
        //{
        //    pointLightComp!.Name = "TestPointLight";
        //    pointLightComp.Color = new Vector3(1.0f, 1.0f, 1.0f);
        //    pointLightComp.Intensity = 10.0f;
        //    pointLightComp.Brightness = 1.0f;
        //    pointLightComp.Radius = 1000.0f;
        //    pointLightComp.CastsShadows = true;
        //    pointLightComp.SetShadowMapResolution(256, 256);
        //}

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

        XRTexture2D skyEquirect = Engine.Assets.LoadEngineAsset<XRTexture2D>("Textures", "overcast_soil_puresky_4k.exr");

        var probe = new SceneNode(rootNode) { Name = "TestLightProbeNode" };
        var probeTransform = probe.SetTransform<Transform>();
        probeTransform.Translation = new Vector3(0.0f, 10.0f, 0.0f);
        if (probe.TryAddComponent<LightProbeComponent>(out var probeComp))
        {
            probeComp!.Name = "TestLightProbe";
            probeComp.ColorResolution = 512;
            probeComp.EnvironmentTextureEquirect = skyEquirect;
            Engine.EnqueueMainThreadTask(probeComp.GenerateIrradianceMap);
            Engine.EnqueueMainThreadTask(probeComp.GeneratePrefilterMap);

            //probeComp.SetCaptureResolution(512, false, 512);
            //probeComp.RealTimeCapture = true;
            //probeComp.RealTimeCaptureUpdateInterval = TimeSpan.FromSeconds(1);

            //Task.Run(async () =>
            //{
            //    await Task.Delay(2000);
            //    Engine.EnqueueMainThreadTask(probeComp.Capture);
            //});
        }

        var skybox = new SceneNode(rootNode) { Name = "TestSkyboxNode" };
        var skyboxTransform = skybox.SetTransform<Transform>();
        skyboxTransform.Translation = new Vector3(0.0f, 0.0f, 0.0f);
        if (skybox.TryAddComponent<ModelComponent>(out var skyboxComp))
        {
            skyboxComp!.Name = "TestSkybox";
            skyboxComp.Model = new Model([new SubMesh(
                XRMesh.Shapes.SolidBox(new Vector3(-10000), new Vector3(10000), true, XRMesh.Shapes.ECubemapTextureUVs.None),
                new XRMaterial([skyEquirect], Engine.Assets.LoadEngineAsset<XRShader>("Shaders", "Scene3D", "Equirect.fs"))
                {
                    RenderPass = (int)EDefaultRenderPass.Background,
                    RenderOptions = new RenderingParameters()
                    {
                        CullMode = ECullMode.None,
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
                        LineWidth = 1.0f,
                    }
                })]);
        }

        var importedModelsNode = new SceneNode(rootNode) { Name = "TestImportedModelsNode" };
        //importedModelsNode.GetTransformAs<Transform>()?.ApplyScale(new Vector3(0.1f));

        //var orbitTransform2 = importedModelsNode.SetTransform<OrbitTransform>();
        //orbitTransform2.Radius = 0.0f;
        //orbitTransform2.IgnoreRotation = false;
        //orbitTransform2.RegisterAnimationTick<OrbitTransform>(t => t.Angle += Engine.DilatedDelta * 0.5f);

        string fbxPathDesktop = Path.Combine(desktopDir, "test.fbx");

        var flags =
            PostProcessSteps.Triangulate |
            PostProcessSteps.JoinIdenticalVertices |
            PostProcessSteps.CalculateTangentSpace;

        //ModelImporter.ImportAsync(fbxPathDesktop, flags, null, MaterialFactory, importedModelsNode, 1, true).ContinueWith(OnFinishedAvatar);

        //string sponzaPath = Path.Combine(Engine.Assets.EngineAssetsPath, "Models", "Sponza", "sponza.obj");
        //ModelImporter.ImportAsync(sponzaPath, flags, null, MaterialFactory, importedModelsNode, 1, false).ContinueWith(OnFinishedWorld);

        return world;
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

        var comp = rootNode.AddComponent<HumanoidComponent>()!;
        comp.IsActive = false;

        var knee = comp!.Right.Knee?.Node?.Transform;
        var leg = comp!.Right.Leg?.Node?.Transform;

        //var knee = rootNode.FindDescendant((x, y) => y.Contains("knee", StringComparison.InvariantCultureIgnoreCase))?.GetTransformAs<Transform>();
        leg?.RegisterAnimationTick<Transform>(t => t.Rotation = Quaternion.CreateFromAxisAngle(Globals.Right, XRMath.DegToRad(180 - 90.0f * (MathF.Cos(Engine.ElapsedTime) * 0.5f + 0.5f))));
        knee?.RegisterAnimationTick<Transform>(t => t.Rotation = Quaternion.CreateFromAxisAngle(Globals.Right, XRMath.DegToRad(90.0f * (MathF.Cos(Engine.ElapsedTime) * 0.5f + 0.5f))));
    }
    private static readonly ConcurrentDictionary<string, XRTexture2D> _textureCache = new();

    public static XRMaterial MaterialFactory(string modelFilePath, string name, List<TextureSlot> textures, TextureFlags flags, ShadingMode mode, Dictionary<string, List<MaterialProperty>> properties)
    {
        //Random r = new();

        XRMaterial mat = textures.Count > 0 ?
            new XRMaterial([
                    new ShaderFloat(1.0f, "Opacity"),
                    new ShaderFloat(1.0f, "Specular"),
                    new ShaderFloat(1.0f, "Roughness"),
                    new ShaderFloat(0.0f, "Metallic"),
                    new ShaderFloat(1.0f, "IndexOfRefraction"),
                ], new XRTexture?[textures.Count], ShaderHelper.TextureFragDeferred()!) :
            XRMaterial.CreateLitColorMaterial(new ColorF4(1.0f, 1.0f, 0.0f, 1.0f));
        mat.RenderPass = (int)EDefaultRenderPass.OpaqueDeferredLit;
        mat.Name = name;
        mat.RenderOptions = new RenderingParameters()
        {
            CullMode = ECullMode.None,
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

    private static void LoadTexture(string modelFilePath, List<TextureSlot> textures, XRMaterial mat, int i)
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

        mat.Textures[i] = _textureCache.GetOrAdd(path, TextureFactory);
    }

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
        float fixedHz = 60.0f;

        int primaryX = NativeMethods.GetSystemMetrics(0);
        int primaryY = NativeMethods.GetSystemMetrics(1);

        return new GameStartupSettings()
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
        };
    }
}