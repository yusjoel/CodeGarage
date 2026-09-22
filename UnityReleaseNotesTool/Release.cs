using System;

namespace UnityReleaseNotesTool
{
    /// <summary>
    /// 一个 Unity 编辑器版本，来自 services.unity.com 的 getUnityReleases 查询。
    /// </summary>
    public class Release
    {
        public Release(string version, string stream, DateTime? releaseDate, string releaseNotesUrl)
        {
            Version = version;
            Stream = stream;
            ReleaseDate = releaseDate;
            ReleaseNotesUrl = releaseNotesUrl;
        }

        public string Version { get; }

        /// <summary>SUPPORTED / LTS / TECH / BETA / ALPHA</summary>
        public string Stream { get; }

        public DateTime? ReleaseDate { get; }

        /// <summary>Google Cloud Storage 上的 .md 文件地址。</summary>
        public string ReleaseNotesUrl { get; }

        /// <summary>
        /// 归档目录 = 版本号第一段。6000.6.2f1 =&gt; 6000，2022.3.25f1 =&gt; 2022，5.0.1f1 =&gt; 5。
        /// </summary>
        public string DirectoryName
        {
            get
            {
                int dot = Version.IndexOf('.');
                return dot < 0 ? Version : Version.Substring(0, dot);
            }
        }

        public string FileName => Version + ".md";

        /// <summary>
        /// 剥掉结尾的发布类型后缀（a/b/f/p + 序号）。
        /// 用来把仓库里缺后缀的老文件名（2017.1.1）和 API 的完整版本号（2017.1.1f1）对上。
        /// </summary>
        public static string NormalizeVersion(string version)
        {
            int i = version.Length;
            while (i > 0 && char.IsDigit(version[i - 1]))
                i--;

            if (i == version.Length)
                return version;

            if (i == 0 || "abfp".IndexOf(char.ToLowerInvariant(version[i - 1])) < 0)
                return version;

            return version.Substring(0, i - 1);
        }
    }
}
