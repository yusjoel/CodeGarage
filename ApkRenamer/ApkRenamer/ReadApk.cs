using System.IO;
using Ionic.Zip;
using Iteedee.ApkReader;

namespace ApkRenamer
{
    public class ReadApk
    {
        public static ApkInfo ReadApkFromPath(string path)
        {
            byte[] manifestData = null;
            byte[] resourcesData = null;
#if false
            using (ICSharpCode.SharpZipLib.Zip.ZipInputStream zip = new ICSharpCode.SharpZipLib.Zip.ZipInputStream(File.OpenRead(path)))
            {
                using (var filestream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    ICSharpCode.SharpZipLib.Zip.ZipFile zipfile = new ICSharpCode.SharpZipLib.Zip.ZipFile(filestream);
                    ICSharpCode.SharpZipLib.Zip.ZipEntry item;
                    while ((item = zip.GetNextEntry()) != null)
                    {
                        if (item.Name.ToLower() == "androidmanifest.xml")
                        {
                            manifestData = new byte[50 * 1024];
                            using (Stream strm = zipfile.GetInputStream(item))
                            {
                                strm.Read(manifestData, 0, manifestData.Length);
                            }

                        }
                        if (item.Name.ToLower() == "resources.arsc")
                        {
                            using (Stream strm = zipfile.GetInputStream(item))
                            {
                                using (BinaryReader s = new BinaryReader(strm))
                                {
                                    resourcesData = s.ReadBytes((int)s.BaseStream.Length);

                                }
                            }
                        }

                        if (resourcesData != null && manifestData != null)
                            break;
                    }
                }
            }
#elif false
            var fileStream = File.OpenRead(path);
            var zipFile = new ZipFile(fileStream);
            foreach (ZipEntry zipEntry in zipFile)
            {
                if(zipEntry.IsDirectory) continue;
                string entryName = zipEntry.Name.ToLower();
                if (entryName == "androidmanifest.xml")
                {
                    manifestData = new byte[50 * 1024];
                    using (var stream = zipFile.GetInputStream(zipEntry))
                    {
                        stream.Read(manifestData, 0, manifestData.Length);
                    }

                }
                else if (entryName == "resources.arsc")
                {
                    using (var stream = zipFile.GetInputStream(zipEntry))
                    {
                        using (var binaryReader = new BinaryReader(stream))
                        {
                            resourcesData = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
                        }
                    }
                }
            }
#else
            using (var zipFile = ZipFile.Read(path))
            {
                foreach (var zipEntry in zipFile)
                {
                    if (zipEntry.IsDirectory) continue;
                    string entryName = zipEntry.FileName.ToLower();
                    if (entryName == "androidmanifest.xml")
                    {
                        using (var stream = new MemoryStream())
                        {
                            zipEntry.Extract(stream);
                            manifestData = stream.ToArray();
                        }
                    }
                    else if (entryName == "resources.arsc")
                    {
                        using (var stream = new MemoryStream())
                        {
                            zipEntry.Extract(stream);
                            resourcesData = stream.ToArray();
                        }
                    }
                }
            }


#endif

            var apkReader = new ApkReader();
            var info = apkReader.extractInfo(manifestData, resourcesData);
#if false
            Console.WriteLine(string.Format("Package Name: {0}", info.packageName));
            Console.WriteLine(string.Format("Version Name: {0}", info.versionName));
            Console.WriteLine(string.Format("Version Code: {0}", info.versionCode));

            Console.WriteLine(string.Format("App Has Icon: {0}", info.hasIcon));
            if(info.iconFileName.Count > 0)
                Console.WriteLine(string.Format("App Icon: {0}", info.iconFileName[0]));
            Console.WriteLine(string.Format("Min SDK Version: {0}", info.minSdkVersion));
            Console.WriteLine(string.Format("Target SDK Version: {0}", info.targetSdkVersion));

            if (info.Permissions != null && info.Permissions.Count > 0)
            {
                Console.WriteLine("Permissions:");
                info.Permissions.ForEach(f =>
                {
                    Console.WriteLine(string.Format("   {0}", f));
                });
            }
            else
                Console.WriteLine("No Permissions Found");

            Console.WriteLine(string.Format("Supports Any Density: {0}", info.supportAnyDensity));
            Console.WriteLine(string.Format("Supports Large Screens: {0}", info.supportLargeScreens));
            Console.WriteLine(string.Format("Supports Normal Screens: {0}", info.supportNormalScreens));
            Console.WriteLine(string.Format("Supports Small Screens: {0}", info.supportSmallScreens));
#endif
            return info;
        }
    }
}
