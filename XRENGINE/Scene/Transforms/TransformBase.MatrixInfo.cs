using System.Numerics;

namespace XREngine.Scene.Transforms
{
    public abstract partial class TransformBase
    {
        private class MatrixInfo
        {
            private readonly ReaderWriterLockSlim _modifiedLock = new();
            private readonly ReaderWriterLockSlim _matrixLock = new();

            private Matrix4x4 _matrix = Matrix4x4.Identity;
            private bool _modified = true;

            public bool NeedsRecalc
            {
                get
                {
                    _modifiedLock.EnterReadLock();
                    bool modified = _modified;
                    _modifiedLock.ExitReadLock();
                    return modified;
                }
                set
                {
                    _modifiedLock.EnterWriteLock();
                    _modified = value;
                    _modifiedLock.ExitWriteLock();
                }
            }
            public Matrix4x4 Matrix
            {
                get
                {
                    _matrixLock.EnterReadLock();
                    Matrix4x4 matrix = _matrix;
                    _matrixLock.ExitReadLock();
                    return matrix;
                }
                set
                {
                    _matrixLock.EnterWriteLock();
                    _matrix = value;
                    _matrixLock.ExitWriteLock();
                }
            }
        }
    }
}