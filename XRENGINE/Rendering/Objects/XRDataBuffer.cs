using System.Numerics;
using System.Runtime.InteropServices;
using XREngine.Data;
using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRDataBuffer : GenericRenderObject, IDisposable
    {
        public delegate void DelPushSubData(int offset, uint length);
        public delegate void DelSetBlockName(XRRenderProgram program, string blockName);
        public delegate void DelSetBlockIndex(uint blockIndex);

        public event Action? PushDataRequested;
        public event DelPushSubData? PushSubDataRequested;
        public event Action? MapBufferDataRequested;
        public event Action? UnmapBufferDataRequested;
        public event DelSetBlockName? SetBlockNameRequested;
        public event DelSetBlockIndex? SetBlockIndexRequested;

        public XRDataBuffer() { }

        public XRDataBuffer(
            string bindingName,
            EBufferTarget target,
            uint elementCount,
            EComponentType componentType,
            uint componentCount,
            bool normalize,
            bool integral)
        {
            BindingName = bindingName;
            Target = target;

            _componentType = componentType;
            _componentCount = componentCount;
            _elementCount = elementCount;
            _normalize = normalize;
            _integral = integral;

            _source = DataSource.Allocate(Length);
        }

        public XRDataBuffer(
            string bindingName,
            EBufferTarget target,
            bool integral)
        {
            BindingName = bindingName;
            Target = target;
            _integral = integral;
        }

        public XRDataBuffer(
            EBufferTarget target,
            bool integral)
        {
            Target = target;
            _integral = integral;
        }

        /// <summary>
        /// The current mapping state of this buffer.
        /// If the buffer is mapped, this means any updates to the buffer will be shown by the GPU immediately.
        /// If the buffer is not mapped, any updates will have to be pushed to the GPU using PushData or PushSubData.
        /// </summary>
        public List<AbstractRenderAPIObject> ActivelyMapping { get; } = [];

        private bool _mapped = false;
        /// <summary>
        /// Determines if this buffer should be mapped when it is generated.
        /// If the buffer is mapped, this means any updates to the buffer will be shown by the GPU immediately.
        /// If the buffer is not mapped, any updates will have to be pushed to the GPU using PushData or PushSubData.
        /// </summary>
        public bool Mapped
        {
            get => _mapped;
            set => SetField(ref _mapped, value);
        }

        private EBufferTarget _target = EBufferTarget.ArrayBuffer;
        /// <summary>
        /// The type of data this buffer will be used for.
        /// </summary>
        public EBufferTarget Target
        {
            get => _target;
            private set => SetField(ref _target, value);
        }

        private EBufferUsage _usage = EBufferUsage.StaticDraw;
        /// <summary>
        /// Determines how this buffer will be used.
        /// Data can be streamed in/out frequently, be unchanging, or be modified infrequently.
        /// </summary>
        public EBufferUsage Usage
        {
            get => _usage;
            set => SetField(ref _usage, value);
        }

        private EComponentType _componentType;
        public EComponentType ComponentType => _componentType;

        private bool _normalize;
        public bool Normalize
        {
            get => _normalize;
            set => SetField(ref _normalize, value);
        }

        private uint _componentCount;
        public uint ComponentCount
        {
            get => _componentCount;
            set => SetField(ref _componentCount, value);
        }

        private uint _elementCount;
        public uint ElementCount
        {
            get => _elementCount;
            set => SetField(ref _elementCount, value);
        }

        private DataSource? _source = null;
        public DataSource? Source
        {
            get => _source;
            set => SetField(ref _source, value);
        }

        public bool _integral = false;
        /// <summary>
        /// Determines if this buffer has integer-type data or otherwise, floating point.
        /// </summary>
        public bool Integral
        {
            get => _integral;
            set => SetField(ref _integral, value);
        }

        private string _bindingName = string.Empty;
        public string BindingName
        {
            get => _bindingName;
            set => SetField(ref _bindingName, value);
        }

        public uint _divisor = 0;
        public uint InstanceDivisor
        {
            get => _divisor;
            set => SetField(ref _divisor, value);
        }

        public VoidPtr Address => _source!.Address;

        /// <summary>
        /// The total size in bytes of this buffer.
        /// </summary>
        public uint Length => ElementCount * ElementSize;

        /// <summary>
        /// The size in bytes of a single element in the buffer.
        /// </summary>
        public uint ElementSize => ComponentCount * ComponentSize;

        /// <summary>
        /// The size in memory of a single component.
        /// A single element in the buffer can contain multiple components.
        /// </summary>
        private uint ComponentSize
            => _componentType switch
            {
                EComponentType.SByte => sizeof(sbyte),
                EComponentType.Byte => sizeof(byte),
                EComponentType.Short => sizeof(short),
                EComponentType.UShort => sizeof(ushort),
                EComponentType.Int => sizeof(int),
                EComponentType.UInt => sizeof(uint),
                EComponentType.Float => sizeof(float),
                EComponentType.Double => sizeof(double),
                _ => 0,
            };

        //TODO: Vulkan methods
        //public Span<T> BeginUpdate()
        //{
        //    void* data;
        //    Api!.MapMemory(Renderer.device, Memory, 0, Size, 0, &data);
        //    return new Span<T>(data, (int)Size);
        //}

        //public void EndUpdate()
        //    => Api!.UnmapMemory(Renderer.device, Memory);

        //public void Set(int startIndex, params T[] items)
        //{
        //    var span = BeginUpdate();
        //    for (int i = 0; i < items.Length; i++)
        //        span[startIndex + i] = items[i];
        //    EndUpdate();
        //}
        //public void Set(int startIndex, IEnumerable<T> items)
        //{
        //    var span = BeginUpdate();
        //    int i = 0;
        //    foreach (var item in items)
        //        span[startIndex + i++] = item;
        //    EndUpdate();
        //}

        //public void CopyTo(VkBuffer<T> other)
        //{
        //    using var scope = Renderer.NewCommandScope();
        //    BufferCopy copyRegion = new() { Size = Size };
        //    Api!.CmdCopyBuffer(scope.CommandBuffer, Buffer, other.Buffer, 1, ref copyRegion);
        //}

        /// <summary>
        /// Allocates and pushes the buffer to the GPU.
        /// </summary>
        public void PushData()
            => PushDataRequested?.Invoke();

        /// <summary>
        /// Pushes the entire buffer to the GPU. Assumes the buffer has already been allocated using PushData.
        /// </summary>
        public void PushSubData()
            => PushSubData(0, Length);

        /// <summary>
        /// Pushes the a portion of the buffer to the GPU. Assumes the buffer has already been allocated using PushData.
        /// </summary>
        public void PushSubData(int offset, uint length)
            => PushSubDataRequested?.Invoke(offset, length);

        public void MapBufferData()
            => MapBufferDataRequested?.Invoke();
        public void UnmapBufferData()
            => UnmapBufferDataRequested?.Invoke();

        public void SetBlockName(XRRenderProgram program, string blockName)
            => SetBlockNameRequested?.Invoke(program, blockName);

        public void SetBlockIndex(uint blockIndex)
            => SetBlockIndexRequested?.Invoke(blockIndex);

        /// <summary>
        /// Reads the struct value at the given offset into the buffer.
        /// Offset is in bytes; NOT relative to the size of the struct.
        /// </summary>
        /// <typeparam name="T">The type of value to read.</typeparam>
        /// <param name="offset">The offset into the buffer, in bytes.</param>
        /// <returns>The T value at the given offset.</returns>
        public T? Get<T>(uint offset) where T : struct
            => _source != null ? (T?)Marshal.PtrToStructure(_source.Address + offset, typeof(T)) : default;

        /// <summary>
        /// Writes the struct value into the buffer at the given index.
        /// This will not update the data in GPU memory unless this buffer is mapped.
        /// To update the GPU data, call PushData or PushSubData after this call.
        /// </summary>
        /// <typeparam name="T">The type of value to write.</typeparam>
        /// <param name="index">The index of the value in the buffer.</param>
        /// <param name="value">The value to write.</param>
        public void Set<T>(uint index, T value) where T : struct
        {
            if (_source != null)
                Marshal.StructureToPtr(value, _source.Address[index, ElementSize], true);
        }

        public Remapper? SetDataRaw<T>(IList<T> list, bool remap = false) where T : struct
        {
            _componentCount = 1;

            switch (typeof(T))
            {
                case Type t when t == typeof(sbyte):
                    _componentType = EComponentType.SByte;
                    break;
                case Type t when t == typeof(byte):
                    _componentType = EComponentType.Byte;
                    break;
                case Type t when t == typeof(short):
                    _componentType = EComponentType.Short;
                    break;
                case Type t when t == typeof(ushort):
                    _componentType = EComponentType.UShort;
                    break;
                case Type t when t == typeof(int):
                    _componentType = EComponentType.Int;
                    break;
                case Type t when t == typeof(uint):
                    _componentType = EComponentType.UInt;
                    break;
                case Type t when t == typeof(float):
                    _componentType = EComponentType.Float;
                    break;
                case Type t when t == typeof(double):
                    _componentType = EComponentType.Double;
                    break;
                case Type t when t == typeof(Vector2):
                    _componentType = EComponentType.Float;
                    _componentCount = 2;
                    break;
                case Type t when t == typeof(Vector3):
                    _componentType = EComponentType.Float;
                    _componentCount = 3;
                    break;
                case Type t when t == typeof(Vector4):
                    _componentType = EComponentType.Float;
                    _componentCount = 4;
                    break;
                case Type t when t == typeof(Matrix4x4):
                    _componentType = EComponentType.Float;
                    _componentCount = 16;
                    break;
                case Type t when t == typeof(Quaternion):
                    _componentType = EComponentType.Float;
                    _componentCount = 4;
                    break;
                default:
                    throw new InvalidOperationException("Not a proper numeric data type.");
            }

            //Engine.DebugPrint("\nSetting numeric vertex data for buffer " + Index + " - " + Name);

            _normalize = false;
            if (remap)
            {
                Remapper remapper = new();
                remapper.Remap(list, null);
                _elementCount = remapper.ImplementationLength;
                _source = DataSource.Allocate(Length);
                uint stride = ElementSize;
                for (uint i = 0; i < remapper.ImplementationLength; ++i)
                {
                    VoidPtr addr = _source.Address[i, stride];
                    T value = list[remapper.ImplementationTable![i]];
                    //Debug.Write(value.ToString() + " ");
                    Marshal.StructureToPtr(value, addr, true);
                }
                //Engine.DebugPrint();
                return remapper;
            }
            else
            {
                _elementCount = (uint)list.Count;
                _source = DataSource.Allocate(Length);
                uint stride = ElementSize;
                for (uint i = 0; i < list.Count; ++i)
                {
                    VoidPtr addr = _source.Address[i, stride];
                    T value = list[(int)i];
                    //Debug.Write(value.ToString() + " ");
                    Marshal.StructureToPtr(value, addr, true);
                }
                //Engine.DebugPrint("\n");
                return null;
            }
        }
        public Remapper? SetData<T>(IList<T> list, bool remap = false) where T : unmanaged, IBufferable
        {
            //Engine.DebugPrint("\nSetting vertex data for buffer " + Index + " - " + Name);

            IBufferable d = default(T);
            _componentType = d.ComponentType;
            _componentCount = d.ComponentCount;
            _normalize = d.Normalize;

            if (remap)
            {
                Remapper remapper = new();
                remapper.Remap(list, null);

                _elementCount = remapper.ImplementationLength;
                _source = DataSource.Allocate(Length);
                uint stride = ElementSize;
                for (uint i = 0; i < remapper.ImplementationLength; ++i)
                    list[remapper.ImplementationTable![i]].Write(_source.Address[i, stride]);
                return remapper;
            }
            else
            {
                _elementCount = (uint)list.Count;
                _source = DataSource.Allocate(Length);
                uint stride = ElementSize;
                for (uint i = 0; i < list.Count; ++i)
                    list[(int)i].Write(_source.Address[i, stride]);
                return null;
            }
        }

        public Remapper? GetData<T>(out T[] array, bool remap = true) where T : unmanaged, IBufferable
        {
            //Engine.DebugPrint("\nGetting vertex data from buffer " + Index + " - " + Name);

            IBufferable d = default(T);
            var componentType = d.ComponentType;
            var componentCount = d.ComponentCount;
            var normalize = d.Normalize;
            if (componentType != _componentType || componentCount != _componentCount || normalize != _normalize)
                throw new InvalidOperationException("Data type mismatch.");

            uint stride = ElementSize;
            array = new T[_elementCount];
            for (uint i = 0; i < _elementCount; ++i)
            {
                T value = default;
                value.Read(_source!.Address[i, stride]);
                array[i] = value;
            }

            if (!remap)
                return null;

            Remapper remapper = new();
            remapper.Remap(array);
            return remapper;
        }

        public Remapper? GetDataRaw<T>(out T[] array, bool remap = true) where T : struct
        {
            EComponentType componentType = EComponentType.Float;
            var componentCount = 1;
            var normalize = false;
            switch (typeof(T))
            {
                case Type t when t == typeof(sbyte):
                    componentType = EComponentType.SByte;
                    break;
                case Type t when t == typeof(byte):
                    componentType = EComponentType.Byte;
                    break;
                case Type t when t == typeof(short):
                    componentType = EComponentType.Short;
                    break;
                case Type t when t == typeof(ushort):
                    componentType = EComponentType.UShort;
                    break;
                case Type t when t == typeof(int):
                    componentType = EComponentType.Int;
                    break;
                case Type t when t == typeof(uint):
                    componentType = EComponentType.UInt;
                    break;
                case Type t when t == typeof(float):
                    //componentType = EComponentType.Float;
                    break;
                case Type t when t == typeof(double):
                    componentType = EComponentType.Double;
                    break;
                case Type t when t == typeof(Vector2):
                    //componentType = EComponentType.Float;
                    componentCount = 2;
                    break;
                case Type t when t == typeof(Vector3):
                    //componentType = EComponentType.Float;
                    componentCount = 3;
                    break;
                case Type t when t == typeof(Vector4):
                    //componentType = EComponentType.Float;
                    componentCount = 4;
                    break;
                case Type t when t == typeof(Matrix4x4):
                    //componentType = EComponentType.Float;
                    componentCount = 16;
                    break;
                case Type t when t == typeof(Quaternion):
                    //componentType = EComponentType.Float;
                    componentCount = 4;
                    break;
                default:
                    throw new InvalidOperationException("Not a proper numeric data type.");
            }

            if (componentType != _componentType || componentCount != _componentCount || normalize != _normalize)
                throw new InvalidOperationException("Data type mismatch.");

            uint stride = ElementSize;
            array = new T[_elementCount];
            for (uint i = 0; i < _elementCount; ++i)
                array[i] = Marshal.PtrToStructure<T>(_source!.Address[i, stride]);
            
            if (!remap)
                return null;

            Remapper remapper = new();
            remapper.Remap(array);
            return remapper;
        }

        ~XRDataBuffer() { Dispose(false); }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool _disposedValue = false;
        protected void Dispose(bool disposing)
        {
            if (_disposedValue)
                return;

            //if (disposing)
            //    Destroy();

            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }

            //_vaoId = 0;
            _disposedValue = true;
        }

        public static implicit operator VoidPtr(XRDataBuffer b) => b.Address;
    }
}