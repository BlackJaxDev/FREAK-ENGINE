using XREngine.Players;

namespace XREngine.Input
{
    public abstract class PlayerControllerBase : PawnController
    {
        private PlayerInfo _playerInfo = new();
        public PlayerInfo PlayerInfo
        {
            get => _playerInfo;
            set => SetField(ref _playerInfo, value);
        }
    }
}
