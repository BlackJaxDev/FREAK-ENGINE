using System.Diagnostics;
using System.Runtime.InteropServices;
using XREngine.Data.Core;
using YamlDotNet.Serialization;

namespace XREngine.Data
{
    //Stores a reference to unmanaged data
    public class DataSource : XRBase, IDisposable
    {
        /// <summary>
        /// If true, this data source references memory that was allocated somewhere else.
        /// </summary>
        public bool External { get; }
        public uint Length { get; set; }
        [YamlIgnore]
        public VoidPtr Address { get; set; }

        public static unsafe DataSource FromArray<T>(T[] data) where T : unmanaged
        {
            DataSource source = new((uint)(data.Length * sizeof(T)));
            fixed (void* ptr = data)
                Memory.Move(source.Address, ptr, source.Length);
            return source;
        }
        public DataSource(byte[] data)
        {
            External = false;
            Length = (uint)data.Length;
            Address = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, Address, data.Length);
        }
        public DataSource(VoidPtr address, uint length, bool copyInternal = false)
        {
            Length = length;
            if (copyInternal)
            {
                Address = Marshal.AllocHGlobal((int)Length);
                Memory.Move(Address, address, length);
                External = false;
            }
            else
            {
                Address = address;
                External = true;
            }
        }

        public DataSource(uint length, bool zeroMemory = false)
        {
            Length = length;
            Address = Marshal.AllocHGlobal((int)Length);
            if (zeroMemory)
                Memory.Fill(Address, (uint)Length, 0);
            External = false;
        }

        public static DataSource Allocate(uint size, bool zeroMemory = false)
            => new(size, zeroMemory);

        public unsafe UnmanagedMemoryStream AsStream()
            => new((byte*)Address, Length);

        #region IDisposable Support
        private bool _disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                try
                {
                    if (!External && Address != null)
                    {
                        Marshal.FreeHGlobal(Address);
                        Address = null;
                        Length = 0;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }

                _disposedValue = true;
            }
        }

        ~DataSource()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[Length];
            Marshal.Copy(Address, bytes, 0, (int)Length);
            return bytes;
        }

        public DataSource Clone()
        {
            if (External)
                return new DataSource(Address, Length, false);

            DataSource clone = new(Length);
            Memory.Move(clone.Address, Address, Length);
            return clone;
        }

        #endregion
    }
}
