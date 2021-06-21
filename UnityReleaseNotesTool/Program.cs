using CommandLine;
using System.IO;
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable ClassNeverInstantiated.Local

namespace UnityReleaseNotesTool
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    //string downloadFolder = "d:/whats-new";
                    string downloadFolder = o.DownloadFolder;
                    if (!Directory.Exists(downloadFolder))
                        Directory.CreateDirectory(downloadFolder);

                    //string url = "https://unity3d.com/unity/whats-new/2020.1.3";
                    string url = o.WhatsNewUrl;
                    var grabber = new Grabber(url, downloadFolder);
                    grabber.Execute();

                    //string destinationDirectory = @"D:\Unity\Workspace\UnityReleaseNotes\";
                    string destinationDirectory = o.ReleaseNotesFolder;
                    var parser = new Parser(downloadFolder, destinationDirectory);
                    parser.Execute();
                });
        }

        private class Options
        {
            [Value(0)]
            public string DownloadFolder { get; set; }

            [Value(1)]
            public string ReleaseNotesFolder { get; set; }

            [Value(2)]
            public string WhatsNewUrl { get; set; }
        }
    }
}
