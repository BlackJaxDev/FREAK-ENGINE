using Extensions;
using System.Diagnostics.CodeAnalysis;
using XREngine.Core.Files;
using XREngine.Input;
using XREngine.Native;

namespace XREngine
{
    public static partial class Engine
    {
        public static GameState LoadOrGenerateGameState(Func<GameState>? generateFactory = null, string assetName = "state")
            => LoadOrGenerateAsset(() => generateFactory?.Invoke() ?? new GameState(), assetName);
        public static GameStartupSettings LoadOrGenerateGameSettings(Func<GameStartupSettings>? generateFactory = null, string assetName = "startup")
            => LoadOrGenerateAsset(() => generateFactory?.Invoke() ?? GenerateGameSettings(), assetName);

        public static T LoadOrGenerateGameState<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<T>? generateFactory = null, string assetName = "state") where T : GameState, new()
            => LoadOrGenerateAsset(() => generateFactory?.Invoke() ?? new T(), assetName);
        public static T LoadOrGenerateGameSettings<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<T>? generateFactory = null, string assetName = "startup") where T : GameStartupSettings, new()
            => LoadOrGenerateAsset(() => generateFactory?.Invoke() ?? new T(), assetName);

        public static T LoadOrGenerateAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<T>? generateFactory, string assetName, params string[] folderPaths) where T : XRAsset, new()
        {
            var asset = Assets.LoadGameAsset<T>($"{assetName}.asset");
            if (asset != null)
                return asset;
            asset = generateFactory?.Invoke() ?? Activator.CreateInstance<T>();
            asset.Name = assetName;
            Assets.SaveGameAssetTo(asset);
            return asset;
        }

        private static GameStartupSettings GenerateGameSettings()
        {
            int w = 1920;
            int h = 1080;
            float update = 60.0f;
            float render = 90.0f;

            int primaryX = NativeMethods.GetSystemMetrics(0);
            int primaryY = NativeMethods.GetSystemMetrics(1);

            return new GameStartupSettings()
            {
                StartupWindows =
                [
                    new()
                    {
                        WindowTitle = "FREAK ENGINE",
                        TargetWorld = new Scene.XRWorld(),
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
                    TargetFramesPerSecond = render,
                    TargetUpdatesPerSecond = update,
                    VSync = EVSyncMode.Off,
                }
            };
        }

        public static class State
        {
            public static event Action<LocalPlayerController>? LocalPlayerAdded;

            //Only up to 4 local players, because we only support up to 4 players split screen, realistically. If that.
            public static LocalPlayerController[] LocalPlayers { get; } = new LocalPlayerController[4];
            public static LocalPlayerController GetOrCreateLocalPlayer(ELocalPlayerIndex index)
                => LocalPlayers[(int)index] ?? AddLocalPLayer(index);

            private static LocalPlayerController AddLocalPLayer(ELocalPlayerIndex index)
            {
                var player = new LocalPlayerController(index);
                LocalPlayers[(int)index] = player;
                LocalPlayerAdded?.Invoke(player);
                return player;
            }

            public static LocalPlayerController? GetLocalPlayer(ELocalPlayerIndex index)
                => LocalPlayers.TryGet((int)index);
            public static List<RemotePlayerController> RemotePlayers { get; } = [];
        }
    }
}
