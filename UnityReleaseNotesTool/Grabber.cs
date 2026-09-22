using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
    /// 通过 services.unity.com 的 GraphQL 接口枚举 Unity 版本，并下载官方 Markdown 发布日志。
    /// </summary>
    public class Grabber : IDisposable
    {
        private const string GraphQlUrl = "https://services.unity.com/api/live-platform/v1/graphql";

        /// <summary>
        /// 接口硬上限。传更大的值会返回 BAD_USER_INPUT —— 注意此时 totalCount 仍会正常返回，
        /// 只有 edges 为空，所以只看 totalCount 会误判成功。
        /// </summary>
        private const int PageSize = 500;

        /// <summary>
        /// 必须带 XLTS，否则会漏掉 2021.3.46-58 / 2022.3.63-76 这 29 个长期支持补丁版
        /// （1572 vs 1601）。
        /// </summary>
        private const string Query = @"query Q($l: Int, $s: Int) {
  getUnityReleases(limit: $l, skip: $s, entitlements: [XLTS]) {
    totalCount
    edges {
      node {
        version
        stream
        releaseDate
        releaseNotes { url type }
      }
    }
  }
}";

        private readonly string destinationDirectory;

        private readonly HttpClient httpClient;

        public Grabber(string destinationDirectory)
        {
            this.destinationDirectory = destinationDirectory;

            // 1633 个文件、约 126MB 未压缩，开 gzip 能省掉大部分流量。
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "UnityReleaseNotesTool");

            RemoveStaleTemporaryFiles();
        }

        /// <summary>
        /// 上一次运行被中断时可能留下 .tmp。run.bat 用的是 git add -A，留着会被一起提交进去。
        /// </summary>
        private void RemoveStaleTemporaryFiles()
        {
            if (!Directory.Exists(destinationDirectory))
                return;

            foreach (string path in Directory.EnumerateFiles(destinationDirectory, "*.tmp", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                }
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        public IReadOnlyList<Release> Execute()
        {
            IReadOnlyList<Release> releases = FetchAllReleases();
            Console.WriteLine($"接口返回 {releases.Count} 个版本。");
            DownloadAllMarkdown(releases);

            return releases;
        }

        /// <summary>
        /// 分页拉取全部版本。返回 null 表示接口异常。
        /// </summary>
        public IReadOnlyList<Release> FetchAllReleases()
        {
            var releases = new List<Release>();
            int offset = 0;
            int reportedTotal = -1;

            while (true)
            {
                var payload = new
                {
                    query = Query,
                    variables = new { l = PageSize, s = offset }
                };

                string body = JsonSerializer.Serialize(payload);
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                using HttpResponseMessage response = httpClient.PostAsync(GraphQlUrl, content).GetAwaiter().GetResult();

                string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"GraphQL 请求失败 (HTTP {(int)response.StatusCode})：{Truncate(json)}");

                using JsonDocument document = JsonDocument.Parse(json);

                // 必须先查 errors。limit 超限时接口会同时返回 totalCount 和一条错误，
                // 不检查的话会把空结果当成成功。
                if (document.RootElement.TryGetProperty("errors", out JsonElement errors)
                    && errors.ValueKind == JsonValueKind.Array
                    && errors.GetArrayLength() > 0)
                {
                    throw new InvalidOperationException($"GraphQL 返回错误：{Truncate(errors[0].ToString())}");
                }

                if (!document.RootElement.TryGetProperty("data", out JsonElement data)
                    || !data.TryGetProperty("getUnityReleases", out JsonElement connection))
                    throw new InvalidOperationException($"GraphQL 响应缺少 getUnityReleases 节点：{Truncate(json)}");

                if (reportedTotal < 0)
                    reportedTotal = connection.GetProperty("totalCount").GetInt32();

                JsonElement edges = connection.GetProperty("edges");
                int received = edges.GetArrayLength();

                foreach (JsonElement edge in edges.EnumerateArray())
                {
                    JsonElement node = edge.GetProperty("node");
                    string version = node.GetProperty("version").GetString();

                    string stream = node.TryGetProperty("stream", out JsonElement streamElement)
                        ? streamElement.GetString()
                        : null;

                    DateTime? releaseDate = null;
                    if (node.TryGetProperty("releaseDate", out JsonElement dateElement)
                        && DateTime.TryParse(dateElement.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal, out DateTime parsed))
                        releaseDate = parsed;

                    string notesUrl = null;
                    if (node.TryGetProperty("releaseNotes", out JsonElement notes)
                        && notes.ValueKind == JsonValueKind.Object
                        && notes.TryGetProperty("url", out JsonElement urlElement)
                        && urlElement.ValueKind == JsonValueKind.String)
                        notesUrl = urlElement.GetString();

                    releases.Add(new Release(version, stream, releaseDate, notesUrl));
                }

                offset += received;

                if (received == 0 || offset >= reportedTotal)
                    break;
            }

            // 接口不做任何承诺，分页中途少给一页也看不出异常，所以这里主动对账。
            if (releases.Count != reportedTotal)
                throw new InvalidOperationException($"枚举数量不符：接口报告 {reportedTotal} 个版本，实际收到 {releases.Count} 个。");

            int distinct = releases.Select(r => r.Version).Distinct(StringComparer.OrdinalIgnoreCase).Count();

            if (distinct != releases.Count)
                throw new InvalidOperationException($"枚举结果有重复版本号：{releases.Count} 条里只有 {distinct} 个不同版本。");

            return releases;
        }

        private void DownloadAllMarkdown(IReadOnlyList<Release> releases)
        {
            // 已经下过的不再重复拉取，因此重复运行是幂等的，也支持增量更新。
            var pending = releases
                .Where(r => !string.IsNullOrEmpty(r.ReleaseNotesUrl))
                .Where(r => !File.Exists(GetNotesPath(r)))
                .ToList();

            Console.WriteLine($"共 {releases.Count} 个版本，其中 {releases.Count - pending.Count} 个已存在，需下载 {pending.Count} 个。");

            if (pending.Count == 0)
                return;

            foreach (Release release in pending)
                Directory.CreateDirectory(Path.GetDirectoryName(GetNotesPath(release)));

            var failures = new List<string>();
            var stopwatch = Stopwatch.StartNew();

            var options = new ProgressBarOptions
            {
                ProgressCharacter = '─',
                ProgressBarOnBottom = true
            };

            using (var progressBar = new ProgressBar(pending.Count, "正在下载发布日志", options))
            {
                object gate = new object();
                int done = 0;

                Parallel.ForEach(
                    pending,
                    new ParallelOptions { MaxDegreeOfParallelism = 12 },
                    release =>
                    {
                        try
                        {
                            byte[] bytes = httpClient.GetByteArrayAsync(release.ReleaseNotesUrl)
                                .GetAwaiter().GetResult();

                            // 先写临时文件再搬过去，避免中途失败留下半截文件被下次运行当成"已存在"。
                            string path = GetNotesPath(release);
                            string temporary = path + ".tmp";
                            File.WriteAllBytes(temporary, MarkdownFixup.ProcessBytes(bytes));
                            File.Move(temporary, path, true);
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

            Console.WriteLine($"下载完成，耗时 {stopwatch.Elapsed:mm\\:ss}。");

            if (failures.Count > 0)
            {
                Console.WriteLine($"有 {failures.Count} 个版本下载失败（重新运行本工具即可重试）：");
                foreach (string failure in failures.Take(20))
                    Console.WriteLine("  " + failure);

                if (failures.Count > 20)
                    Console.WriteLine($"  …… 另有 {failures.Count - 20} 条");
            }
        }

        private string GetNotesPath(Release release)
        {
            return Path.Combine(destinationDirectory, release.DirectoryName, release.FileName);
        }

        private static string Truncate(string value, int length = 400)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= length ? value : value.Substring(0, length) + "…";
        }
    }
}
