using XREngine.Components;
using XREngine.Data.Core;

namespace XREngine.Input
{
    /// <summary>
    /// This base class is used to send input information to a movement component for an actor.
    /// Input can come from a local or server player, being an actual person or an AI (these are subclasses to pawn controller).
    /// </summary>
    public abstract class PawnController : XRObjectBase
    {
        //TODO: gamemode vs pawncontroller possession queue usage?
        protected Queue<PawnComponent> _pawnPossessionQueue = new();

        protected PawnComponent? _controlledPawn;
        public virtual PawnComponent? ControlledPawn
        {
            get => _controlledPawn;
            set
            {
                SetField(ref _controlledPawn, value);

                if (_controlledPawn is null && _pawnPossessionQueue != null && _pawnPossessionQueue.Count != 0)
                    _controlledPawn = _pawnPossessionQueue.Dequeue();
            }
        }
        /// <summary>
        /// Queues the given pawn for possession.
        /// If the currently possessed pawn is null, possesses the given pawn immediately.
        /// </summary>
        /// <param name="pawn">The pawn to possess.</param>
        public void EnqueuePosession(PawnComponent pawn)
        {
            if (ControlledPawn is null)
                ControlledPawn = pawn;
            else
                _pawnPossessionQueue.Enqueue(pawn);
        }

        protected override void OnDestroying()
        {
            base.OnDestroying();
            UnlinkControlledPawn();
        }

        public void UnlinkControlledPawn()
        {
            _pawnPossessionQueue.Clear();
            ControlledPawn = null;
        }
    }
}
