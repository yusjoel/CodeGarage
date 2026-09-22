namespace UnityReleaseNotesTool
{
    /// <summary>
    /// 一个可以从接口重新下载的 HTML 文件，以及它对应的、应该已经下载好的 Markdown 文件。
    ///
    /// 两者文件名通常不同：仓库里的老文件按 URL slug 命名（5.5.6.html），
    /// 而接口返回的是完整版本号（5.5.6f1.md）。
    /// </summary>
    public class ReproducibleFile
    {
        public ReproducibleFile(string htmlPath, string markdownPath)
        {
            HtmlPath = htmlPath;
            MarkdownPath = markdownPath;
        }

        public string HtmlPath { get; }

        public string MarkdownPath { get; }
    }
}
