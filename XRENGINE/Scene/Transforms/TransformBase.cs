using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using XREngine.Data.Core;
using XREngine.Rendering.UI;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Represents the basis for transforming a scene node in the hierarchy.
    /// Inherit from this class to create custom transformation implementations, or use the Transform class for default functionality.
    /// This class is thread-safe.
    /// </summary>
    public abstract class TransformBase : XRWorldObjectBase, IList, IList<TransformBase>, IEnumerable<TransformBase>
    {
        public XREvent<TransformBase> LocalMatrixChanged;
        public XREvent<TransformBase> InverseLocalMatrixChanged;
        public XREvent<TransformBase> WorldMatrixChanged;
        public XREvent<TransformBase> InverseWorldMatrixChanged;

        protected TransformBase(TransformBase? parent)
        {
            _sceneNode = null;
            _parent = parent;
            _children = [];

            _localMatrix = new MatrixInfo { Modified = true };
            _worldMatrix = new MatrixInfo { Modified = true };
            _inverseLocalMatrix = new MatrixInfo { Modified = true };
            _inverseWorldMatrix = new MatrixInfo { Modified = true };

            LocalMatrixChanged = new XREvent<TransformBase>();
            InverseLocalMatrixChanged = new XREvent<TransformBase>();
            WorldMatrixChanged = new XREvent<TransformBase>();
            InverseWorldMatrixChanged = new XREvent<TransformBase>();
        }

        private class MatrixInfo
        {
            public Matrix4x4 Matrix = Matrix4x4.Identity;
            public bool Modified = true;
        }

        protected readonly object _lock = new();

        private SceneNode? _sceneNode;
        /// <summary>
        /// This is the scene node that this transform is attached to and affects.
        /// Scene nodes are used to house components in relation to the scene hierarchy.
        /// </summary>
        public virtual SceneNode? SceneNode
        {
            get => _sceneNode;
            set => SetField(ref _sceneNode, value);
        }

        private TransformBase? _parent;
        /// <summary>
        /// The parent of this transform.
        /// Will affect this transform's world matrix.
        /// </summary>
        public virtual TransformBase? Parent
        {
            get => _parent;
            set
            {
                lock (_lock)
                {
                    SetField(ref _parent, value);
                    MarkWorldModified();
                }
            }
        }

        private readonly EventList<TransformBase> _children;
        public EventList<TransformBase> Children => _children;

        private readonly MatrixInfo _localMatrix;
        /// <summary>
        /// This transform's local matrix relative to its parent.
        /// </summary>
        public Matrix4x4 LocalMatrix
        {
            get
            {
                lock (_lock)
                {
                    if (_localMatrix.Modified)
                    {
                        _localMatrix.Matrix = CreateLocalMatrix();
                        _localMatrix.Modified = false;
                        OnLocalMatrixChanged();
                    }
                    return _localMatrix.Matrix;
                }
            }
        }

        protected virtual void OnLocalMatrixChanged()
        {
            LocalMatrixChanged.Invoke(this);
        }

        private readonly MatrixInfo _worldMatrix;
        /// <summary>
        /// This transform's world matrix relative to the root of the scene (all ancestor transforms accounted for).
        /// </summary>
        public Matrix4x4 WorldMatrix
        {
            get
            {
                lock (_lock)
                {
                    if (_worldMatrix.Modified)
                    {
                        _worldMatrix.Matrix = CreateWorldMatrix();
                        _worldMatrix.Modified = false;
                        OnWorldMatrixChanged();
                    }
                    return _worldMatrix.Matrix;
                }
            }
        }

        protected virtual void OnWorldMatrixChanged()
        {
            WorldMatrixChanged.Invoke(this);
        }

        private readonly MatrixInfo _inverseLocalMatrix;
        /// <summary>
        /// The inverse of this transform's local matrix.
        /// Calculated when requested if needed and cached until invalidated.
        /// </summary>
        public Matrix4x4 InverseLocalMatrix
        {
            get
            {
                lock (_lock)
                {
                    if (_inverseLocalMatrix.Modified && TryCreateInverseLocalMatrix(out Matrix4x4 inverted))
                    {
                        _inverseLocalMatrix.Matrix = inverted;
                        _inverseLocalMatrix.Modified = false;
                        OnInverseLocalMatrixChanged();
                    }
                    return _inverseLocalMatrix.Matrix;
                }
            }
        }

        protected virtual void OnInverseLocalMatrixChanged()
        {
            InverseLocalMatrixChanged.Invoke(this);
        }

        private readonly MatrixInfo _inverseWorldMatrix;
        /// <summary>
        /// The inverse of this transform's world matrix.
        /// Calculated when requested if needed and cached until invalidated.
        /// </summary>
        public Matrix4x4 InverseWorldMatrix
        {
            get
            {
                lock (_lock)
                {
                    if (_inverseWorldMatrix.Modified && TryCreateInverseWorldMatrix(out Matrix4x4 inverted))
                    {
                        _inverseWorldMatrix.Matrix = inverted;
                        _inverseWorldMatrix.Modified = false;
                        OnInverseWorldMatrixChanged();
                    }
                    return _inverseWorldMatrix.Matrix;
                }
            }
        }

        protected virtual void OnInverseWorldMatrixChanged()
        {
            InverseWorldMatrixChanged.Invoke(this);
        }

        /// <summary>
        /// Returns the parent world matrix, or identity if no parent.
        /// </summary>
        public Matrix4x4 ParentWorldMatrix => Parent?.WorldMatrix ?? Matrix4x4.Identity;
        /// <summary>
        /// Returns the inverse of the parent world matrix, or identity if no parent.
        /// </summary>
        public Matrix4x4 ParentInverseWorldMatrix => Parent?.InverseWorldMatrix ?? Matrix4x4.Identity;

        /// <summary>
        /// This transform's world up vector.
        /// </summary>
        public Vector3 WorldUp => Vector3.TransformNormal(Globals.Up, WorldMatrix);
        /// <summary>
        /// This transform's world right vector.
        /// </summary>
        public Vector3 WorldRight => Vector3.TransformNormal(Globals.Right, WorldMatrix);
        /// <summary>
        /// This transform's world forward vector.
        /// </summary>
        public Vector3 WorldForward => Vector3.TransformNormal(Globals.Forward, WorldMatrix);

        /// <summary>
        /// This transform's local up vector.
        /// </summary>
        public Vector3 LocalUp => Vector3.TransformNormal(Globals.Up, LocalMatrix);
        /// <summary>
        /// This transform's local right vector.
        /// </summary>
        public Vector3 LocalRight => Vector3.TransformNormal(Globals.Right, LocalMatrix);
        /// <summary>
        /// This transform's local forward vector.
        /// </summary>
        public Vector3 LocalForward => Vector3.TransformNormal(Globals.Forward, LocalMatrix);

        /// <summary>
        /// This transform's position in world space.
        /// </summary>
        public Vector3 WorldTranslation => WorldMatrix.Translation;
        /// <summary>
        /// This transform's position in local space relative to the parent.
        /// </summary>
        public Vector3 LocalTranslation => LocalMatrix.Translation;

        protected virtual Matrix4x4 CreateWorldMatrix()
            => Parent is null ? LocalMatrix : Parent.WorldMatrix * LocalMatrix;
        protected virtual bool TryCreateInverseLocalMatrix(out Matrix4x4 inverted)
            => Matrix4x4.Invert(LocalMatrix, out inverted);
        protected virtual bool TryCreateInverseWorldMatrix(out Matrix4x4 inverted)
            => Matrix4x4.Invert(WorldMatrix, out inverted);

        protected abstract Matrix4x4 CreateLocalMatrix();

        /// <summary>
        /// Marks the local matrix as modified, which will cause it to be recalculated on the next access.
        /// </summary>
        protected void MarkLocalModified()
        {
            lock (_lock)
            {
                _localMatrix.Modified = true;
                _inverseLocalMatrix.Modified = true;
                MarkWorldModified();
            }
        }

        /// <summary>
        /// Marks the world matrix as modified, which will cause it to be recalculated on the next access.
        /// </summary>
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

        protected internal virtual void Start() { }
        protected internal virtual void Stop() { }

        /// <summary>
        /// Adds a child to this transform.
        /// </summary>
        /// <param name="child"></param>
        public void AddChild(TransformBase child)
        {
            lock (_lock)
            {
                Children.Add(child);
                child.Parent = this;
            }
        }

        /// <summary>
        /// Removes a child from this transform.
        /// </summary>
        /// <param name="child"></param>
        public void RemoveChild(TransformBase child)
        {
            lock (_lock)
            {
                Children.Remove(child);
                child.Parent = null;
            }
        }

        /// <summary>
        /// Removes all children from this transform.
        /// </summary>
        public void ClearChildren()
        {
            lock (_lock)
            {
                foreach (TransformBase child in Children)
                    child.Parent = null;
                Children.Clear();
            }
        }

        /// <summary>
        /// Adds several children to this transform.
        /// </summary>
        /// <param name="children"></param>
        public void AddRange(IEnumerable<TransformBase> children)
        {
            lock (_lock)
            {
                foreach (TransformBase child in children)
                    AddChild(child);
            }
        }

        /// <summary>
        /// Removes several children from this transform.
        /// </summary>
        /// <param name="children"></param>
        public void RemoveRange(IEnumerable<TransformBase> children)
        {
            lock (_lock)
            {
                foreach (TransformBase child in children)
                    RemoveChild(child);
            }
        }

        /// <summary>
        /// Adds several children to this transform.
        /// </summary>
        /// <param name="children"></param>
        public void AddRange(params TransformBase[] children)
        {
            lock (_lock)
            {
                foreach (TransformBase child in children)
                    AddChild(child);
            }
        }

        /// <summary>
        /// Removes several children from this transform.
        /// </summary>
        /// <param name="children"></param>
        public void RemoveRange(params TransformBase[] children)
        {
            lock (_lock)
            {
                foreach (TransformBase child in children)
                    RemoveChild(child);
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

        /// <summary>
        /// Used to verify if the placement info for a child is the right type before being returned to the requester.
        /// </summary>
        /// <param name="childTransform"></param>
        public virtual void VerifyPlacementInfo(UITransform childTransform) { }
        /// <summary>
        /// Used by the physics system to derive a world matrix from a physics body into the components used by this transform.
        /// </summary>
        /// <param name="value"></param>
        public virtual void DeriveWorldMatrix(Matrix4x4 value) { }

        [RequiresUnreferencedCode("This method is used to find all transform types in all assemblies in the current domain and should not be trimmed.")]
        public static Type[] GetAllTransformTypes() 
            => AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetExportedTypes())
                .Where(x => x.IsSubclassOf(typeof(TransformBase)))
                .ToArray();

        [RequiresUnreferencedCode("This method is used to find all transform types in all assemblies in the current domain and should not be trimmed.")]
        public static string[] GetFriendlyTransformTypeSelector()
            => GetAllTransformTypes().Select(x => x.Name).ToArray();
    }
}