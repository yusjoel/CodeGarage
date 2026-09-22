using HtmlAgilityPack;
using ReverseMarkdown;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityReleaseNotesTool
{
    /// <summary>
    /// 处理仓库里 Unity 官方已经下架的"孤本" HTML —— 3.x / 4.x 全部，以及 5.0/5.1/5.2/5.3
    /// 大版本页。这些在 GraphQL 接口里查不到（接口最老只到 5.0.0f4），删掉就永久没了，
    /// 所以本地转成 Markdown 并保留原 HTML。
    ///
    /// 其余 HTML 都能从接口重新拿到，属于可再生文件，由 <see cref="FindReproducibleHtml"/> 列出待删除。
    /// </summary>
    public class LegacyConverter
    {
        private readonly string destinationDirectory;

        /// <summary>归一化版本号 → 接口里对应的版本，可能一对多（5.5.6f1 和 5.5.6p1 都归一化成 5.5.6）。</summary>
        private readonly Dictionary<string, List<Release>> releasesByKey;

        public LegacyConverter(string destinationDirectory, IEnumerable<Release> releases)
        {
            this.destinationDirectory = destinationDirectory;

            releasesByKey = releases
                .GroupBy(r => Release.NormalizeVersion(r.Version), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        }

        public void Execute()
        {
            List<string> legacy = FindLegacyHtml();

            Console.WriteLine($"发现 {legacy.Count} 个官方已下架的孤本 HTML，开始转换为 Markdown。");

            var converter = new Converter(new Config
            {
                Tags = { Unknown = Config.UnknownTagsOption.Bypass },
                // 默认是 Environment.NewLine，在 Windows 上会写出 CRLF，和接口下载的 .md 不一致。
                Formatting = { RemoveComments = true, OutputLineEnding = "\n" },
                Links = { SmartHref = true },
                GithubFlavored = true
            });

            int converted = 0;

            foreach (string sourcePath in legacy)
            {
                string version = Path.GetFileNameWithoutExtension(sourcePath);
                string destinationPath = Path.Combine(
                    Path.GetDirectoryName(sourcePath) ?? destinationDirectory,
                    version + ".md");

                try
                {
                    string content = ExtractContent(File.ReadAllText(sourcePath));
                    string markdown = Tidy(converter.Convert(content));

                    markdown = MarkdownFixup.RewriteRelativeUrls(
                        MarkdownFixup.NormalizeLineEndings(markdown));

                    if (string.IsNullOrWhiteSpace(markdown))
                    {
                        Console.WriteLine($"  跳过（正文为空）：{sourcePath}");
                        continue;
                    }

                    File.WriteAllText(destinationPath, markdown, new UTF8Encoding(false));
                    converted++;

                    // 原 HTML 保留，作为转换结果的对照。
                }
                catch (Exception e)
                {
                    Console.WriteLine($"  转换失败 {sourcePath}：{e.Message}");
                }
            }

            Console.WriteLine($"已转换 {converted} 个孤本；原 HTML 保留未删除。");
        }

        /// <summary>
        /// 列出可以从接口重新下载、因而是多余的 HTML 文件（用于确认后的清理步骤），
        /// 并给出它对应的、应当已经下载好的 Markdown 路径。
        /// </summary>
        public List<ReproducibleFile> FindReproducibleHtml()
        {
            var result = new List<ReproducibleFile>();

            foreach (string path in EnumerateContentHtml())
            {
                string key = Release.NormalizeVersion(Path.GetFileNameWithoutExtension(path));

                if (!releasesByKey.TryGetValue(key, out List<Release> candidates))
                    continue;

                Release release = PickCanonical(key, candidates);

                result.Add(new ReproducibleFile(path, GetMarkdownPath(release)));
            }

            return result.OrderBy(f => f.HtmlPath, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public List<string> FindLegacyHtml()
        {
            return EnumerateContentHtml()
                .Where(path => !releasesByKey.ContainsKey(
                    Release.NormalizeVersion(Path.GetFileNameWithoutExtension(path))))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 仓库里的老文件名去掉了发布类型后缀（5.5.6.html 对应接口的 5.5.6f1）。
        /// 优先选正式版 f，避免把内容配到 5.5.6p1 之类的补丁版上去。
        /// </summary>
        private static Release PickCanonical(string key, List<Release> candidates)
        {
            Release final = candidates.FirstOrDefault(
                r => r.Version.StartsWith(key + "f", StringComparison.OrdinalIgnoreCase));

            if (final != null)
                return final;

            return candidates.OrderBy(r => r.Version, StringComparer.OrdinalIgnoreCase).First();
        }

        private string GetMarkdownPath(Release release)
        {
            return Path.Combine(destinationDirectory, release.DirectoryName, release.FileName);
        }

        /// <summary>
        /// 仓库里的内容 HTML。排除生成出来的 index.html / index-*.html。
        /// </summary>
        private IEnumerable<string> EnumerateContentHtml()
        {
            if (!Directory.Exists(destinationDirectory))
                yield break;

            foreach (string path in Directory.EnumerateFiles(destinationDirectory, "*.html", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(path);

                if (name.StartsWith("index", StringComparison.OrdinalIgnoreCase))
                    continue;

                string relative = Path.GetRelativePath(destinationDirectory, path);

                // 跳过构建产物，以及 .git —— 递归下去会把历史版本也扫出来。
                if (relative.StartsWith(".", StringComparison.Ordinal)
                    || relative.StartsWith("bin", StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith("obj", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Backup 里放的是已经归档的原始 HTML，不能再被当成待处理内容。
                if (IsUnderBackup(relative))
                    continue;

                yield return path;
            }
        }

        /// <summary>Backup 目录名，原始 HTML 归档到这里之后就不再参与处理。</summary>
        public const string BackupDirectoryName = "Backup";

        private static bool IsUnderBackup(string relativePath)
        {
            string first = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];

            return string.Equals(first, BackupDirectoryName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 取出正文。老页面的结构非常统一：正文都在若干 &lt;div class="g12 markdown"&gt; 里，
        /// 而 Changeset 块（div.g12.pt0）和前后翻页导航（div.g12 &gt; .scroll-link）都没有 markdown 类，
        /// 所以按类筛选即可自动排除。
        /// </summary>
        private static string ExtractContent(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNodeCollection blocks = document.DocumentNode.SelectNodes(
                "//div[contains(concat(' ', normalize-space(@class), ' '), ' markdown ')]");

            if (blocks == null)
                return string.Empty;

            var builder = new StringBuilder();

            foreach (HtmlNode block in blocks)
            {
                // 正文块里仍混着下载按钮和"New to Unity? Get started"推广语。
                RemoveNodes(block, ".//a[contains(@class, 'blue-btn')]");
                RemoveNodes(block, ".//p[contains(concat(' ', normalize-space(@class), ' '), ' clear ')]");

                builder.AppendLine(block.InnerHtml);
            }

            return builder.ToString();
        }

        private static void RemoveNodes(HtmlNode scope, string xpath)
        {
            List<HtmlNode> targets = scope.SelectNodes(xpath)?.ToList();

            if (targets == null)
                return;

            foreach (HtmlNode node in targets)
                node.Remove();
        }

        /// <summary>收拾转换后的空白，避免把三行空行之类的塞进仓库。</summary>
        private static string Tidy(string markdown)
        {
            markdown = Regex.Replace(markdown, @"[ \t]+\r?\n", "\n");
            markdown = Regex.Replace(markdown, @"(\r?\n){3,}", "\n\n");

            return markdown.Trim() + "\n";
        }
    }
}
