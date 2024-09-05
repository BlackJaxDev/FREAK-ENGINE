using XREngine.Components;
namespace XREngine
{
    public class GameMode
    {
        public Dictionary<PawnComponent, Queue<ELocalPlayerIndex>> PossessionQueue = [];

        /// <summary>
        /// Immediately possesses the given pawn with the provided player.
        /// </summary>
        /// <param name="pawnComponent"></param>
        /// <param name="possessor"></param>
        public void ForcePossession(PawnComponent pawnComponent, ELocalPlayerIndex possessor)
        {
            var localPLayer = Engine.State.GetLocalPlayer(possessor);
            if (localPLayer != null)
                pawnComponent.Controller = localPLayer;
            else
                Debug.Out($"Local player {possessor} does not exist.");
        }

        /// <summary>
        /// Queues the given pawn for possession by the provided player.
        /// The player won't posses the pawn until all other players in the queue have gained and released possession of the pawn first.
        /// </summary>
        /// <param name="pawnComponent"></param>
        /// <param name="possessor"></param>
        public void EnqueuePossession(PawnComponent pawnComponent, ELocalPlayerIndex possessor)
        {
            if (pawnComponent.Controller is null)
                ForcePossession(pawnComponent, possessor);
            else
            {
                if (!PossessionQueue.ContainsKey(pawnComponent))
                    PossessionQueue[pawnComponent] = new Queue<ELocalPlayerIndex>();

                PossessionQueue[pawnComponent].Enqueue(possessor);
                pawnComponent.PreUnpossessed += OnPawnUnPossessing;
            }
        }

        private void OnPawnUnPossessing(PawnComponent pawnComponent)
        {
            if (!PossessionQueue.TryGetValue(pawnComponent, out Queue<ELocalPlayerIndex>? value))
                return;
            
            var possessor = value.Dequeue();
            if (value.Count == 0)
            {
                PossessionQueue.Remove(pawnComponent);
                ForcePossession(pawnComponent, possessor);
            }
        }
    }
}