using Extensions;
using System.Numerics;

namespace XREngine.Scene.Transforms
{
    public abstract partial class TransformBase
    {
        public float DistanceTo(TransformBase other)
            => WorldTranslation.Distance(other.WorldTranslation);
        public float DistanceToParent()
            => WorldTranslation.Distance(Parent?.WorldTranslation ?? Vector3.Zero);

        private float _replicationKeyframeIntervalSec = 5.0f;
        public float ReplicationKeyframeIntervalSec
        {
            get => _replicationKeyframeIntervalSec;
            set => SetField(ref _replicationKeyframeIntervalSec, value);
        }

        public float TimeSinceLastKeyframeReplicated => _timeSinceLastKeyframe;

        private float _timeSinceLastKeyframe = 0;
        public byte[] EncodeToBytes()
        {
            _timeSinceLastKeyframe += Engine.Time.Timer.Collect.Delta;
            if (_timeSinceLastKeyframe > ReplicationKeyframeIntervalSec)
            {
                _timeSinceLastKeyframe = 0;
                return EncodeToBytes(false);
            }
            else
                return EncodeToBytes(true);
        }
        public virtual byte[] EncodeToBytes(bool delta)
        {
            return [];
        }
        public virtual void DecodeFromBytes(byte[] arr)
        {
            
        }

        public Vector3 InverseTransformPoint(Vector3 worldPosition)
            => Vector3.Transform(worldPosition, InverseWorldMatrix);
        public Vector3 TransformPoint(Vector3 localPosition)
            => Vector3.Transform(localPosition, WorldMatrix);

        public Vector3 TransformDirection(Vector3 localDirection)
            => Vector3.TransformNormal(localDirection, WorldMatrix);
        public Vector3 InverseTransformDirection(Vector3 worldDirection)
            => Vector3.TransformNormal(worldDirection, InverseWorldMatrix);

        public TransformBase? FirstChild()
        {
            lock (Children)
                return Children.FirstOrDefault();
        }
        public TransformBase? LastChild()
        {
            lock (Children)
                return Children.LastOrDefault();
        }

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