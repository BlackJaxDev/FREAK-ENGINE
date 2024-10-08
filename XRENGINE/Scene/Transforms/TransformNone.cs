﻿using System.Numerics;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Does not transform the node.
    /// </summary>
    /// <param name="parent"></param>
    public class TransformNone : TransformBase
    {
        public TransformNone() { }
        public TransformNone(TransformBase parent)
            : base(parent) { }

        protected override Matrix4x4 CreateLocalMatrix()
            => Matrix4x4.Identity;
    }
}