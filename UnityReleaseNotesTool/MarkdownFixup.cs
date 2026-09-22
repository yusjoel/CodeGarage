using System.Text;
using System.Text.RegularExpressions;

namespace UnityReleaseNotesTool
{
    /// <summary>
    /// 对 Markdown 正文做统一整理。下载的和本地转换的都要过一遍，保证仓库里风格一致。
    /// </summary>
    public static class MarkdownFixup
    {
        /// <summary>
        /// 把指向 unity.com 的站内相对链接补成绝对地址。
        ///
        /// Unity 导出的 Markdown 里大量使用 <c>](/security#CVE-...)</c>、<c>![](/sites/default/files/...)</c>
        /// 这类相对路径，它们在 GitHub Pages 上全部 404。5.0.0f4 一个文件就有 31 张这样的图。
        /// </summary>
        private static readonly Regex RelativeUrl = new Regex(@"\]\(/(?!/)", RegexOptions.Compiled);

        public static string RewriteRelativeUrls(string markdown)
        {
            return string.IsNullOrEmpty(markdown)
                ? markdown
                : RelativeUrl.Replace(markdown, "](https://unity.com/");
        }

        /// <summary>
        /// 统一成 LF。Unity 自己返回的 .md 是 CRLF/LF 混用的（实测），
        /// 本地转换出来的又会跟着平台走，所以两边都归一，避免仓库里三种行尾混在一起。
        /// </summary>
        public static string NormalizeLineEndings(string text)
        {
            return string.IsNullOrEmpty(text)
                ? text
                : text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        /// <summary>
        /// 以 UTF-8 解码并整理；解不出来就原样返回，不冒损坏文件的风险。
        /// </summary>
        public static byte[] ProcessBytes(byte[] raw)
        {
            string text;

            try
            {
                text = new UTF8Encoding(false, true).GetString(raw);
            }
            catch (DecoderFallbackException)
            {
                return raw;
            }

            string processed = RewriteRelativeUrls(NormalizeLineEndings(text));
            byte[] encoded = new UTF8Encoding(false).GetBytes(processed);

            return encoded;
        }
    }
}
