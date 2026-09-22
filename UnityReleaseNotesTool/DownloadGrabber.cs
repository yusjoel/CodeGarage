using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UnityReleaseNotesTool
{
    /// <summary>
    /// 抓取每个版本的下载链接，写成 &lt;version&gt;.downloads.json。
    ///
    /// 主数据源是 Unity Hub 的公开 REST 接口。它配额很紧（实测被打爆后会直接 429
    /// 「Quota has been exceeded」，连单个请求都不放行），所以**按页列举**而不是逐个版本查：
    /// 一页 25 个版本带完整下载信息，全量只要约 65 次请求，而不是 1601 次。
    ///
    /// Hub 接口查不到 XLTS 长期支持版，这些从 unity.com 的 whats-new 页补 ——
    /// 页面服务端渲染的 payload 里有 downloads 数组。
    /// </summary>
    public class DownloadGrabber : IDisposable
    {
        private const string HubApiUrl = "https://services.api.unity.com/unity/editor/release/v1/releases";
        private const string WhatsNewUrl = "https://unity.com/releases/editor/whats-new/";

        /// <summary>接口上限就是 25，超过会 400。</summary>
        private const int PageSize = 25;

        private readonly string destinationDirectory;

        private readonly HttpClient httpClient;

        public DownloadGrabber(string destinationDirectory)
        {
            this.destinationDirectory = destinationDirectory;

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(3) };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        public void Execute(IReadOnlyList<Release> releases)
        {
            List<Release> pending = releases.Where(r => !File.Exists(GetManifestPath(r))).ToList();

            Console.WriteLine($"下载清单：共 {releases.Count} 个版本，{releases.Count - pending.Count} 个已存在，需抓取 {pending.Count} 个。");

            if (pending.Count == 0)
                return;

            foreach (Release release in pending)
                Directory.CreateDirectory(Path.GetDirectoryName(GetManifestPath(release)));

            Dictionary<string, DownloadManifest> fromHub = FetchAllFromHub();

            Console.WriteLine($"Hub 接口返回 {fromHub.Count} 个版本的下载信息。");

            var failures = new List<string>();
            int viaHub = 0;
            int viaUnity = 0;

            using (var progressBar = new ProgressBar(pending.Count, "正在整理下载清单",
                       new ProgressBarOptions { ProgressCharacter = '─', ProgressBarOnBottom = true }))
            {
                object gate = new object();
                int done = 0;

                // Hub 一页就覆盖 25 个版本，只有它没给的那些（XLTS）才需要额外请求 unity.com，
                // 所以这里并发数只影响少数版本，开小一点避免又触发配额。
                Parallel.ForEach(
                    pending,
                    new ParallelOptions { MaxDegreeOfParallelism = 4 },
                    release =>
                    {
                        try
                        {
                            DownloadManifest manifest = null;

                            // Hub 接口有下载链接，或者至少有 revision / Hub 深度链接，就直接用。
                            if (fromHub.TryGetValue(release.Version, out DownloadManifest hub)
                                && (hub.Downloads.Count > 0
                                    || !string.IsNullOrEmpty(hub.Revision)
                                    || !string.IsNullOrEmpty(hub.UnityHubDeepLink)))
                            {
                                manifest = hub;
                                Interlocked.Increment(ref viaHub);
                            }
                            else
                            {
                                manifest = FetchFromWhatsNew(release);
                                Interlocked.Increment(ref viaUnity);
                            }

                            if (manifest == null
                                || (manifest.Downloads.Count == 0
                                    && string.IsNullOrEmpty(manifest.Revision)
                                    && string.IsNullOrEmpty(manifest.UnityHubDeepLink)))
                            {
                                lock (gate)
                                    failures.Add($"{release.Version}: 两个数据源都没有下载信息");
                            }
                            else
                            {
                                WriteManifest(release, manifest);
                            }
                        }
                        catch (Exception e)
                        {
                            lock (gate)
                                failures.Add($"{release.Version}: {e.Message}");
                        }
                        finally
                        {
                            int current = Interlocked.Increment(ref done);
                            lock (gate)
                                progressBar.Tick(current);
                        }
                    });
            }

            Console.WriteLine($"下载清单完成：{viaHub} 个来自 Hub API，{viaUnity} 个来自 unity.com。");

            if (failures.Count > 0)
            {
                Console.WriteLine($"有 {failures.Count} 个版本没抓全（重新运行即可重试）：");
                foreach (string failure in failures.Take(20))
                    Console.WriteLine("  " + failure);

                if (failures.Count > 20)
                    Console.WriteLine($"  …… 另有 {failures.Count - 20} 条");
            }
        }

        private void WriteManifest(Release release, DownloadManifest manifest)
        {
            string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });

            string path = GetManifestPath(release);
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, json + "\n", new UTF8Encoding(false));
            File.Move(temporary, path, true);
        }

        /// <summary>
        /// 按页列举 Hub 接口。返回 版本号 → 下载清单。
        /// </summary>
        private Dictionary<string, DownloadManifest> FetchAllFromHub()
        {
            var map = new Dictionary<string, DownloadManifest>(StringComparer.OrdinalIgnoreCase);
            int offset = 0;
            int total = -1;

            while (true)
            {
                string json = GetWithRetry($"{HubApiUrl}?limit={PageSize}&offset={offset}");

                if (json == null)
                    break;

                using JsonDocument document = JsonDocument.Parse(json);

                if (!document.RootElement.TryGetProperty("results", out JsonElement results)
                    || results.ValueKind != JsonValueKind.Array)
                    break;

                if (total < 0 && document.RootElement.TryGetProperty("total", out JsonElement t))
                    total = t.GetInt32();

                int received = results.GetArrayLength();

                foreach (JsonElement node in results.EnumerateArray())
                {
                    DownloadManifest manifest = ReadHubNode(node);

                    if (manifest != null)
                        map[manifest.Version] = manifest;
                }

                offset += received;

                if (received == 0 || (total > 0 && offset >= total))
                    break;
            }

            return map;
        }

        /// <summary>
        /// 配额被打爆时接口返回 429 且是全局的，重试间隔要够长才有意义。
        /// </summary>
        private string GetWithRetry(string url)
        {
            int[] delaysSeconds = { 5, 20, 60 };

            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    using HttpResponseMessage response = httpClient.GetAsync(url).GetAwaiter().GetResult();

                    if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < delaysSeconds.Length)
                    {
                        Console.WriteLine($"  触发接口配额限制，{delaysSeconds[attempt]} 秒后重试……");
                        Task.Delay(TimeSpan.FromSeconds(delaysSeconds[attempt])).GetAwaiter().GetResult();
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                catch (HttpRequestException) when (attempt < delaysSeconds.Length)
                {
                    Task.Delay(TimeSpan.FromSeconds(delaysSeconds[attempt])).GetAwaiter().GetResult();
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"  Hub 接口不可用：{e.Message}");
                    return null;
                }
            }
        }

        private static DownloadManifest ReadHubNode(JsonElement node)
        {
            string version = GetString(node, "version");

            if (string.IsNullOrEmpty(version))
                return null;

            var manifest = new DownloadManifest
            {
                Version = version,
                Revision = GetString(node, "shortRevision"),
                UnityHubDeepLink = GetString(node, "unityHubDeepLink"),
                ReleaseDate = GetString(node, "releaseDate"),
                Source = "hub-api"
            };

            if (node.TryGetProperty("downloads", out JsonElement downloads)
                && downloads.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement entry in downloads.EnumerateArray())
                {
                    var item = new DownloadEntry
                    {
                        Platform = GetString(entry, "platform"),
                        Architecture = GetString(entry, "architecture"),
                        Type = GetString(entry, "type"),
                        Url = GetString(entry, "url"),
                        InstallerSize = GetSize(entry, "downloadSize")
                    };

                    if (entry.TryGetProperty("modules", out JsonElement modules)
                        && modules.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement module in modules.EnumerateArray())
                        {
                            item.Modules.Add(new DownloadModule
                            {
                                Name = GetString(module, "name"),
                                Category = GetString(module, "category"),
                                Url = GetString(module, "url")
                            });
                        }
                    }

                    manifest.Downloads.Add(item);
                }
            }

            if (node.TryGetProperty("thirdPartyNotices", out JsonElement notices)
                && notices.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement notice in notices.EnumerateArray())
                {
                    manifest.ThirdPartyNotices.Add(new ThirdPartyNotice
                    {
                        Name = GetString(notice, "originalFileName") ?? GetString(notice, "name"),
                        Url = GetString(notice, "url")
                    });
                }
            }

            return manifest;
        }

        /// <summary>
        /// 从 unity.com 的 whats-new 页补数据，覆盖 Hub 接口没有的 XLTS 版本。
        /// </summary>
        private DownloadManifest FetchFromWhatsNew(Release release)
        {
            string html;

            try
            {
                html = httpClient.GetStringAsync(WhatsNewUrl + Uri.EscapeDataString(release.Version))
                    .GetAwaiter().GetResult();
            }
            catch (HttpRequestException)
            {
                return null;
            }

            // payload 是塞在 JS 字符串里的 JSON，先反转义再按括号配对取出 downloads 数组。
            string unescaped = html
                .Replace("\\\"", "\"")
                .Replace("\\n", " ")
                .Replace("\\/", "/");

            string downloadsJson = ExtractArray(unescaped, "\"downloads\":[");

            var manifest = new DownloadManifest
            {
                Version = release.Version,
                Revision = ExtractString(unescaped, "\"shortRevision\":\""),
                UnityHubDeepLink = ExtractString(unescaped, "\"unityHubDeepLink\":\""),
                ReleaseDate = release.ReleaseDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Source = "unity-com"
            };

            if (downloadsJson != null)
            {
                using JsonDocument array = JsonDocument.Parse(downloadsJson);

                foreach (JsonElement entry in array.RootElement.EnumerateArray())
                {
                    var item = new DownloadEntry
                    {
                        Platform = GetString(entry, "platform"),
                        Architecture = GetString(entry, "architecture"),
                        Url = GetString(entry, "url")
                    };

                    if (entry.TryGetProperty("modules", out JsonElement modules)
                        && modules.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement module in modules.EnumerateArray())
                        {
                            item.Modules.Add(new DownloadModule
                            {
                                Name = GetString(module, "name"),
                                Category = GetString(module, "category"),
                                Url = GetString(module, "url")
                            });
                        }
                    }

                    manifest.Downloads.Add(item);
                }
            }
            else
            {
                // 老版本（5.1 那一批）页面里没有结构化的 downloads 数组，
                // 下载链接是以普通 <a href> 渲染的，只能直接从 HTML 里扫。
                CollectRawLinks(html, manifest);
                manifest.Source = "unity-com-html";
            }

            // 有些老版本 Unity 只发布了 Download Assistant，直接的编辑器安装包从来没有过
            // （实测 5.1.0f1 的三个编辑器 URL 都是 404）。这种情况清单可能为空，
            // 但只要还有 revision / Hub 链接就仍然有价值，不该丢掉。
            bool useful = manifest.Downloads.Count > 0
                          || !string.IsNullOrEmpty(manifest.Revision)
                          || !string.IsNullOrEmpty(manifest.UnityHubDeepLink);

            return useful ? manifest : null;
        }

        private static readonly System.Text.RegularExpressions.Regex RawDownloadUrl =
            new System.Text.RegularExpressions.Regex(
                @"https://download\.unity3d\.com/[^""'\\\s<>]+",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>从页面 HTML 里直接收集下载链接，并按 URL 里的安装器目录推断平台。</summary>
        private static void CollectRawLinks(string html, DownloadManifest manifest)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (System.Text.RegularExpressions.Match match in RawDownloadUrl.Matches(html))
            {
                string url = match.Value.Replace("&amp;", "&");

                if (!seen.Add(url))
                    continue;

                string[] segments = url.Split('/');

                // .../download_unity/{revision}/{installerDir}/{fileName}
                string installerDir = segments.Length >= 2 ? segments[segments.Length - 2] : string.Empty;

                manifest.Downloads.Add(new DownloadEntry
                {
                    Platform = InferPlatform(installerDir),
                    Type = installerDir,
                    Url = url,
                    Modules = new List<DownloadModule>
                    {
                        new DownloadModule
                        {
                            Name = segments.Length >= 1 ? segments[segments.Length - 1] : url,
                            Category = installerDir.IndexOf("TargetInstaller", StringComparison.OrdinalIgnoreCase) >= 0
                                       || installerDir.IndexOf("TargetSupportInstaller", StringComparison.OrdinalIgnoreCase) >= 0
                                ? "PLATFORM"
                                : "EDITOR",
                            Url = url
                        }
                    }
                });
            }
        }

        private static string InferPlatform(string installerDir)
        {
            if (installerDir.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0)
                return "WINDOWS";

            if (installerDir.IndexOf("Mac", StringComparison.OrdinalIgnoreCase) >= 0)
                return "MAC_OS";

            if (installerDir.IndexOf("Linux", StringComparison.OrdinalIgnoreCase) >= 0)
                return "LINUX";

            return "OTHER";
        }

        /// <summary>从 marker 位置开始做括号配对，取出完整的 JSON 数组。</summary>
        private static string ExtractArray(string text, string marker)
        {
            int start = text.IndexOf(marker, StringComparison.Ordinal);

            if (start < 0)
                return null;

            int open = start + marker.Length - 1;
            int depth = 0;

            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '[')
                    depth++;
                else if (text[i] == ']')
                {
                    depth--;

                    if (depth == 0)
                        return text.Substring(open, i - open + 1);
                }
            }

            return null;
        }

        private static string ExtractString(string text, string marker)
        {
            int start = text.IndexOf(marker, StringComparison.Ordinal);

            if (start < 0)
                return null;

            start += marker.Length;
            int end = text.IndexOf('"', start);

            return end < 0 ? null : text.Substring(start, end - start);
        }

        private string GetManifestPath(Release release)
        {
            return Path.Combine(destinationDirectory, release.DirectoryName, release.Version + ".downloads.json");
        }

        private static string GetString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement value)
                   && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static long? GetSize(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out JsonElement size)
                || size.ValueKind != JsonValueKind.Object
                || !size.TryGetProperty("value", out JsonElement value)
                || value.ValueKind != JsonValueKind.Number)
                return null;

            return value.GetInt64();
        }
    }
}
