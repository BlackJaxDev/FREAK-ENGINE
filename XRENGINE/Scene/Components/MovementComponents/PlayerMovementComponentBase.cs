using System.Numerics;
using XREngine.Data;

namespace XREngine.Components
{
    /// <summary>
    /// Base component for all components that move a player scene node.
    /// </summary>
    public abstract class PlayerMovementComponentBase : XRComponent
    {
        private Vector3 _frameInputDirection = Vector3.Zero;

        public Vector3 CurrentFrameInputDirection { get; private set; } = Vector3.Zero;
        public Vector3 TargetFrameInputDirection => _frameInputDirection;
        public Vector3 ConstantInputDirection { get; set; } = Vector3.Zero;

        public void AddMovementInput(Vector3 offset)
            => _frameInputDirection += offset;
        public void AddMovementInput(float x, float y, float z)
        {
            _frameInputDirection.X += x;
            _frameInputDirection.Y += y;
            _frameInputDirection.Z += z;
        }
        public virtual Vector3 ConsumeInput()
        {
            CurrentFrameInputDirection = Interp.Lerp(CurrentFrameInputDirection, _frameInputDirection, 0.1f);
            _frameInputDirection = Vector3.Zero;
            return ConstantInputDirection + CurrentFrameInputDirection;
        }
    }
}
