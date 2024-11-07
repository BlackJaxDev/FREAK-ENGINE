using Extensions;
using System.IO.MemoryMappedFiles;
using System.Text.Json.Serialization;
using XREngine.Data.Core;
using YamlDotNet.Serialization;

namespace XREngine.Core.Files
{
    /// <summary>
    /// An asset is a common base class for all engine-formatted objects that are loaded from disk.
    /// </summary>
    public abstract partial class XRAsset : XRObjectBase
    {
        public XRAsset() { }
        public XRAsset(string name) => Name = name;

        private EventList<XRAsset> _embeddedAssets = [];
        /// <summary>
        /// List of sub-assets contained in this file.
        /// </summary>
        public EventList<XRAsset> EmbeddedAssets
        {
            get => _embeddedAssets; 
            internal set => SetField(ref _embeddedAssets, value);
        }

        private string? _originalPath;
        /// <summary>
        /// The original path of this asset before it was imported and converted for engine use.
        /// </summary>
        public string? OriginalPath
        {
            get => _originalPath;
            set => SetField(ref _originalPath, value);
        }

        private string? _filePath;
        /// <summary>
        /// The absolute origin of this asset in the file system.
        /// </summary>
        public string? FilePath
        {
            get => _sourceAsset is null || _sourceAsset == this ? _filePath : _sourceAsset.FilePath;
            set => SetField(ref _filePath, (value?.IsValidPath() ?? false) ? Path.GetFullPath(value) : value);
        }

        private XRAsset? _sourceAsset = null;
        /// <summary>
        /// The root asset that this asset resides inside of.
        /// The root asset is the one actually written as a file instead of being included in another asset.
        /// </summary>
        public XRAsset SourceAsset
        {
            get => _sourceAsset ?? this;
            set => SetField(ref _sourceAsset, value);
        }

        /// <summary>
        /// The map of the asset in memory for unsafe pointer use.
        /// </summary>
        [JsonIgnore]
        [YamlIgnore]
        private MemoryMappedFile? FileMap { get; set; }
        /// <summary>
        /// A stream to the file for sequential reading and writing.
        /// </summary>
        [JsonIgnore]
        [YamlIgnore]
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

        /// <summary>
        /// Called when the filePath has an extension that is in the XR3rdPartyExtensionsAttribute list.
        /// </summary>
        /// <param name="filePath"></param>
        public virtual void Load3rdParty(string filePath)
        {

        }

        public virtual async Task Load3rdPartyAsync(string filePath)
        {
            //Run the synchronous version of the method async by default
            await Task.Run(() => Load3rdParty(filePath));
        }
    }
}
