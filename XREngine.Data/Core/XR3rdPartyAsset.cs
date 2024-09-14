using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using XREngine.Data.Core;

namespace XREngine.Core.Files
{
    public abstract class XR3rdPartyAsset : XRBase
    {
        public XR3rdPartyAsset() { }

        public static event Action? AssetLoaded;
        public static event Action? AssetSaved;

        protected void OnAssetLoaded()
            => AssetLoaded?.Invoke();
        protected void OnAssetSaved()
            => AssetSaved?.Invoke();

        public string? FilePath { get; set; }
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

        private MemoryMappedFile? FileMap { get; set; }
        public MemoryMappedViewStream? FileMapStream { get; private set; }

        public abstract void Load(string filePath);
        public abstract Task LoadAsync(string filePath);

        public abstract void Save();
        public void SaveTo(string filePath)
        {
            FilePath = filePath;
            Save();
        }

        public abstract Task SaveAsync();
        public async Task SaveToAsync(string filePath)
        {
            FilePath = filePath;
            await SaveAsync();
        }

        public static async Task<T?> LoadAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string path) where T : XR3rdPartyAsset, new()
        {
            if (!File.Exists(path))
            {
                Debug.WriteLine($"Could not find the file at {path}.");
                return null;
            }

            T asset = new();
            await asset.LoadAsync(path);
            return asset;
        }

        public static T? Load<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string path) where T : XR3rdPartyAsset, new()
        {
            if (!File.Exists(path))
            {
                Debug.WriteLine($"Could not find the file at {path}.");
                return null;
            }

            T asset = new();
            asset.Load(path);
            return asset;
        }
    }
}
