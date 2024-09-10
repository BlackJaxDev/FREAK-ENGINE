﻿using System.Numerics;

namespace XREngine.Scene.Transforms
{
    public class VRHMDTransform(TransformBase? parent = null) : Transform(parent)
    {
        protected override Matrix4x4 CreateLocalMatrix()
        {
            var headset = Engine.VRState.Api.IsHeadsetPresent ? Engine.VRState.Api.Headset : null;
            if (headset is null)
                return Matrix4x4.Identity;

            Matrix4x4 world = Matrix4x4.CreateFromQuaternion(headset.Rotation) * Matrix4x4.CreateTranslation(headset.Position);
            if (Parent is null || !Matrix4x4.Invert(Parent.WorldMatrix, out Matrix4x4 invParent))
                return world;

            return world * invParent;
        }
    }
}