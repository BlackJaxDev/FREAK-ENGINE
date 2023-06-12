using System.Collections;
using XREngine.Data;
using XREngine.Data.Transforms;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Scenes.Transforms
{
    /// <summary>
    /// Represents the basis for transforming a heirarchy of scene nodes.
    /// Inherit from this class to create custom transformation implementations, or use the Transform class for default functionality.
    /// This class is thread-safe.
    /// </summary>
    public abstract class TransformBase : IList, IList<TransformBase>, IEnumerable<TransformBase>
    {
        public XEvent<TransformBase> LocalMatrixChanged;
        public XEvent<TransformBase> InverseLocalMatrixChanged;
        public XEvent<TransformBase> WorldMatrixChanged;
        public XEvent<TransformBase> InverseWorldMatrixChanged;

        protected TransformBase(SceneNode node, TransformBase? parent)
        {
            _sceneNode = node;
            _parent = parent;
            _children = new List<TransformBase>();

            _localMatrix = new MatrixInfo { Modified = true };
            _worldMatrix = new MatrixInfo { Modified = true };
            _inverseLocalMatrix = new MatrixInfo { Modified = true };
            _inverseWorldMatrix = new MatrixInfo { Modified = true };

            LocalMatrixChanged = new XEvent<TransformBase>();
            InverseLocalMatrixChanged = new XEvent<TransformBase>();
            WorldMatrixChanged = new XEvent<TransformBase>();
            InverseWorldMatrixChanged = new XEvent<TransformBase>();
        }

        private class MatrixInfo
        {
            public Matrix Matrix;
            public bool Modified;
        }

        protected readonly object _lock = new();
        private SceneNode _sceneNode;
        public SceneNode SceneNode
        {
            get => _sceneNode;
            set => _sceneNode = value;
        }

        private TransformBase? _parent;
        public TransformBase? Parent
        {
            get => _parent;
            set
            {
                lock (_lock)
                {
                    _parent = value;
                    MarkWorldModified();
                }
            }
        }

        private readonly List<TransformBase> _children;
        public List<TransformBase> Children => _children;

        private readonly MatrixInfo _localMatrix;
        public Matrix LocalMatrix
        {
            get
            {
                lock (_lock)
                {
                    if (_localMatrix.Modified)
                    {
                        _localMatrix.Matrix = CreateLocalMatrix();
                        _localMatrix.Modified = false;
                        LocalMatrixChanged?.Invoke(this);
                    }
                    return _localMatrix.Matrix;
                }
            }
        }

        private readonly MatrixInfo _worldMatrix;
        public Matrix WorldMatrix
        {
            get
            {
                lock (_lock)
                {
                    if (_worldMatrix.Modified)
                    {
                        _worldMatrix.Matrix = Parent == null ? LocalMatrix : Parent.WorldMatrix * LocalMatrix;
                        _worldMatrix.Modified = false;
                        WorldMatrixChanged?.Invoke(this);
                    }
                    return _worldMatrix.Matrix;
                }
            }
        }

        private readonly MatrixInfo _inverseLocalMatrix;
        public Matrix InverseLocalMatrix
        {
            get
            {
                lock (_lock)
                {
                    if (_inverseLocalMatrix.Modified)
                    {
                        _inverseLocalMatrix.Matrix = LocalMatrix.Inverted();
                        _inverseLocalMatrix.Modified = false;
                        InverseLocalMatrixChanged?.Invoke(this);
                    }
                    return _inverseLocalMatrix.Matrix;
                }
            }
        }

        private readonly MatrixInfo _inverseWorldMatrix;
        public Matrix InverseWorldMatrix
        {
            get
            {
                lock (_lock)
                {
                    if (_inverseWorldMatrix.Modified)
                    {
                        _inverseWorldMatrix.Matrix = WorldMatrix.Inverted();
                        _inverseWorldMatrix.Modified = false;
                        InverseWorldMatrixChanged?.Invoke(this);
                    }
                    return _inverseWorldMatrix.Matrix;
                }
            }
        }

        public Matrix ParentWorldMatrix => Parent?.WorldMatrix ?? Matrix.Identity;
        public Matrix ParentInverseWorldMatrix => Parent?.InverseWorldMatrix ?? Matrix.Identity;

        protected abstract Matrix CreateLocalMatrix();

        protected void MarkLocalModified()
        {
            lock (_lock)
            {
                _localMatrix.Modified = true;
                _inverseLocalMatrix.Modified = true;
                MarkWorldModified();
            }
        }

        protected void MarkWorldModified()
        {
            lock (_lock)
            {
                _worldMatrix.Modified = true;
                _inverseWorldMatrix.Modified = true;
                foreach (TransformBase child in Children)
                    child.MarkWorldModified();
            }
        }

        public void AddChild(TransformBase child)
        {
            lock (_lock)
            {
                Children.Add(child);
                child.Parent = this;
            }
        }

        public void RemoveChild(TransformBase child)
        {
            lock (_lock)
            {
                Children.Remove(child);
                child.Parent = null;
            }
        }

        public int Count => ((ICollection<TransformBase>)Children).Count;
        public bool IsReadOnly => ((ICollection<TransformBase>)Children).IsReadOnly;
        public bool IsFixedSize => ((IList)Children).IsFixedSize;
        public bool IsSynchronized => ((ICollection)Children).IsSynchronized;
        public object SyncRoot => ((ICollection)Children).SyncRoot;

        object? IList.this[int index] { get => ((IList)Children)[index]; set => ((IList)Children)[index] = value; }
        public TransformBase this[int index] { get => ((IList<TransformBase>)Children)[index]; set => ((IList<TransformBase>)Children)[index] = value; }

        public IEnumerator<TransformBase> GetEnumerator() => ((IEnumerable<TransformBase>)Children).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Children).GetEnumerator();
        public int IndexOf(TransformBase item) => ((IList<TransformBase>)Children).IndexOf(item);
        public void Insert(int index, TransformBase item) => ((IList<TransformBase>)Children).Insert(index, item);
        public void RemoveAt(int index) => ((IList<TransformBase>)Children).RemoveAt(index);
        public void Add(TransformBase item) => ((ICollection<TransformBase>)Children).Add(item);
        public void Clear() => ((ICollection<TransformBase>)Children).Clear();
        public bool Contains(TransformBase item) => ((ICollection<TransformBase>)Children).Contains(item);
        public void CopyTo(TransformBase[] array, int arrayIndex) => ((ICollection<TransformBase>)Children).CopyTo(array, arrayIndex);
        public bool Remove(TransformBase item) => ((ICollection<TransformBase>)Children).Remove(item);
        public int Add(object? value) => ((IList)Children).Add(value);
        public bool Contains(object? value) => ((IList)Children).Contains(value);
        public int IndexOf(object? value) => ((IList)Children).IndexOf(value);
        public void Insert(int index, object? value) => ((IList)Children).Insert(index, value);
        public void Remove(object? value) => ((IList)Children).Remove(value);
        public void CopyTo(Array array, int index) => ((ICollection)Children).CopyTo(array, index);
    }
}