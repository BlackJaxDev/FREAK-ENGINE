using Microsoft.DotNet.PlatformAbstractions;
using System.Diagnostics.CodeAnalysis;
using XREngine.Core.Files;
using XREngine.Data;
using YamlDotNet.Serialization;

namespace XREngine
{
    public class AssetManager
    {
        public AssetManager(string? engineAssetsDirPath = null)
        {
            if (!string.IsNullOrWhiteSpace(engineAssetsDirPath) && Directory.Exists(engineAssetsDirPath))
                EngineAssetsPath = engineAssetsDirPath;
            else
            {
                string? basePath = ApplicationEnvironment.ApplicationBasePath;
                //Iterate up the directory tree until we find the Build directory
                while (basePath is not null && !Directory.Exists(Path.Combine(basePath, "Build")))
                    basePath = Path.GetDirectoryName(basePath);
                if (basePath is null)
                    throw new DirectoryNotFoundException("Could not find the Build directory in the application path.");
                string buildDirectory = Path.Combine(basePath, "Build");
                EngineAssetsPath = Path.Combine(buildDirectory, "CommonAssets");
            }

            VerifyPathExists(GameAssetsPath);

            Watcher.Path = GameAssetsPath;
            Watcher.Filter = "*.*";
            Watcher.IncludeSubdirectories = true;
            Watcher.EnableRaisingEvents = true;
            Watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            Watcher.Created += FileCreated;
            Watcher.Changed += FileChanged;
            Watcher.Deleted += FileDeleted;
            Watcher.Error += FileError;
            Watcher.Renamed += FileRenamed;
        }

        private bool VerifyPathExists(string? directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                return false;

            if (Directory.Exists(directoryPath))
                return true;

            string? parent = Path.GetDirectoryName(directoryPath);

            if (VerifyPathExists(parent))
                Directory.CreateDirectory(directoryPath);

            return true;

        }

        void FileCreated(object sender, FileSystemEventArgs args)
        {
            Debug.Out($"File '{args.FullPath}' was created.");
        }
        void FileChanged(object sender, FileSystemEventArgs args)
        {
            Debug.Out($"File '{args.FullPath}' was changed.");
        }
        void FileDeleted(object sender, FileSystemEventArgs args)
        {
            Debug.Out($"File '{args.FullPath}' was deleted.");
            //Leave files intact
            //if (LoadedAssetsByPathInternal.TryGetValue(args.FullPath, out var list))
            //{
            //    foreach (var asset in list)
            //        asset.Destroy();
            //    LoadedAssetsByPathInternal.Remove(args.FullPath);
            //}
        }
        void FileError(object sender, ErrorEventArgs args)
        {
            Debug.LogWarning($"An error occurred in the file system watcher: {args.GetException().Message}");
        }
        void FileRenamed(object sender, RenamedEventArgs args)
        {
            Debug.Out($"File '{args.OldFullPath}' was renamed to '{args.FullPath}'.");

            if (!LoadedAssetsByPathInternal.TryGetValue(args.OldFullPath, out var list))
                return;
            
            LoadedAssetsByPathInternal.Remove(args.OldFullPath);
            LoadedAssetsByPathInternal.Add(args.FullPath, list);
        }

        private void CacheAsset(XRAsset asset)
        {
            string path = asset.FilePath ?? string.Empty;
            if (LoadedAssetsByPathInternal.TryGetValue(path, out var existingAsset))
            {
                if (existingAsset is not null)
                {
                    if (existingAsset == asset || existingAsset.EmbeddedAssets.Contains(asset))
                        return;

                    existingAsset.EmbeddedAssets.Add(asset);
                }
                else
                    LoadedAssetsByPathInternal[path] = asset;
            }
            else
                LoadedAssetsByPathInternal.Add(path, asset);

            if (asset.ID == Guid.Empty)
            {
                Debug.LogWarning("An asset was loaded with an empty ID.");
                return;
            }

            if (!LoadedAssetsByIDInternal.TryGetValue(asset.ID, out XRAsset? existingAssetByID))
                LoadedAssetsByIDInternal.Add(asset.ID, asset);
            else
            {
                if (existingAssetByID == asset)
                    return;
                
                Debug.LogWarning($"An asset with the ID {asset.ID} already exists in the asset manager. The new asset will be added to the list of assets with the same ID.");
                LoadedAssetsByIDInternal[asset.ID] = asset;
            }
        }

        public FileSystemWatcher Watcher { get; } = new FileSystemWatcher();
        public string EngineAssetsPath { get; }
        public string GameAssetsPath { get; set; } = Path.Combine(ApplicationEnvironment.ApplicationBasePath, "Assets");
        public string PackagesPath { get; set; } = Path.Combine(ApplicationEnvironment.ApplicationBasePath, "Packages");
        public EventDictionary<string, XRAsset> LoadedAssetsByPathInternal { get; } = [];
        public EventDictionary<Guid, XRAsset> LoadedAssetsByIDInternal { get; } = [];

        public XRAsset? GetAssetByID(Guid id)
            => LoadedAssetsByIDInternal.TryGetValue(id, out XRAsset? asset) ? asset : null;

        public bool TryGetAssetByID(Guid id, [NotNullWhen(true)] out XRAsset? asset)
            => LoadedAssetsByIDInternal.TryGetValue(id, out asset);

        public XRAsset? GetAssetByPath(string path)
            => LoadedAssetsByPathInternal.TryGetValue(path, out var asset) ? asset : null;

        public bool TryGetAssetByPath(string path, [NotNullWhen(true)] out XRAsset? asset)
            => LoadedAssetsByPathInternal.TryGetValue(path, out asset);

        public async Task<T?> LoadEngineAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset
            => await LoadAsync<T>(Path.Combine(EngineAssetsPath, Path.Combine(relativePathFolders)));

        public async Task<T?> LoadGameAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset
            => await LoadAsync<T>(Path.Combine(GameAssetsPath, Path.Combine(relativePathFolders)));

        public T? LoadEngineAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset
            => Load<T>(Path.Combine(EngineAssetsPath, Path.Combine(relativePathFolders)));

        public T? LoadGameAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset
            => Load<T>(Path.Combine(GameAssetsPath, Path.Combine(relativePathFolders)));

        public async Task<T?> LoadEngine3rdPartyAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
            => await XR3rdPartyAsset.LoadAsync<T>(Path.Combine(EngineAssetsPath, Path.Combine(relativePathFolders)));

        public async Task<T?> LoadGame3rdPartyAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
            => await XR3rdPartyAsset.LoadAsync<T>(Path.Combine(GameAssetsPath, Path.Combine(relativePathFolders)));

        public T? LoadEngine3rdPartyAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
            => XR3rdPartyAsset.Load<T>(Path.Combine(EngineAssetsPath, Path.Combine(relativePathFolders)));

        public T? LoadGame3rdPartyAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
            => XR3rdPartyAsset.Load<T>(Path.Combine(GameAssetsPath, Path.Combine(relativePathFolders)));

        public void Dispose()
        {
            foreach (var asset in LoadedAssetsByIDInternal.Values)
                asset.Destroy();
            LoadedAssetsByIDInternal.Clear();
            LoadedAssetsByPathInternal.Clear();
        }

        public event Action<XRAsset>? AssetLoaded;
        public event Action<XRAsset>? AssetSaved;

        private void PostSaved(XRAsset asset, bool newAsset)
        {
            if (newAsset)
                CacheAsset(asset);
            AssetSaved?.Invoke(asset);
        }

        private void PostLoaded<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string filePath, T? file) where T : XRAsset
        {
            if (file is null)
                return;

            file.FilePath = filePath;
            CacheAsset(file);
            AssetLoaded?.Invoke(file);
        }

        public async Task<T?> LoadAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string filePath) where T : XRAsset
        {
            T? file;
#if !DEBUG
            try
            {
#endif
            if (TryGetAssetByPath(filePath, out XRAsset? existingAsset))
                return existingAsset is T tAsset ? tAsset : null;

            file = !File.Exists(filePath)
                ? null
                : Deserializer.Deserialize<T>(await File.ReadAllTextAsync(filePath));
            PostLoaded(filePath, file);
#if !DEBUG
            }
            catch (Exception e)
            {
                return null;
            }
#endif
            return file;
        }

        public T? Load<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string filePath) where T : XRAsset
        {
            T? file;
#if !DEBUG
            try
            {
#endif
            if (TryGetAssetByPath(filePath, out XRAsset? existingAsset))
                return existingAsset is T tAsset ? tAsset : null;

            file = !File.Exists(filePath)
                ? null
                : Deserializer.Deserialize<T>(File.ReadAllText(filePath));
            PostLoaded(filePath, file);
#if !DEBUG
            }
            catch (Exception e)
            {
                return null;
            }
#endif
            return file;
        }

        public async Task SaveAsync(XRAsset asset)
        {
            if (asset.FilePath is null)
            {
                Debug.LogWarning("Cannot save an asset without a file path.");
                return;
            }    
#if !DEBUG
            try
            {
#endif
            await File.WriteAllTextAsync(asset.FilePath, Serializer.Serialize(this));
            PostSaved(asset, false);
#if !DEBUG
            }
            catch (Exception e)
            {

            }
#endif
        }

        public void Save(XRAsset asset)
        {
            if (asset.FilePath is null)
            {
                Debug.LogWarning("Cannot save an asset without a file path.");
                return;
            }
#if !DEBUG
            try
            {
#endif
            File.WriteAllText(asset.FilePath, Serializer.Serialize(this));
            PostSaved(asset, false);
#if !DEBUG
            }
            catch (Exception e)
            {

            }
#endif
        }

        public void SaveTo(XRAsset asset, string directory)
        {
#if !DEBUG
            try
            {
#endif
            string path = Path.Combine(directory, $"{(string.IsNullOrWhiteSpace(asset.Name) ? GetType().Name : asset.Name)}.asset");
            File.WriteAllText(path, Serializer.Serialize(this));
            asset.FilePath = path;
            PostSaved(asset, true);
#if !DEBUG
            }
            catch (Exception e)
            {

            }
#endif
        }

        public async Task SaveToAsync(XRAsset asset, string directory)
        {
#if !DEBUG
            try
            {
#endif
            string path = Path.Combine(directory, $"{(string.IsNullOrWhiteSpace(asset.Name) ? GetType().Name : asset.Name)}.asset");
            await File.WriteAllTextAsync(path, Serializer.Serialize(this));
            asset.FilePath = path;
            CacheAsset(asset);
            PostSaved(asset, true);
#if !DEBUG
            }
            catch (Exception e)
            {

            }
#endif
        }

        private static readonly ISerializer Serializer =
            new SerializerBuilder()
            .WithTypeConverter(new DataSourceYamlTypeConverter())
            .WithTypeConverter(new XRAssetYamlTypeConverter())
            .Build();
        private static readonly IDeserializer Deserializer =
            new DeserializerBuilder()
            .WithTypeConverter(new DataSourceYamlTypeConverter())
            .WithTypeConverter(new XRAssetYamlTypeConverter())
            .Build();
    }
}
