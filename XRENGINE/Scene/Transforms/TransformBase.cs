using Extensions;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using XREngine.Data.Core;
using XREngine.Rendering.UI;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Represents the basis for transforming a scene node in the hierarchy.
    /// Inherit from this class to create custom transformation implementations, or use the Transform class for default functionality.
    /// This class is thread-safe.
    /// </summary>
    public abstract partial class TransformBase : XRWorldObjectBase, IList, IList<TransformBase>, IEnumerable<TransformBase>
    {
        public XREvent<TransformBase> LocalMatrixChanged;
        public XREvent<TransformBase> InverseLocalMatrixChanged;
        public XREvent<TransformBase> WorldMatrixChanged;
        public XREvent<TransformBase> InverseWorldMatrixChanged;

        protected TransformBase() : this(null) { }
        protected TransformBase(TransformBase? parent)
        {
            _sceneNode = null;
            _parent = parent;
            Depth = parent?.Depth + 1 ?? 0;
            _children = [];
            _children.PostAnythingAdded += ChildAdded;
            _children.PostAnythingRemoved += ChildRemoved;

            _localMatrix = new MatrixInfo { NeedsRecalc = true };
            _worldMatrix = new MatrixInfo { NeedsRecalc = true };
            _inverseLocalMatrix = new MatrixInfo { NeedsRecalc = true };
            _inverseWorldMatrix = new MatrixInfo { NeedsRecalc = true };

            LocalMatrixChanged = new XREvent<TransformBase>();
            InverseLocalMatrixChanged = new XREvent<TransformBase>();
            WorldMatrixChanged = new XREvent<TransformBase>();
            InverseWorldMatrixChanged = new XREvent<TransformBase>();
        }

        private void ChildAdded(TransformBase e)
            => e.Parent = this;

        private void ChildRemoved(TransformBase e)
            => e.Parent = null;

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

        public int Depth { get; private set; } = 0;

        private TransformBase? _parent;
        /// <summary>
        /// The parent of this transform.
        /// Will affect this transform's world matrix.
        /// </summary>
        public virtual TransformBase? Parent
        {
            get => _parent;
            set => SetField(ref _parent, value);
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(Parent):
                        if (_parent is not null)
                        {
                            lock (_parent.Children)
                                _parent.Children.Remove(this);
                        }
                        break;
                }
            }
            return change;
        }
        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Parent):
                    if (_parent is not null)
                    {
                        Depth = _parent.Depth + 1;

                        lock (_parent.Children)
                            _parent.Children.Add(this);
                    }
                    else
                        Depth = 0;
                    //TODO: world is not set here
                    if (SceneNode is not null)
                        SceneNode.World = World;
                    MarkWorldModified();
                    break;
                case nameof(SceneNode):
                    World = SceneNode?.World;
                    break;
            }
        }

        public interface IBoneTransformDependent
        {

        }

        /// <summary>
        /// These objects depend on the bone transform and will be updated when the bone transform is updated.
        /// Use these to verify the bone is on screen and should be updated.
        /// </summary>
        public EventList<IBoneTransformDependent> Dependencies { get; } = [];

        /// <summary>
        /// 
        /// </summary>
        internal void TryParallelDepthRecalculate()
        {
            VerifyLocal();
            VerifyLocalInv();
            VerifyWorld();
            VerifyWorldInv();
            lock (Children)
                foreach (TransformBase child in Children)
                    child.MarkWorldModified();
        }

        private readonly EventList<TransformBase> _children;
        public EventList<TransformBase> Children => _children;

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

        #region Local Matrix
        private readonly MatrixInfo _localMatrix;
        /// <summary>
        /// This transform's local matrix relative to its parent.
        /// </summary>
        public Matrix4x4 LocalMatrix
        {
            get
            {
                VerifyLocal();
                return _localMatrix.Matrix;
            }
        }

        private void VerifyLocal()
        {
            if (!_localMatrix.NeedsRecalc)
                return;
            
            _localMatrix.NeedsRecalc = false;
            RecalcLocal();
        }

        internal void RecalcLocal()
        {
            _localMatrix.Matrix = CreateLocalMatrix();
            _inverseLocalMatrix.NeedsRecalc = true;
            OnLocalMatrixChanged();
        }

        protected virtual void OnLocalMatrixChanged()
            => LocalMatrixChanged.Invoke(this);
        #endregion

        #region World Matrix
        private readonly MatrixInfo _worldMatrix;
        /// <summary>
        /// This transform's world matrix relative to the root of the scene (all ancestor transforms accounted for).
        /// </summary>
        public Matrix4x4 WorldMatrix
        {
            get
            {
                VerifyWorld();
                return _worldMatrix.Matrix;
            }
        }

        private void VerifyWorld()
        {
            if (!_worldMatrix.NeedsRecalc)
                return;
            
            _worldMatrix.NeedsRecalc = false;
            RecalcWorld(false);
        }

        private void RecalcWorld(bool allowSetLocal)
        {
            _worldMatrix.Matrix = CreateWorldMatrix();
            _inverseWorldMatrix.NeedsRecalc = true;
            if (allowSetLocal && !_localMatrix.NeedsRecalc)
            {
                _localMatrix.Matrix = GenerateLocalMatrixFromWorld();
                _inverseLocalMatrix.NeedsRecalc = true;
                OnLocalMatrixChanged();
            }
            OnWorldMatrixChanged();
        }

        private Matrix4x4 GenerateLocalMatrixFromWorld()
            => Parent is null || !Matrix4x4.Invert(Parent.WorldMatrix, out Matrix4x4 inverted)
                ? WorldMatrix
                : WorldMatrix * inverted;

        protected virtual void OnWorldMatrixChanged()
            => WorldMatrixChanged.Invoke(this);
        #endregion

        #region Inverse Local Matrix
        private readonly MatrixInfo _inverseLocalMatrix;
        /// <summary>
        /// The inverse of this transform's local matrix.
        /// Calculated when requested if needed and cached until invalidated.
        /// </summary>
        public Matrix4x4 InverseLocalMatrix
        {
            get
            {
                VerifyLocalInv();
                return _inverseLocalMatrix.Matrix;
            }
        }

        private void VerifyLocalInv()
        {
            VerifyLocal();

            if (!_inverseLocalMatrix.NeedsRecalc)
                return;
            
            _inverseLocalMatrix.NeedsRecalc = false;
            RecalcLocalInv();
        }

        internal void RecalcLocalInv()
        {
            if (!TryCreateInverseLocalMatrix(out Matrix4x4 inverted))
                return;
            
            _inverseLocalMatrix.Matrix = inverted;
            OnInverseLocalMatrixChanged();
        }

        protected virtual void OnInverseLocalMatrixChanged()
            => InverseLocalMatrixChanged.Invoke(this);

        #endregion

        #region Inverse World Matrix
        private readonly MatrixInfo _inverseWorldMatrix;
        /// <summary>
        /// The inverse of this transform's world matrix.
        /// Calculated when requested if needed and cached until invalidated.
        /// </summary>
        public Matrix4x4 InverseWorldMatrix
        {
            get
            {
                VerifyWorldInv();
                return _inverseWorldMatrix.Matrix;
            }
        }

        private void VerifyWorldInv()
        {
            VerifyWorld();

            if (!_inverseWorldMatrix.NeedsRecalc)
                return;
            
            _inverseWorldMatrix.NeedsRecalc = false;
            RecalcWorldInv(false);
        }

        internal void RecalcWorldInv(bool allowSetLocal)
        {
            if (!TryCreateInverseWorldMatrix(out Matrix4x4 inverted))
                return;
            
            _inverseWorldMatrix.Matrix = inverted;
            if (allowSetLocal && !_inverseLocalMatrix.NeedsRecalc)
            {
                _inverseLocalMatrix.Matrix = GenerateInverseLocalMatrixFromInverseWorld();
                OnInverseLocalMatrixChanged();
            }
            OnInverseWorldMatrixChanged();
        }

        private Matrix4x4 GenerateInverseLocalMatrixFromInverseWorld()
            => Parent is null || !Matrix4x4.Invert(Parent.WorldMatrix, out Matrix4x4 inverted)
                ? InverseWorldMatrix
                : inverted * InverseWorldMatrix;

        protected virtual void OnInverseWorldMatrixChanged()
            => InverseWorldMatrixChanged.Invoke(this);
        #endregion

        #region Overridable Methods
        protected virtual Matrix4x4 CreateWorldMatrix()
            => Parent is null ? LocalMatrix : LocalMatrix * Parent.WorldMatrix;
        protected virtual bool TryCreateInverseLocalMatrix(out Matrix4x4 inverted)
            => Matrix4x4.Invert(LocalMatrix, out inverted);
        protected virtual bool TryCreateInverseWorldMatrix(out Matrix4x4 inverted)
            => Matrix4x4.Invert(WorldMatrix, out inverted);
        protected abstract Matrix4x4 CreateLocalMatrix();
        #endregion

        /// <summary>
        /// Marks the local matrix as modified, which will cause it to be recalculated on the next access.
        /// </summary>
        protected void MarkLocalModified()
        {
            _localMatrix.NeedsRecalc = true;
            MarkWorldModified();
            World?.AddDirtyTransform(this);
        }

        /// <summary>
        /// Marks the world matrix as modified, which will cause it to be recalculated on the next access.
        /// </summary>
        protected void MarkWorldModified()
        {
            _worldMatrix.NeedsRecalc = true;
            //lock (_children)
            //    foreach (TransformBase child in _children)
            //        child.MarkWorldModified();
            World?.AddDirtyTransform(this);
        }

        ///// <summary>
        ///// Marks the inverse local matrix as modified, which will cause it to be recalculated on the next access.
        ///// </summary>
        //protected void MarkInverseLocalModified()
        //{
        //    _inverseLocalMatrix.Modified = true;
        //    MarkInverseWorldModified();
        //    World?.AddDirtyTransform(this);
        //}

        ///// <summary>
        ///// Marks the inverse world matrix as modified, which will cause it to be recalculated on the next access.
        ///// </summary>
        //protected void MarkInverseWorldModified()
        //{
        //    _inverseWorldMatrix.Modified = true;
        //    foreach (TransformBase child in Children)
        //        child.MarkInverseWorldModified();
        //    World?.AddDirtyTransform(this);
        //}

        //[Flags]
        //public enum ETransformTypeFlags
        //{
        //    None = 0,
        //    Local = 1,
        //    LocalInverse = 2,
        //    World = 4,
        //    WorldInverse = 8,
        //    All = 0xF,
        //}

        protected internal virtual void Start()
        {
            lock (Children)
                foreach (TransformBase child in Children)
                    child.Start();
        }
        protected internal virtual void Stop()
        {
            lock (Children)
                foreach (TransformBase child in Children)
                    child.Stop();
            ClearTicks();
        }

        #region Interfaces
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
        #endregion

        /// <summary>
        /// Used to verify if the placement info for a child is the right type before being returned to the requester.
        /// </summary>
        /// <param name="childTransform"></param>
        public virtual void VerifyPlacementInfo(UITransform childTransform) { }
        /// <summary>
        /// Used by the physics system to derive a world matrix from a physics body into the components used by this transform.
        /// </summary>
        /// <param name="value"></param>
        public void DeriveWorldMatrix(Matrix4x4 value)
            => DeriveLocalMatrix(ParentInverseWorldMatrix * value);
        /// <summary>
        /// Derives components to create the local matrix from the given matrix.
        /// </summary>
        /// <param name="value"></param>
        public virtual void DeriveLocalMatrix(Matrix4x4 value) { }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public static Type[] TransformTypes { get; } = GetAllTransformTypes();

        [RequiresUnreferencedCode("This method is used to find all transform types in all assemblies in the current domain and should not be trimmed.")]
        private static Type[] GetAllTransformTypes() 
            => AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetExportedTypes())
                .Where(x => x.IsSubclassOf(typeof(TransformBase)))
                .ToArray();

        [RequiresUnreferencedCode("This method is used to find all transform types in all assemblies in the current domain and should not be trimmed.")]
        public static string[] GetFriendlyTransformTypeSelector()
            => TransformTypes.Select(FriendlyTransformName).ToArray();

        private static string FriendlyTransformName(Type x)
        {
            DisplayNameAttribute? name = x.GetCustomAttribute<DisplayNameAttribute>();
            return $"{name?.DisplayName ?? x.Name} ({x.Assembly.GetName()})";
        }
    }
}