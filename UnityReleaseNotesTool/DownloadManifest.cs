using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityReleaseNotesTool
{
    /// <summary>
    /// 单个版本的下载清单，存成 &lt;version&gt;.downloads.json，与发布日志分开放
    /// （对应 README TODO 里的「分离下载和发布日志」）。
    ///
    /// 存的是接口给出的真实 URL，不做任何拼接：实测编辑器安装包虽然规律，
    /// 但各平台模块的有无是逐个版本的事（2017.4.40f1 有 Linux 而更晚的 2018.1.0f1 没有），
    /// JDK 依赖包名还带内容哈希，推导不出来。
    /// </summary>
    public class DownloadManifest
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("revision")]
        public string Revision { get; set; }

        [JsonPropertyName("unityHubDeepLink")]
        public string UnityHubDeepLink { get; set; }

        [JsonPropertyName("releaseDate")]
        public string ReleaseDate { get; set; }

        /// <summary>数据来自哪里：hub-api 或 unity-com。</summary>
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("downloads")]
        public List<DownloadEntry> Downloads { get; set; } = new List<DownloadEntry>();

        [JsonPropertyName("thirdPartyNotices")]
        public List<ThirdPartyNotice> ThirdPartyNotices { get; set; } = new List<ThirdPartyNotice>();
    }

    public class DownloadEntry
    {
        [JsonPropertyName("platform")]
        public string Platform { get; set; }

        [JsonPropertyName("architecture")]
        public string Architecture { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("installerSize")]
        public long? InstallerSize { get; set; }

        [JsonPropertyName("modules")]
        public List<DownloadModule> Modules { get; set; } = new List<DownloadModule>();
    }

    public class DownloadModule
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class ThirdPartyNotice
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
