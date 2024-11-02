using Microsoft.DotNet.PlatformAbstractions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using XREngine.Core.Files;
using XREngine.Data;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace XREngine
{
    public class AssetManager
    {
        public const string AssetExtension = "asset";

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

            VerifyDirectoryExists(GameAssetsPath);

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

        private static bool VerifyDirectoryExists(string? directoryPath)
        {
            //If the path is null or empty, return false
            if (string.IsNullOrWhiteSpace(directoryPath))
                return false;

            //If the path is a file, get the directory
            if (Path.HasExtension(directoryPath))
            {
                directoryPath = Path.GetDirectoryName(directoryPath);

                //If the directory is null or empty, return false
                if (string.IsNullOrWhiteSpace(directoryPath))
                    return false;
            }

            //If the current directory exists, return true
            if (Directory.Exists(directoryPath))
                return true;

            //Recursively create the parent directories
            string? parent = Path.GetDirectoryName(directoryPath);
            if (VerifyDirectoryExists(parent))
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

            if (!LoadedAssetsByPathInternal.TryGetValue(args.OldFullPath, out var asset))
                return;

            LoadedAssetsByPathInternal.Remove(args.OldFullPath, out _);
            LoadedAssetsByPathInternal.TryAdd(args.FullPath, asset);
        }

        private void CacheAsset(XRAsset asset)
        {
            string path = asset.FilePath ?? string.Empty;
            XRAsset UpdatePathDict(string existingPath, XRAsset existingAsset)
            {
                if (existingAsset is not null)
                {
                    if (existingAsset != asset && !existingAsset.EmbeddedAssets.Contains(asset))
                        existingAsset.EmbeddedAssets.Add(asset);
                    return existingAsset;
                }
                else
                    return asset;
            }
            LoadedAssetsByPathInternal.AddOrUpdate(path, asset, UpdatePathDict);

            if (asset.ID == Guid.Empty)
            {
                Debug.LogWarning("An asset was loaded with an empty ID.");
                return;
            }

            XRAsset UpdateIDDict(Guid existingID, XRAsset existingAsset)
            {
                Debug.Out($"An asset with the ID {existingID} already exists in the asset manager. The new asset will be added to the list of assets with the same ID.");
                return existingAsset;
            }
            LoadedAssetsByIDInternal.AddOrUpdate(asset.ID, asset, UpdateIDDict);
        }

        public FileSystemWatcher Watcher { get; } = new FileSystemWatcher();
        /// <summary>
        /// This is the path to /Build/CommonAssets/ in the root folder of the engine.
        /// </summary>
        public string EngineAssetsPath { get; }
        public string GameAssetsPath { get; set; } = Path.Combine(ApplicationEnvironment.ApplicationBasePath, "Assets");
        public string PackagesPath { get; set; } = Path.Combine(ApplicationEnvironment.ApplicationBasePath, "Packages");
        public ConcurrentDictionary<string, XRAsset> LoadedAssetsByPathInternal { get; } = [];
        public ConcurrentDictionary<Guid, XRAsset> LoadedAssetsByIDInternal { get; } = [];
        
        public XRAsset? GetAssetByID(Guid id)
            => LoadedAssetsByIDInternal.TryGetValue(id, out XRAsset? asset) ? asset : null;

        public bool TryGetAssetByID(Guid id, [NotNullWhen(true)] out XRAsset? asset)
            => LoadedAssetsByIDInternal.TryGetValue(id, out asset);

        public XRAsset? GetAssetByPath(string path)
            => LoadedAssetsByPathInternal.TryGetValue(path, out var asset) ? asset : null;

        public bool TryGetAssetByPath(string path, [NotNullWhen(true)] out XRAsset? asset)
            => LoadedAssetsByPathInternal.TryGetValue(path, out asset);

        public T LoadEngineAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset, new()
        {
            string path = ResolveEngineAssetPath(relativePathFolders);
            return Load<T>(path) ?? throw new FileNotFoundException($"Unable to find engine file at {path}");
        }
        public async Task<T> LoadEngineAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset, new()
        {
            string path = ResolveEngineAssetPath(relativePathFolders);
            return await LoadAsync<T>(path) ?? throw new FileNotFoundException($"Unable to find engine file at {path}");
        }

        public T? LoadGameAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset, new()
        {
            string path = ResolveGameAssetPath(relativePathFolders);
            return Load<T>(path);
        }

        public async Task<T?> LoadGameAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XRAsset, new()
        {
            string path = ResolveGameAssetPath(relativePathFolders);
            return await LoadAsync<T>(path);
        }

        //public async Task<T?> LoadEngine3rdPartyAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
        //    => await XR3rdPartyAsset.LoadAsync<T>(Path.Combine(EngineAssetsPath, Path.Combine(relativePathFolders)));

        //public async Task<T?> LoadGame3rdPartyAssetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
        //    => await XR3rdPartyAsset.LoadAsync<T>(Path.Combine(GameAssetsPath, Path.Combine(relativePathFolders)));

        //public T? LoadEngine3rdPartyAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
        //    => XR3rdPartyAsset.Load<T>(Path.Combine(EngineAssetsPath, Path.Combine(relativePathFolders)));

        //public T? LoadGame3rdPartyAsset<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params string[] relativePathFolders) where T : XR3rdPartyAsset, new()
        //    => XR3rdPartyAsset.Load<T>(Path.Combine(GameAssetsPath, Path.Combine(relativePathFolders)));

        /// <summary>
        /// Creates a full path to an asset in the engine's asset directory.
        /// </summary>
        /// <param name="relativePathFolders"></param>
        /// <returns></returns>
        public string ResolveEngineAssetPath(params string[] relativePathFolders)
            => Path.Combine(EngineAssetsPath, Path.Combine(relativePathFolders));

        /// <summary>
        /// Creates a full path to an asset in the game's asset directory.
        /// </summary>
        /// <param name="relativePathFolders"></param>
        /// <returns></returns>
        public string ResolveGameAssetPath(params string[] relativePathFolders)
            => Path.Combine(GameAssetsPath, Path.Combine(relativePathFolders));

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

            file.Name = Path.GetFileNameWithoutExtension(filePath);
            file.FilePath = filePath;

            CacheAsset(file);
            AssetLoaded?.Invoke(file);
        }

        public async Task<T?> LoadAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string filePath) where T : XRAsset, new()
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
                : await DeserializeAsync<T>(filePath);
            PostLoaded(filePath, file);
#if !DEBUG
            }
            catch (Exception e)
            {
                Debug.LogException(e, $"An error occurred while loading the asset at '{filePath}'.");
                return null;
            }
#endif
            return file;
        }

        public T? Load<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string filePath) where T : XRAsset, new()
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
                : Deserialize<T>(filePath);
            PostLoaded(filePath, file);
#if !DEBUG
            }
            catch (Exception e)
            {
                Debug.LogException(e, $"An error occurred while loading the asset at '{filePath}'.");
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
            await File.WriteAllTextAsync(asset.FilePath, Serializer.Serialize(asset));
            PostSaved(asset, false);
#if !DEBUG
            }
            catch (Exception e)
            {
                Debug.LogException(e, $"An error occurred while saving the asset at '{asset.FilePath}'.");
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
            File.WriteAllText(asset.FilePath, Serializer.Serialize(asset));
            PostSaved(asset, false);
#if !DEBUG
            }
            catch (Exception e)
            {
                Debug.LogException(e, $"An error occurred while saving the asset at '{asset.FilePath}'.");
            }
#endif
        }

        public static string VerifyAssetPath(XRAsset asset, string directory)
        {
            VerifyDirectoryExists(directory);
            string name = string.IsNullOrWhiteSpace(asset.Name) ? asset.GetType().Name : asset.Name;
            string fileName = $"{name}.{AssetExtension}";
            string path = Path.Combine(directory, fileName);
            return path;
        }

        public void SaveTo(XRAsset asset, Environment.SpecialFolder folder, params string[] folderNames)
            => SaveTo(asset, Path.Combine([Environment.GetFolderPath(folder), ..folderNames]));
        public void SaveTo(XRAsset asset, string directory)
        {
#if !DEBUG
            try
            {
#endif
            string path = VerifyAssetPath(asset, directory);

            if (!(AllowOverwriteCallback?.Invoke(path) ?? true))
                path = GetUniqueAssetPath(path);

            File.WriteAllText(path, Serializer.Serialize(asset));
            asset.FilePath = path;
            PostSaved(asset, true);
#if !DEBUG
            }
            catch (Exception e)
            {
                Debug.LogException(e, $"An error occurred while saving the asset to '{directory}'.");
            }
#endif
        }

        public Func<string, bool>? AllowOverwriteCallback { get; set; } = path => true;

        public static string GetUniqueAssetPath(string path)
        {
            if (!File.Exists(path))
                return path;

            string? dir = Path.GetDirectoryName(path);
            if (dir is null)
                return path;

            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            int i = 1;
            string newPath;
            do
            {
                newPath = Path.Combine(dir, $"{name} ({i++}){ext}");
            } while (File.Exists(newPath));
            return newPath;
        }

        public Task SaveToAsync(XRAsset asset, Environment.SpecialFolder folder, params string[] folderNames)
            => SaveToAsync(asset, Path.Combine([Environment.GetFolderPath(folder), .. folderNames]));
        public async Task SaveToAsync(XRAsset asset, string directory)
        {
#if !DEBUG
            try
            {
#endif
            string path = VerifyAssetPath(asset, directory);
            await File.WriteAllTextAsync(path, Serializer.Serialize(asset));
            asset.FilePath = path;
            CacheAsset(asset);
            PostSaved(asset, true);
#if !DEBUG
            }
            catch (Exception e)
            {
                Debug.LogException(e, $"An error occurred while saving the asset to '{directory}'.");
            }
#endif
        }

        public Task SaveGameAssetToAsync(XRAsset asset, params string[] folderNames)
            => SaveToAsync(asset, Path.Combine(GameAssetsPath, Path.Combine(folderNames)));
        public void SaveGameAssetTo(XRAsset asset, params string[] folderNames)
            => SaveTo(asset, Path.Combine(GameAssetsPath, Path.Combine(folderNames)));

        public static readonly ISerializer Serializer = new SerializerBuilder()
            .WithEventEmitter(nextEmitter => new DepthTrackingEventEmitter(nextEmitter))
            //.WithTypeConverter(new XRAssetYamlConverter())
            .WithTypeConverter(new DataSourceYamlTypeConverter())
            .Build();

        public static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNodeDeserializer(
                inner => new DepthTrackingNodeDeserializer(inner),
                s => s.InsteadOf<ObjectNodeDeserializer>())
            //.WithNodeDeserializer(new XRAssetDeserializer(), w => w.OnTop())
            .Build();

        private static T? Deserialize<T>(string filePath) where T : XRAsset, new()
        {
            string ext = Path.GetExtension(filePath)[1..].ToLowerInvariant();
            if (ext == AssetExtension)
                return Deserializer.Deserialize<T>(File.ReadAllText(filePath));
            else
            {
                var extensions3rdParty = typeof(T).GetCustomAttribute<XR3rdPartyExtensionsAttribute>()?.Extensions;
                if (extensions3rdParty?.Contains(ext) ?? false)
                {
                    var asset = new T();
                    asset.Load3rdParty(filePath);
                    return asset;
                }
                else
                {
                    Debug.LogWarning($"The file extension '{ext}' is not supported by the asset type '{typeof(T).Name}'.");
                    return null;
                }
            }
        }

        private static async Task<T?> DeserializeAsync<T>(string filePath) where T : XRAsset, new()
        {
            string ext = Path.GetExtension(filePath)[1..].ToLowerInvariant();
            if (ext == AssetExtension)
                return await Task.Run(async () => Deserializer.Deserialize<T>(await File.ReadAllTextAsync(filePath)));
            else
            {
                var exts = typeof(T).GetCustomAttribute<XR3rdPartyExtensionsAttribute>()?.Extensions;
                if (exts?.Contains(ext) ?? false)
                {
                    var asset = new T();
                    await asset.Load3rdPartyAsync(filePath);
                    return asset;
                }
                else
                {
                    Debug.LogWarning($"The file extension '{ext}' is not supported by the asset type '{typeof(T).Name}'.");
                    return null;
                }
            }
        }
    }
}
