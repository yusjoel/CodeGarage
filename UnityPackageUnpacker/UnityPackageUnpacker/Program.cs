using CommandLine;
using SharpCompress.Readers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityPackageUnpacker
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string packagePath = args[0];
            string extractDirectory = args[1];

            if (!File.Exists(packagePath))
                return;

            var map = new Dictionary<string, AssetInfo>();
            var memoryStream = new MemoryStream(1024 * 1024 * 16);

            using (Stream stream = File.OpenRead(packagePath))
            {
                var reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry())
                {
                    var entry = reader.Entry;
                    if (entry.IsDirectory)
                    {
                        string assetId = entry.Key;
                        if (assetId.EndsWith("/"))
                            assetId = assetId.Substring(0, assetId.Length - 1);
                        map[assetId] = new AssetInfo
                        {
                            AssetId = assetId
                        };
                    }
                    else
                    {
                        if (entry.Key == ".icon.png")
                            continue;

                        string assetId = Path.GetDirectoryName(entry.Key);
                        if(string.IsNullOrEmpty(assetId))
                            continue;

                        string fileName = Path.GetFileName(entry.Key);
                        reader.WriteEntryTo(memoryStream);

                        int count = (int) memoryStream.Position;
                        var bytes = new byte[count];
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        memoryStream.Read(bytes, 0, count);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        var assetInfo = map[assetId];
                        if (fileName == "asset")
                        {
                            assetInfo.Asset = bytes;
                        }
                        else if (fileName == "asset.meta")
                        {
                            assetInfo.AssetMeta = bytes;
                        }
                        else if (fileName == "pathname")
                        {
                            string allLines = Encoding.UTF8.GetString(bytes);
                            assetInfo.Path = allLines.Split('\n')[0];
                        }
                    }
                }
            }

            foreach (var assetInfo in map.Values)
            {
                string assetPath = Path.Combine(extractDirectory, assetInfo.Path);
                string directoryName = Path.GetDirectoryName(assetPath);
                if(string.IsNullOrEmpty(directoryName))
                    continue;

                if (!Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                if (assetInfo.Asset != null)
                    File.WriteAllBytes(assetPath, assetInfo.Asset);
                string metaPath = assetPath + ".meta";
                if (assetInfo.AssetMeta != null)
                    File.WriteAllBytes(metaPath, assetInfo.AssetMeta);
            }
        }

        private class AssetInfo
        {
            public byte[] Asset;

            // ReSharper disable once NotAccessedField.Local
            public string AssetId;

            public byte[] AssetMeta;

            public string Path;
        }

        public class Options
        {
            [Value(0)]
            public string UnityPackagePath { get; set; }

            [Value(1, Required = false)]
            public string ExtractDirectory { get; set; }
        }
    }
}
