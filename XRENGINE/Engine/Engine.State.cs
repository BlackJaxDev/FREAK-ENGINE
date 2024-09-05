using Extensions;
using System.Drawing;
using XREngine.Input;

namespace XREngine
{
    public static partial class Engine
    {
        public static Color InvalidColor { get; } = Color.Magenta;

        public static class State
        {
            //Only up to 4 local players, because we only support up to 4 players split screen, realistically. If that.
            public static LocalPlayerController[] LocalPlayers { get; } = new LocalPlayerController[4];
            public static LocalPlayerController GetOrCreateLocalPlayer(ELocalPlayerIndex index)
                => LocalPlayers[(int)index] ?? (LocalPlayers[(int)index] = new LocalPlayerController(index));
            public static LocalPlayerController? GetLocalPlayer(ELocalPlayerIndex index)
                => LocalPlayers.TryGet((int)index);
            public static List<RemotePlayerController> RemotePlayers { get; } = [];
        }
    }
}
