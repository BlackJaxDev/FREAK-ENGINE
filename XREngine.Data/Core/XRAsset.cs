using Extensions;
using System.IO.MemoryMappedFiles;
using XREngine.Data.Core;

namespace XREngine.Core.Files
{
    public abstract partial class XRAsset : XRBase
    {
        public XRAsset() { }
        public XRAsset(string name) => Name = name;

        public string Name { get; set; } = string.Empty;
        public Guid Guid { get; set; } = Guid.NewGuid();
        public bool Initialized { get; set; } = false;

        /// <summary>
        /// List of sub-assets contained in this file.
        /// </summary>
        public List<XRAsset> EmbeddedAssets { get; } = [];
        
        private string? _filePath;
        /// <summary>
        /// The absolute origin of this asset in the file system.
        /// </summary>
        public string? FilePath
        {
            get
            {
                if (_sourceAsset is null || _sourceAsset == this)
                    return _filePath;

                return _sourceAsset.FilePath;
            }
            set => _filePath = (value?.IsValidPath() ?? false) ? Path.GetFullPath(value) : value;
        }

        private XRAsset? _sourceAsset = null;
        /// <summary>
        /// The root asset that this asset resides inside of.
        /// The root asset is the one actually written as a file instead of being included in another asset.
        /// </summary>
        public XRAsset SourceAsset
        {
            get => _sourceAsset ?? this;
            set => _sourceAsset = value;
        }
        
        /// <summary>
        /// The map of the asset in memory for unsafe pointer use.
        /// </summary>
        private MemoryMappedFile? FileMap { get; set; }
        /// <summary>
        /// A stream to the file for sequential reading and writing.
        /// </summary>
        public MemoryMappedViewStream? FileMapStream { get; private set; }

        public void OpenForStreaming()
        {
            if (FilePath is null)
                throw new InvalidOperationException("Cannot open a file for streaming without a file path.");

            CloseStreaming();

            FileMap = MemoryMappedFile.CreateFromFile(FilePath);
            FileMapStream = FileMap.CreateViewStream();
        }
        public void CloseStreaming()
        {
            FileMapStream?.Dispose();
            FileMapStream = null;

            FileMap?.Dispose();
            FileMap = null;
        }

        public static Task<T?> LoadAsync<T>(string filePath)
        {
            //TODO
            return null;
        }
    }
}
