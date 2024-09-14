using Microsoft.DotNet.PlatformAbstractions;
using System.Diagnostics.CodeAnalysis;
using XREngine.Core.Files;

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

            XRAsset.AssetLoaded += CacheAsset;
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
            if (!LoadedAssetsByPathInternal.TryGetValue(path, out var list))
                LoadedAssetsByPathInternal.Add(path, list = []);

            if (!list.Contains(asset))
                list.Add(asset);

            if (asset.ID == Guid.Empty)
                return;

            if (!LoadedAssetsByIDInternal.TryGetValue(asset.ID, out XRAsset? existingAsset))
                LoadedAssetsByIDInternal.Add(asset.ID, asset);
            else
            {
                if (existingAsset == asset)
                    return;
                
                Debug.LogWarning($"An asset with the ID {asset.ID} already exists in the asset manager. The new asset will be added to the list of assets with the same ID.");
                LoadedAssetsByIDInternal[asset.ID] = asset;
            }
        }

        public FileSystemWatcher Watcher { get; } = new FileSystemWatcher();
        public string EngineAssetsPath { get; }
        public string GameAssetsPath { get; set; } = Path.Combine(ApplicationEnvironment.ApplicationBasePath, "Assets");
        public string PackagesPath { get; set; } = Path.Combine(ApplicationEnvironment.ApplicationBasePath, "Packages");
        public EventDictionary<string, List<XRAsset>> LoadedAssetsByPathInternal { get; } = [];
        public EventDictionary<Guid, XRAsset> LoadedAssetsByIDInternal { get; } = [];

        public XRAsset GetAssetByID(Guid id)
            => LoadedAssetsByIDInternal.TryGetValue(id, out XRAsset? asset)
                ? asset
                : throw new KeyNotFoundException($"Could not find an asset with the ID {id}.");

        public IReadOnlyList<XRAsset> GetAssetsByPath(string path)
            => LoadedAssetsByPathInternal.TryGetValue(path, out var list)
                ? (IReadOnlyList<XRAsset>)list
                : throw new KeyNotFoundException($"Could not find any assets at the path {path}.");

        public async Task<T?> LoadEngineAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset
        {
            string path = Path.Combine(EngineAssetsPath, Path.Combine(relativePathFolders));
            return !File.Exists(path)
                ? throw new FileNotFoundException($"Could not find the file at {path}.")
                : await XRAsset.LoadAsync<T>(path);
        }

        public async Task<T?> LoadGameAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset
        {
            string path = Path.Combine(GameAssetsPath, Path.Combine(relativePathFolders));
            return !File.Exists(path)
                ? throw new FileNotFoundException($"Could not find the file at {path}.")
                : await XRAsset.LoadAsync<T>(path);
        }

        public T? LoadEngineAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset
        {
            string path = Path.Combine(EngineAssetsPath, Path.Combine(relativePathFolders));
            return !File.Exists(path)
                ? throw new FileNotFoundException($"Could not find the file at {path}.")
                : XRAsset.Load<T>(path);
        }

        public T? LoadGameAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset
        {
            string path = Path.Combine(GameAssetsPath, Path.Combine(relativePathFolders));
            return !File.Exists(path)
                ? throw new FileNotFoundException($"Could not find the file at {path}.")
                : XRAsset.Load<T>(path);
        }


        public async Task<T?> LoadEngine3rdPartyAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
        {
            string path = Path.Combine(EngineAssetsPath, Path.Combine(relativePathFolders));
            return !File.Exists(path)
                ? throw new FileNotFoundException($"Could not find the file at {path}.")
                : await XR3rdPartyAsset.LoadAsync<T>(path);
        }

        public async Task<T?> LoadGame3rdPartyAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
        {
            string path = Path.Combine(GameAssetsPath, Path.Combine(relativePathFolders));
            return !File.Exists(path)
                ? throw new FileNotFoundException($"Could not find the file at {path}.")
                : await XR3rdPartyAsset.LoadAsync<T>(path);
        }

        public T? LoadEngine3rdPartyAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
        {
            string path = Path.Combine(EngineAssetsPath, Path.Combine(relativePathFolders));
            return !File.Exists(path)
                ? throw new FileNotFoundException($"Could not find the file at {path}.")
                : XR3rdPartyAsset.Load<T>(path);
        }

        public T? LoadGame3rdPartyAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
        {
            string path = Path.Combine(GameAssetsPath, Path.Combine(relativePathFolders));
            return !File.Exists(path)
                ? throw new FileNotFoundException($"Could not find the file at {path}.")
                : XR3rdPartyAsset.Load<T>(path);
        }

        public void Dispose()
        {
            XRAsset.AssetLoaded -= CacheAsset;
            foreach (var asset in LoadedAssetsByIDInternal.Values)
                asset.Destroy();
            LoadedAssetsByIDInternal.Clear();
            LoadedAssetsByPathInternal.Clear();
        }
    }
}
