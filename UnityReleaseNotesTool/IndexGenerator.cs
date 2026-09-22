using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnityReleaseNotesTool
{
    /// <summary>
    /// 生成索引页与下载页。
    ///
    /// index.html 每个分组下是一张「小版本 × f/p/b/a」矩阵，每个格子是该组合下最新的那个版本，
    /// 没有就留空占位；每组末尾再给一个链接到 index-&lt;组&gt;.html 的完整列表。
    ///
    /// 下载链接指向 download.html?f=...，由那个页面读取 .downloads.json 渲染成可点的链接，
    /// 而不是把 .json 直接甩给浏览器（那样只会触发下载或显示一堆源码）。
    /// </summary>
    public class IndexGenerator
    {
        private const string Title = "Unity发布日志";
        private const string DownloadPageName = "download.html";

        private readonly string destinationDirectory;

        public IndexGenerator(string destinationDirectory)
        {
            this.destinationDirectory = destinationDirectory;
        }

        public void Execute()
        {
            List<string> directories = GetDirectories();

            if (directories.Count == 0)
            {
                Console.WriteLine("没有找到任何含 Markdown 的版本目录，跳过索引生成。");
                return;
            }

            WriteDownloadPage();

            var index = new StringBuilder();

            foreach (string directory in directories)
            {
                List<string> versions = GetVersions(directory);

                WriteDirectoryIndex(directory, versions);
                index.Append(BuildGroupSection(directory, versions));
            }

            File.WriteAllText(Path.Combine(destinationDirectory, "index.html"), Page(Title, index.ToString()),
                new UTF8Encoding(false));

            Console.WriteLine($"已生成 index.html（{directories.Count} 个分组矩阵）、{directories.Count} 个完整列表页和 {DownloadPageName}。");
        }

        /// <summary>一个分组：标题 + 小版本矩阵 + 完整列表入口。</summary>
        private string BuildGroupSection(string directory, List<string> versions)
        {
            var html = new StringBuilder();

            html.AppendFormat("<h2>Unity {0}</h2>\n", directory);

            // minor（如 6000.7）→ 后缀（f/p/b/a）→ 该组合下最新的版本。
            // 3.x / 4.x 的老版本号没有后缀字母，按语义归到 f 列（它们就是当时的正式版）。
            var groups = new Dictionary<string, Dictionary<char, string>>(StringComparer.Ordinal);

            foreach (string version in versions)
            {
                string minor = GetMinor(version);
                char suffix = GetSuffix(version);

                if (!groups.TryGetValue(minor, out Dictionary<char, string> cells))
                {
                    cells = new Dictionary<char, string>();
                    groups[minor] = cells;
                }

                // versions 已按新版本在前排好，所以先到先得就是最新的。但保险起见再比一次。
                if (!cells.TryGetValue(suffix, out string existing)
                    || string.CompareOrdinal(Pad(existing), Pad(version)) < 0)
                    cells[suffix] = version;
            }

            // 整列都没数据的后缀就不显示 —— 例如 Unity 6000 至今没有补丁版（p），
            // 老版本（3.x/4.x）则连 b/a 都没有。留一整列空白只是噪音。
            List<char> columns = Suffixes
                .Where(suffix => groups.Values.Any(cells => cells.ContainsKey(suffix)))
                .ToList();

            html.Append("<table class=\"matrix\">\n<thead><tr><th>版本</th>");

            foreach (char suffix in columns)
                html.AppendFormat("<th>{0}</th>", SuffixLabel(suffix));

            html.Append("</tr></thead>\n<tbody>\n");

            // 小版本新的在前。
            foreach (string minor in groups.Keys.OrderBy(m => m, new MinorComparer()).Reverse())
            {
                Dictionary<char, string> cells = groups[minor];
                html.AppendFormat("<tr><th>{0}</th>", minor);

                foreach (char suffix in columns)
                {
                    html.Append("<td>");

                    if (cells.TryGetValue(suffix, out string version))
                        html.Append(VersionCell(directory, version));

                    html.Append("</td>");
                }

                html.Append("</tr>\n");
            }

            html.Append("</tbody></table>\n");
            html.AppendFormat("<p class=\"more\"><a href=\"index-{0}.html\">完整列表（{1} 个版本）</a></p>\n",
                directory, versions.Count);

            return html.ToString();
        }

        private string VersionCell(string directory, string version)
        {
            string path = $"{directory}/{version}";

            var cell = new StringBuilder();
            cell.AppendFormat("<a href=\"{0}.md\">{1}</a>", path, version);

            if (File.Exists(Path.Combine(destinationDirectory, directory, version + ".downloads.json")))
            {
                cell.AppendFormat(
                    " <a class=\"dl\" href=\"{0}?f={1}.downloads.json\" target=\"_blank\" rel=\"noopener\">下载</a>",
                    DownloadPageName, path);
            }

            return cell.ToString();
        }

        private static readonly char[] Suffixes = { 'f', 'p', 'b', 'a' };

        private static string SuffixLabel(char suffix)
        {
            switch (suffix)
            {
                case 'f': return "f 正式版";
                case 'p': return "p 补丁版";
                case 'b': return "b Beta";
                case 'a': return "a Alpha";
                default: return suffix.ToString();
            }
        }

        /// <summary>把 6000.7.0b1 缩成 6000.7（小版本）；3.5.7 这种取前两段。</summary>
        public static string GetMinor(string version)
        {
            string[] parts = version.Split('.');

            return parts.Length >= 2 ? parts[0] + "." + parts[1] : version;
        }

        /// <summary>
        /// 取发布类型后缀。老版本号（3.4.1、5.0）没有后缀字母，归为 f —— 它们就是当时的正式版。
        /// </summary>
        public static char GetSuffix(string version)
        {
            for (int i = version.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(version[i]))
                    continue;

                char c = char.ToLowerInvariant(version[i]);

                return "abfp".IndexOf(c) >= 0 ? c : 'f';
            }

            return 'f';
        }

        /// <summary>数字补零，用于比较版本先后。</summary>
        private static string Pad(string version)
        {
            return System.Text.RegularExpressions.Regex.Replace(version, @"\d+", m => m.Value.PadLeft(6, '0'));
        }

        private class MinorComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return string.CompareOrdinal(Pad(x), Pad(y));
            }
        }

        private void WriteDirectoryIndex(string directory, List<string> versions)
        {
            var body = new StringBuilder();
            body.AppendFormat("<h2>Unity {0}</h2>\n", directory);

            foreach (string version in versions)
            {
                body.AppendFormat("<p><a href=\"{0}/{1}.md\">Unity {1}</a>", directory, version);

                if (File.Exists(Path.Combine(destinationDirectory, directory, version + ".downloads.json")))
                {
                    body.AppendFormat(
                        " · <a href=\"{0}?f={1}/{2}.downloads.json\" target=\"_blank\" rel=\"noopener\">下载</a>",
                        DownloadPageName, directory, version);
                }

                body.Append("</p>\n");
            }

            body.Append("<p><a href=\"index.html\">返回目录</a></p>\n");

            File.WriteAllText(Path.Combine(destinationDirectory, $"index-{directory}.html"), Page(directory, body.ToString()),
                new UTF8Encoding(false));
        }

        /// <summary>
        /// 下载页。GitHub Pages 是纯静态的，所以这里在浏览器端 fetch 对应的 .downloads.json 再渲染。
        /// 参数只接受站内相对路径，避免被塞进任意 URL。
        /// </summary>
        private void WriteDownloadPage()
        {
            File.WriteAllText(Path.Combine(destinationDirectory, DownloadPageName), DownloadPageHtml,
                new UTF8Encoding(false));
        }

        private const string DownloadPageHtml = @"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>下载 - Unity发布日志</title>
<style>
  body { font-family: system-ui, -apple-system, ""Segoe UI"", sans-serif; line-height: 1.6;
         max-width: 60rem; margin: 0 auto; padding: 1.5rem; color: #1a1a1a; }
  h1 { font-size: 1.4rem; margin: 0 0 .25rem; }
  h2 { font-size: 1.1rem; margin: 1.75rem 0 .5rem; border-bottom: 1px solid #ddd; padding-bottom: .25rem; }
  h3 { font-size: .95rem; margin: 1rem 0 .35rem; color: #444; }
  a { color: #0b5cad; }
  .meta { color: #666; font-size: .9rem; margin-bottom: 1rem; }
  .meta code { background: #f2f2f2; padding: .1rem .3rem; border-radius: 3px; }
  ul { margin: .25rem 0; padding-left: 1.25rem; }
  li { margin: .15rem 0; }
  .size { color: #888; font-size: .85rem; }
  .err { color: #b00020; }
  @media (prefers-color-scheme: dark) {
    body { background: #16181d; color: #e6e6e6; }
    h2 { border-color: #3a3f47; } h3 { color: #b8b8b8; }
    a { color: #6cb6ff; } .meta { color: #999; }
    .meta code { background: #2a2e35; } .size { color: #888; }
  }
</style>
</head>
<body>
<div id=""content"">加载中…</div>
<script>
(function () {
  var out = document.getElementById('content');
  var file = new URLSearchParams(location.search).get('f');

  // 只允许站内相对路径：字母数字、点、下划线、连字符和斜杠，且不含 ..
  if (!file || !/^[A-Za-z0-9._\/-]+$/.test(file) || file.indexOf('..') !== -1) {
    out.innerHTML = '<p class=""err"">参数 f 无效。</p>';
    return;
  }

  function el(tag, text, cls) {
    var e = document.createElement(tag);
    if (text !== undefined && text !== null) e.textContent = text;   // 一律用 textContent，避免注入
    if (cls) e.className = cls;
    return e;
  }

  function humanSize(bytes) {
    if (!bytes) return '';
    var units = ['B', 'KB', 'MB', 'GB', 'TB'], i = 0, n = bytes;
    while (n >= 1024 && i < units.length - 1) { n /= 1024; i++; }
    return n.toFixed(i === 0 ? 0 : 1) + ' ' + units[i];
  }

  fetch(file).then(function (r) {
    if (!r.ok) throw new Error('HTTP ' + r.status);
    return r.json();
  }).then(function (data) {
    out.textContent = '';

    out.appendChild(el('h1', 'Unity ' + (data.version || '')));

    var meta = el('div', null, 'meta');
    if (data.revision) {
      meta.appendChild(el('span', 'Revision: '));
      meta.appendChild(el('code', data.revision));
      meta.appendChild(el('span', '　'));
    }
    if (data.releaseDate) meta.appendChild(el('span', '发布：' + data.releaseDate.slice(0, 10)));
    out.appendChild(meta);

    if (data.unityHubDeepLink) {
      var p = el('p');
      p.appendChild(el('span', '通过 Unity Hub 安装：'));
      var a = el('a', data.unityHubDeepLink);
      a.href = data.unityHubDeepLink;
      p.appendChild(a);
      out.appendChild(p);
    }

    var downloads = data.downloads || [];
    if (!downloads.length) {
      out.appendChild(el('p', '这一版官方没有提供直接下载（只有上面的 Hub 链接）。'));
    }

    downloads.forEach(function (d) {
      out.appendChild(el('h2', [d.platform, d.architecture].filter(Boolean).join(' / ')));

      var top = el('p');
      var link = el('a', d.url ? d.url.split('/').pop() : '编辑器安装包');
      link.href = d.url;
      top.appendChild(link);
      if (d.installerSize) top.appendChild(el('span', '  ' + humanSize(d.installerSize), 'size'));
      out.appendChild(top);

      var modules = d.modules || [];
      if (modules.length) {
        out.appendChild(el('h3', '模块 / 组件'));
        var ul = el('ul');
        modules.forEach(function (m) {
          var li = el('li');
          var a = el('a', m.name || m.url);
          a.href = m.url;
          li.appendChild(a);
          if (m.category) li.appendChild(el('span', '  ' + m.category, 'size'));
          ul.appendChild(li);
        });
        out.appendChild(ul);
      }
    });

    var notices = data.thirdPartyNotices || [];
    if (notices.length) {
      out.appendChild(el('h2', '第三方声明'));
      var ul2 = el('ul');
      notices.forEach(function (n) {
        var li = el('li');
        var a = el('a', n.name || n.url);
        a.href = n.url;
        li.appendChild(a);
        ul2.appendChild(li);
      });
      out.appendChild(ul2);
    }
  }).catch(function (e) {
    out.textContent = '';
    var p = el('p', '读取下载清单失败：' + e.message, 'err');
    out.appendChild(p);
  });
})();
</script>
</body>
</html>
";

        private List<string> GetDirectories()
        {
            if (!Directory.Exists(destinationDirectory))
                return new List<string>();

            return Directory.EnumerateDirectories(destinationDirectory)
                .Select(Path.GetFileName)
                .Where(name => int.TryParse(name, out _))
                .Where(name => Directory.EnumerateFiles(Path.Combine(destinationDirectory, name), "*.md").Any())
                .OrderByDescending(name => int.Parse(name))
                .ToList();
        }

        private List<string> GetVersions(string directory)
        {
            return Directory
                .EnumerateFiles(Path.Combine(destinationDirectory, directory), "*.md")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderByAlphaNumeric(v => v)
                .ToList();
        }

        private static string Page(string group, string body)
        {
            string heading = group == Title ? Title : $"Unity {group} 发布日志";

            return $@"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>{heading}</title>
    <style>
      body {{ font-family: system-ui, -apple-system, ""Segoe UI"", sans-serif; line-height: 1.6;
             max-width: 60rem; margin: 0 auto; padding: 1.5rem; color: #1a1a1a; }}
      h1 {{ font-size: 1.5rem; }}
      h2 {{ font-size: 1.15rem; margin: 1.75rem 0 .5rem; border-bottom: 1px solid #ddd; padding-bottom: .25rem; }}
      a {{ color: #0b5cad; }}
      table.matrix {{ border-collapse: collapse; width: 100%; margin: .5rem 0; font-size: .92rem; }}
      table.matrix th, table.matrix td {{ border: 1px solid #e0e0e0; padding: .35rem .5rem; text-align: left;
                                          vertical-align: top; white-space: nowrap; }}
      table.matrix thead th {{ background: #f6f6f6; font-weight: 600; }}
      table.matrix tbody th {{ background: #fafafa; font-weight: 600; }}
      a.dl {{ font-size: .8rem; color: #0a7a3f; }}
      p.more {{ margin: .5rem 0 0; font-size: .9rem; }}
      @media (prefers-color-scheme: dark) {{
        body {{ background: #16181d; color: #e6e6e6; }}
        h2 {{ border-color: #3a3f47; }}
        a {{ color: #6cb6ff; }}
        table.matrix th, table.matrix td {{ border-color: #3a3f47; }}
        table.matrix thead th, table.matrix tbody th {{ background: #22262d; }}
        a.dl {{ color: #5fd08a; }}
      }}
    </style>
</head>
<body>
<h1>{heading}</h1>
{body}</body>
</html>
";
        }
    }
}
