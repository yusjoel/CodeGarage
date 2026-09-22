using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityReleaseNotesTool
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            // 控制台默认是 GBK，中文输出会变成乱码。重定向时设置编码可能失败，忽略即可。
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch (IOException)
            {
            }

            return CommandLine.Parser.Default.ParseArguments<Options>(args)
                .MapResult(Run, _ => 1);
        }

        private static int Run(Options options)
        {
            string folder = options.ReleaseNotesFolder;

            if (string.IsNullOrWhiteSpace(folder))
            {
                Console.WriteLine("用法：UnityReleaseNotesTool <releaseNotesFolder> [--archive-html] [--skip-download]");
                return 1;
            }

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            IReadOnlyList<Release> releases;

            using (var grabber = new Grabber(folder))
            {
                if (options.SkipDownload)
                {
                    releases = grabber.FetchAllReleases();
                    Console.WriteLine($"接口返回 {releases.Count} 个版本（已跳过下载）。");
                }
                else
                {
                    releases = grabber.Execute();
                }
            }

            if (!options.SkipDownload)
            {
                using var downloadGrabber = new DownloadGrabber(folder);
                downloadGrabber.Execute(releases);
            }

            var legacyConverter = new LegacyConverter(folder, releases);
            legacyConverter.Execute();

            new IndexGenerator(folder).Execute();

            if (options.ArchiveHtml)
                ArchiveHtml(legacyConverter, folder);

            return 0;
        }

        /// <summary>
        /// 把原始 HTML 移到 Backup/ 下（保持原有相对路径），主目录只留 Markdown。
        ///
        /// 是移动而不是删除：这些 HTML 是转换孤本 Markdown 的唯一来源，
        /// 而且个别版本（2020.3.15f1）接口给的是残缺的短日志，完整的只存在于这里的归档中。
        /// </summary>
        private static void ArchiveHtml(LegacyConverter legacyConverter, string folder)
        {
            var all = new List<string>();

            all.AddRange(legacyConverter.FindLegacyHtml());
            all.AddRange(legacyConverter.FindReproducibleHtml().Select(f => f.HtmlPath));

            string backupRoot = Path.Combine(folder, LegacyConverter.BackupDirectoryName);
            int moved = 0;

            foreach (string path in all)
            {
                string relative = Path.GetRelativePath(folder, path);
                string target = Path.Combine(backupRoot, relative);

                Directory.CreateDirectory(Path.GetDirectoryName(target));

                if (File.Exists(target))
                    File.Delete(target);

                File.Move(path, target);
                moved++;
            }

            Console.WriteLine($"已把 {moved} 个原始 HTML 移到 {LegacyConverter.BackupDirectoryName}/ 备份。");
        }

        private class Options
        {
            [Value(0, MetaName = "releaseNotesFolder", HelpText = "UnityReleaseNotes 仓库根目录")]
            public string ReleaseNotesFolder { get; set; }

            [Option("archive-html", HelpText = "把原始 HTML 移到 Backup/ 目录（主目录只留 Markdown）")]
            public bool ArchiveHtml { get; set; }

            [Option("skip-download", HelpText = "只枚举版本并处理本地文件，不下载 Markdown")]
            public bool SkipDownload { get; set; }
        }
    }
}
