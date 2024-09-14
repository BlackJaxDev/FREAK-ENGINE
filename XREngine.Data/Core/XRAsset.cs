using Extensions;
using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
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

        /// <summary>
        /// List of sub-assets contained in this file.
        /// </summary>
        public EventList<XRAsset> EmbeddedAssets { get; } = [];

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

        public static event Action<XRAsset>? AssetLoaded;
        public static event Action<XRAsset>? AssetSaved;

        private static void PostLoad<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string filePath, T? file) where T : XRAsset
        {
            if (file is null)
                return;

            file.FilePath = filePath;
            AssetLoaded?.Invoke(file);
        }

        public static async Task<T?> LoadAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string filePath) where T : XRAsset
        {
            T? file = !File.Exists(filePath)
                ? await Task.FromResult<T?>(null)
                : new Deserializer().Deserialize<T>(await File.ReadAllTextAsync(filePath));
            PostLoad(filePath, file);
            return file;
        }

        public static T? Load<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string filePath) where T : XRAsset
        {
            T? file = !File.Exists(filePath)
                ? null
                : new Deserializer().Deserialize<T>(File.ReadAllText(filePath));
            PostLoad(filePath, file);
            return file;
        }

        public async Task SaveAsync()
        {
            if (FilePath is null)
                throw new InvalidOperationException("Cannot save an asset without a file path.");

            await File.WriteAllTextAsync(FilePath, new Serializer().Serialize(this));
        }

        public void Save()
        {
            if (FilePath is null)
                throw new InvalidOperationException("Cannot save an asset without a file path.");

            File.WriteAllText(FilePath, new Serializer().Serialize(this));
        }

        public void SaveTo(string filePath)
        {
            File.WriteAllText(filePath, new Serializer().Serialize(this));
            FilePath = filePath;
        }

        public async Task SaveToAsync(string filePath)
        {
            await File.WriteAllTextAsync(filePath, new Serializer().Serialize(this));
            FilePath = filePath;
        }
    }
}
