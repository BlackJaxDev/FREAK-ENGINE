using XREngine.Scene;

namespace XREngine.Editor;

internal partial class Program
{
    private static void Main(string[] args)
    {
        //TODO: use args?

        Engine.Initialize(GetEngineSettings(), GetGameState());
        Engine.Run();
    }

    private static GameState GetGameState()
    {
        return new GameState()
        {

        };
    }

    private static GameStartupSettings GetEngineSettings(XRWorld? targetWorld = null)
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
            OutputVerbosity = EOutputVerbosity.Verbose,
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