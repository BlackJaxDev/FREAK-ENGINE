using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System.Text;

namespace XREngine
{
    public static class UnityPackageExtractor
    {
        public static void Extract(string packagePath, string destinationFolderPath, bool overwrite)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), $"{Path.GetFileName(packagePath)}.unitypackage.extract");
            try
            {
                using (var i = new FileStream(packagePath, FileMode.Open, FileAccess.Read))
                using (var gi = new GZipInputStream(i))
                using (var tar = TarArchive.CreateInputTarArchive(gi, Encoding.Default))
                {
                    tar.ExtractContents(tempFolder);
                }

                foreach (var item in Directory.GetDirectories(tempFolder))
                {
                    var pathname = File.ReadAllText(Path.Combine(item, "pathname"));
                    var path = Path.Combine(destinationFolderPath, pathname);
                    var assetPath = Path.Combine(item, "asset");

                    if (File.Exists(assetPath))
                    {
                        var folder = Path.GetDirectoryName(Path.GetFullPath(path));
                        if (folder is not null)
                        {
                            if (!Directory.Exists(folder!))
                                Directory.CreateDirectory(folder!);

                            if (overwrite || !File.Exists(path))
                                File.Copy(assetPath, path, true);
                        }
                    }
                    else if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    
                    var assetMetaPath = Path.Combine(item, "asset.meta");
                    var metaPath = $"{path}.meta";

                    if (overwrite || !File.Exists(metaPath))
                        File.Copy(assetMetaPath, metaPath, true);
                }
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
        }

        public static async Task ExtractAsync(string packagePath, string destinationFolderPath, bool overwrite)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), $"{Path.GetFileName(packagePath)}.unitypackage.extract");
            try
            {
                void Extract()
                {
                    using var i = new FileStream(packagePath, FileMode.Open, FileAccess.Read);
                    using var gi = new GZipInputStream(i);
                    using var tar = TarArchive.CreateInputTarArchive(gi, Encoding.Default);
                    tar.ExtractContents(tempFolder);
                }
                void Copy()
                {
                    foreach (var item in Directory.GetDirectories(tempFolder))
                    {
                        var pathname = File.ReadAllText(Path.Combine(item, "pathname"));
                        var path = Path.Combine(destinationFolderPath, pathname);
                        var assetPath = Path.Combine(item, "asset");

                        if (File.Exists(assetPath))
                        {
                            var folder = Path.GetDirectoryName(Path.GetFullPath(path));
                            if (folder is not null)
                            {
                                if (!Directory.Exists(folder!))
                                    Directory.CreateDirectory(folder!);

                                if (overwrite || !File.Exists(path))
                                    File.Copy(assetPath, path, true);
                            }
                        }
                        else if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        var assetMetaPath = Path.Combine(item, "asset.meta");
                        var metaPath = $"{path}.meta";

                        if (overwrite || !File.Exists(metaPath))
                            File.Copy(assetMetaPath, metaPath, true);
                    }
                }
                await Task.Run(Extract);
                await Task.Run(Copy);
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
        }
    }
    public static class UnityPackageImporter
    {
        public static void Import(string packagePath)
        {
            string destinationFolderPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                UnityPackageExtractor.Extract(packagePath, destinationFolderPath, true);

            }
            finally
            {
                Directory.Delete(destinationFolderPath, true);
            }
        }
    }
}
