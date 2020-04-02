using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MiniJSON;

namespace UnityPackageRenamer
{
    internal class Program
    {
        private static object GetValue(Dictionary<string, object> dictionary, string key)
        {
            if (dictionary == null) return null;

            dictionary.TryGetValue(key, out var value);
            return value;
        }

        private static void Main(string[] args)
        {
            if (args == null || args.Length < 2)
                return;

            string packagePath = args[0];
            string destDir = args[1];

            if (!File.Exists(packagePath))
            {
                Console.WriteLine("Error: not found package path: " + packagePath);
                return;
            }

            if (!Directory.Exists(destDir))
            {
                Console.WriteLine("Warning: not found destination directory: " + destDir);
                Directory.CreateDirectory(destDir);
            }

            string destFileName;
            using (var br = new BinaryReader(File.OpenRead(packagePath)))
            {
                byte id1 = br.ReadByte();
                byte id2 = br.ReadByte();
                if (id1 != 0x1f && id2 != 0x8b)
                {
                    Console.WriteLine("Not Gzip format");
                    return;
                }

                // compressionMethod
                br.ReadByte();
                byte flags = br.ReadByte();
                bool hasExtraField = (flags & 0x04) == 0x04;

                // modificationTime
                br.ReadInt32();
                // extraFlags
                br.ReadByte();
                // operatingSystem
                br.ReadByte();

                if (!hasExtraField)
                {
                    Console.WriteLine("no extra field");
                    return;
                }

                // extraLength
                br.ReadInt16();

                // Sub Field
                // id1
                br.ReadByte();
                // id2
                br.ReadByte();
                int length = br.ReadInt16();
                var data = br.ReadBytes(length);

            #if false
{
    "link": {
        "id": "3535",
        "type": "content"
    },
    "unity_version": "4.0.0f7",
    "pubdate": "03 Mar 2015",
    "version": "2.1.10",
    "upload_id": "53017",
    "version_id": "98481",
    "category": {
        "id": "109",
        "label": "Editor Extensions/Utilities"
    },
    "id": "3535",
    "title": "Script Inspector 2",
    "publisher": {
        "id": "1414",
        "label": "Flipbook Games"
    }
}
            #endif

                string json = Encoding.UTF8.GetString(data);
                var jsonData = Json.Deserialize(json) as Dictionary<string, object>;

                string version = GetValue(jsonData, "version") as string;
                var category = GetValue(jsonData, "category") as Dictionary<string, object>;
                string categoryLabel = GetValue(category, "label") as string;

                string title = GetValue(jsonData, "title") as string;
                string publishDate = GetValue(jsonData, "pubdate") as string;
                string unityVersion = GetValue(jsonData, "unity_version") as string;

                Console.WriteLine("title: " + title);
                Console.WriteLine("version: " + version);
                Console.WriteLine("categoryLabel: " + categoryLabel);
                Console.WriteLine("pub date: " + publishDate);
                Console.WriteLine("unity version: " + unityVersion);

                if (string.IsNullOrEmpty(title))
                    return;
                if (string.IsNullOrEmpty(categoryLabel))
                    return;

                string dir = Path.Combine(destDir, categoryLabel);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string fileName = title;
                if (!string.IsNullOrEmpty(version))
                    fileName += " v" + version;

                if (!string.IsNullOrEmpty(publishDate))
                    fileName += " (" + publishDate + ")";

                if (!string.IsNullOrEmpty(unityVersion))
                    fileName += " (unity " + unityVersion + ")";

                fileName += ".unitypackage";

                var illegalChars = new[] { '/', '\\', '\"', ':', '*', '?', '<', '>', '|', '\t' };
                foreach (char illegalChar in illegalChars)
                    fileName = fileName.Replace(illegalChar, ' ');

                destFileName = Path.Combine(dir, fileName);
            }

            if (string.IsNullOrEmpty(destFileName))
                return;

            if (!File.Exists(destFileName))
                File.Move(packagePath, destFileName);
        }
    }
}
