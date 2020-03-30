using ICSharpCode.SharpZipLib.Zip;
using Iteedee.ApkReader;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace ApkRenamer
{
    internal class Program
    {
        private static readonly string[] InvalidChars = new[]
        {
            "\\", "/", ":", "*", "?", "<", ">", "|"
        };

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                Environment.Exit(-1);
            }

            string apkPath = args[0];

            if (!File.Exists(apkPath))
            {
                Console.WriteLine("File not exists: " + apkPath);
                PrintHelp();
                Environment.Exit(-1);
            }

            string directory = Path.GetDirectoryName(apkPath);
            string extension = Path.GetExtension(apkPath);
            if (directory == null || extension == null)
            {
                Console.WriteLine("This is not an apk/xapk file: " + apkPath);
                PrintHelp();
                Environment.Exit(-1);
            }

            extension = extension.ToLower();
            if (extension != ".apk" && extension != ".xapk")
            {
                Console.WriteLine("This is not an apk/xapk file: " + apkPath);
                PrintHelp();
                Environment.Exit(-1);
            }

            string pattern = "|label|(|version-name|)" + extension;
            if (args.Length > 1)
            {
                pattern = args[1];
                if (!pattern.ToLower().EndsWith(extension))
                    pattern = pattern + extension;
            }

            if (extension == ".apk")
                RenameApk(apkPath, pattern, directory);
            else if (extension == ".xapk")
                RenameXapk(apkPath, pattern, directory);
        }

        private class XapkManifest
        {
            public string package_name;
            public string name;
            public string version_code;
            public string version_name;
        }

        private static void RenameXapk(string xapkPath, string pattern, string directory)
        {
            XapkManifest xapkManifest = null;
            using (var fileStream = File.OpenRead(xapkPath))
            using(var zipFile = new ZipFile(fileStream))
            {
                try
                {
                    foreach (ZipEntry zipEntry in zipFile)
                    {
                        if (zipEntry.IsDirectory) continue;
                        string entryName = zipEntry.Name.ToLower();

                        if (entryName == "manifest.json")
                        {
                            var manifestData = new byte[50 * 1024];
                            using (var stream = zipFile.GetInputStream(zipEntry))
                                stream.Read(manifestData, 0, manifestData.Length);

                            xapkManifest = JsonConvert.DeserializeObject<XapkManifest>(Encoding.UTF8.GetString(manifestData));
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    PrintHelp();
                    Environment.Exit(-1);
                }
            }

            if (xapkManifest == null)
                return;

            pattern = pattern.Replace("|label|", xapkManifest.name);
            pattern = pattern.Replace("|application-name|", xapkManifest.name);
            pattern = pattern.Replace("|name|", xapkManifest.name);
            pattern = pattern.Replace("|version-name|", xapkManifest.version_name);
            pattern = pattern.Replace("|version-code|", xapkManifest.version_code);
            pattern = pattern.Replace("|package-name|", xapkManifest.package_name);
            foreach (string invalidChar in InvalidChars)
                pattern = pattern.Replace(invalidChar, " ");

            string newApkPath = Path.Combine(directory, pattern);
            try
            {
                File.Move(xapkPath, newApkPath);
                Console.Write("{0} => {1}\n", xapkPath, newApkPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PrintHelp();
                Environment.Exit(-1);
            }
        }

        private static void RenameApk(string apkPath, string pattern, string directory)
        {
            ApkInfo apkInfo = null;
            try
            {
                apkInfo = ReadApk.ReadApkFromPath(apkPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PrintHelp();
                Environment.Exit(-1);
            }

            pattern = pattern.Replace("|label|", apkInfo.label);
            pattern = pattern.Replace("|application-name|", apkInfo.label);
            pattern = pattern.Replace("|name|", apkInfo.label);
            pattern = pattern.Replace("|version-name|", apkInfo.versionName);
            pattern = pattern.Replace("|version-code|", apkInfo.versionCode);
            pattern = pattern.Replace("|package-name|", apkInfo.packageName);
            foreach (string invalidChar in InvalidChars)
                pattern = pattern.Replace(invalidChar, " ");

            string newApkPath = Path.Combine(directory, pattern);
            try
            {
                File.Move(apkPath, newApkPath);
                Console.Write("{0} => {1}\n", apkPath, newApkPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PrintHelp();
                Environment.Exit(-1);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: ApkRenamer apk-path [pattern]");
            Console.WriteLine("pattern: |label| or |application-name| or |name|, |version-name|, |version-code|, |package-name|");
            Console.WriteLine("Default pattern is |label|(|version-name|).apk");
        }
    }
}
