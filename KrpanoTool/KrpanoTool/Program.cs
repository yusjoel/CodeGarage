using System.IO;
using System.Text;

namespace KrpanoTool
{
    internal class Program
    {
        private static readonly string batchScript = @"@echo off
setlocal enabledelayedexpansion

set aria2c=""D:\Software\aria2\aria2c.exe""
set krpano=""D:\Program Files (x86)\PanoDownloaderMaster\krpanotools.exe""

%aria2c% -i ""{0}"" -d ""{1}""

set sides=fbudlr

for /l %%i in (0, 1, 5) do (
    set side=!sides:~%%i,1!
    %krpano% maketiles ""{1}/l{6}_!side!_{7}_{8}.jpg"" ""{1}_!side!.jpg"" -insize={3}x{4} -intilesize={5}
)

%krpano% cube2sphere ""{1}_*.jpg"" -o=""{1}.jpg""
";

        private static void Main(string[] args)
        {
            var title = args[0];
            var tileSize = int.Parse(args[1]);
            var tiledImageWidth = int.Parse(args[2]);
            var tiledImageHeight = int.Parse(args[3]);
            var url = args[4];
            var downloadDirectory = Path.Combine(args[5], title);
            var level = int.Parse(args[6]);

            var v = (tiledImageWidth + tileSize - 1) / tileSize;
            var h = (tiledImageHeight + tileSize - 1) / tileSize;
            var sides = "fbudlr";

            if (!Directory.Exists(downloadDirectory))
                Directory.CreateDirectory(downloadDirectory);

            string vPlaceholder = "%%v";
            if (url.Contains("%v"))
                vPlaceholder = "%%v";
            else if (url.Contains("%0v"))
                vPlaceholder = "%%0v";

            string hPlaceholder = "%%h";
            if (url.Contains("%h"))
                hPlaceholder = "%%h";
            else if (url.Contains("%0h"))
                hPlaceholder = "%%0h";

            var filePath = Path.Combine(downloadDirectory, "download.lst");
            using (var streamWriter = new StreamWriter(File.Open(filePath, FileMode.Create)))
            {
                foreach (var side in sides)
                    for (var vi = 1; vi <= v; vi++)
                    for (var hi = 1; hi <= h; hi++)
                    {
                        var downloadUrl = url
                            .Replace("%s", side.ToString())
                            .Replace("%v", vi.ToString())
                            .Replace("%h", hi.ToString())
                            .Replace("%0v", vi.ToString("d2"))
                            .Replace("%0h", hi.ToString("d2"));
                        streamWriter.WriteLine(downloadUrl);
                    }
            }

            var scriptPath = Path.Combine(downloadDirectory, "run.bat");
            using (var streamWriter =
                new StreamWriter(File.Open(scriptPath, FileMode.Create), Encoding.GetEncoding("gb2312")))
            {
                var line = string.Format(batchScript,
                    filePath,
                    downloadDirectory,
                    title,
                    tiledImageWidth, tiledImageHeight, tileSize,
                    level, vPlaceholder, hPlaceholder
                );
                streamWriter.WriteLine(line);
            }
        }
    }
}
