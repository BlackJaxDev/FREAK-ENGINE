using System.Numerics;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Allows code to directly set the world matrix of the scene node.
    /// </summary>
    public class DrivenWorldTransform : TransformBase
    {
        public DrivenWorldTransform() { }
        public DrivenWorldTransform(TransformBase parent)
            : base(parent) { }

        private Matrix4x4 _worldMatrix;

        public void SetWorldMatrix(Matrix4x4 matrix)
        {
            _worldMatrix = matrix;
            MarkWorldModified();
        }

        protected override Matrix4x4 CreateWorldMatrix()
            => _worldMatrix;

        protected override Matrix4x4 CreateLocalMatrix()
            => Parent != null ? Parent.InverseWorldMatrix * _worldMatrix : _worldMatrix;
    }
}