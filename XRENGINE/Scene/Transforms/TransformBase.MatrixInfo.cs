using Extensions;
using MathNet.Numerics.Distributions;
using System.Collections.Concurrent;
using System.IO.Compression;
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

        private Matrix4x4 _lastReplicatedMatrix = Matrix4x4.Identity;
        public byte[] EncodeToBytes()
        {
            _timeSinceLastKeyframe += Engine.Time.Timer.Update.Delta;
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
            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
            {
                var deltaBytes = BitConverter.GetBytes(delta);
                gzipStream.Write(deltaBytes, 0, deltaBytes.Length);
                if (delta)
                {
                    // Encode only the difference between the current and last replicated transform
                    var deltaMatrix = _localMatrix.Matrix - _lastReplicatedMatrix;
                    var matrixBytes = MatrixToBytes(deltaMatrix);
                    gzipStream.Write(matrixBytes, 0, matrixBytes.Length);
                }
                else
                {
                    var matrixBytes = MatrixToBytes(_localMatrix.Matrix);
                    gzipStream.Write(matrixBytes, 0, matrixBytes.Length);
                    _lastReplicatedMatrix = _localMatrix.Matrix;
                }
            }
            return memoryStream.ToArray();
        }

        private static byte[] MatrixToBytes(Matrix4x4 matrix)
        {
            var bytes = new byte[16 * sizeof(float)];
            Buffer.BlockCopy(new[]
            {
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            }, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public virtual void DecodeFromBytes(byte[] arr)
        {
            using var memoryStream = new MemoryStream(arr);
            using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            var deltaBytes = new byte[sizeof(bool)];
            gzipStream.Read(deltaBytes, 0, deltaBytes.Length);
            bool delta = BitConverter.ToBoolean(deltaBytes, 0);

            var matrixBytes = new byte[16 * sizeof(float)];
            gzipStream.Read(matrixBytes, 0, matrixBytes.Length);
            var matrix = BytesToMatrix(matrixBytes);

            if (delta)
                _localMatrix.Matrix += matrix;
            else
                _localMatrix.Matrix = matrix;
        }

        private static Matrix4x4 BytesToMatrix(byte[] bytes)
        {
            var values = new float[16];
            Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            return new Matrix4x4(
                values[0], values[1], values[2], values[3],
                values[4], values[5], values[6], values[7],
                values[8], values[9], values[10], values[11],
                values[12], values[13], values[14], values[15]
            );
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
            lock (_children)
                return _children.FirstOrDefault();
        }
        public TransformBase? LastChild()
        {
            lock (_children)
                return _children.LastOrDefault();
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