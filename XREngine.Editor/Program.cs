﻿using XREngine;
using XREngine.Editor;
using XREngine.Native;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene;

internal class Program
{
    /// <summary>
    /// This project serves as a hardcoded game client for development purposes.
    /// This editor will autogenerate the client exe csproj to compile production games.
    /// </summary>
    /// <param name="args"></param>
    private static void Main(string[] args)
    {
        RenderInfo2D.ConstructorOverride = RenderInfo2DConstructor;
        RenderInfo3D.ConstructorOverride = RenderInfo3DConstructor;
        CodeManager.Instance.CompileOnChange = true;

        Engine.Run(/*Engine.LoadOrGenerateGameSettings(() => */GetEngineSettings(EditorWorld.CreateUnitTestWorld(true, false)/*), "startup", false*/), Engine.LoadOrGenerateGameState());
    }

    static EditorRenderInfo2D RenderInfo2DConstructor(IRenderable owner, RenderCommand[] commands)
        => new(owner, commands);
    static EditorRenderInfo3D RenderInfo3DConstructor(IRenderable owner, RenderCommand[] commands)
        => new(owner, commands);

    private static VRGameStartupSettings<EVRActionCategory, EVRGameAction> GetEngineSettings(XRWorld targetWorld)
    {
        int w = 1920;
        int h = 1080;
        float updateHz = 90.0f;
        float renderHz = 0.0f;
        float fixedHz = 45.0f;

        int primaryX = NativeMethods.GetSystemMetrics(0);
        int primaryY = NativeMethods.GetSystemMetrics(1);

        var settings = new VRGameStartupSettings<EVRActionCategory, EVRGameAction>()
        {
            GameName = "XRE EDITOR",
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
            NetworkingType = GameStartupSettings.ENetworkingType.Client,
        };
        if (EditorWorld.VRPawn)
            EditorVR.ApplyVRSettings(settings);
        return settings;
    }
}