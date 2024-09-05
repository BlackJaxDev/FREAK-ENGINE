using XREngine.Data.Core;

namespace XREngine.Players
{
    public class PlayerInfo : XRBase
    {
        /// <summary>
        /// Every player has a unique server ID
        /// </summary>
        public int ServerIndex { get; set; }
        /// <summary>
        /// If the player is a local player, this is the index of the player on the local machine.
        /// </summary>
        public ELocalPlayerIndex? LocalIndex { get; set; }
    }
}
