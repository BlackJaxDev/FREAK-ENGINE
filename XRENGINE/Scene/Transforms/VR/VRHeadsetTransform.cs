using System.Numerics;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// The transform for the VR headset.
    /// </summary>
    /// <param name="parent"></param>
    public class VRHeadsetTransform : TransformBase
    {
        public VRHeadsetTransform()
        {
            Engine.VRState.RecalcMatrixOnDraw += VRState_RecalcMatrixOnDraw;
        }
        public VRHeadsetTransform(TransformBase parent)
            : base(parent)
        {
            Engine.VRState.RecalcMatrixOnDraw += VRState_RecalcMatrixOnDraw;
        }

        private void VRState_RecalcMatrixOnDraw()
        {
            ParallelDepthRecalculate();
        }

        protected override Matrix4x4 CreateLocalMatrix()
        {
            var headset = Engine.VRState.Api.Headset;
            if (headset is null)
                return Matrix4x4.Identity;

            //MarkLocalModified();
            return headset.RenderDeviceToAbsoluteTrackingMatrix;
        }
    }
}