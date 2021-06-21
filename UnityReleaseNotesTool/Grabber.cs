using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace UnityReleaseNotesTool
{
    public class Grabber
    {
        private readonly string destinationDirectory;

        private readonly string indexUrl;

        private readonly List<string> whatsNewUrls = new List<string>();

        public Grabber(string indexUrl, string destinationDirectory)
        {
            this.indexUrl = indexUrl;
            this.destinationDirectory = destinationDirectory;
        }

        private void Download(string uriString, string path)
        {
            Stream responseStream = null;

            try
            {
                var uri = new Uri(uriString);
                var webRequest = WebRequest.Create(uri);
                responseStream = webRequest.GetResponse().GetResponseStream();
                if (responseStream == null)
                    return;

                using var streamReader = new StreamReader(responseStream);
                string contents = streamReader.ReadToEnd();
                File.WriteAllText(path, contents);
            }
            catch (Exception e)
            {
                Console.Write(e);
            }
            finally
            {
                responseStream?.Close();
            }
        }

        public void Execute()
        {
            RetrieveAllUrls();
            DownloadAllHtml();
        }

        private void DownloadAllHtml()
        {
            ProgressBar progressBar = new ProgressBar(whatsNewUrls.Count, "正在抓取所有的版本的信息");
            for (int i = 0; i < whatsNewUrls.Count; i++)
            {
                string whatsNewUrl = whatsNewUrls[i];
                progressBar.Tick($"{whatsNewUrl} {i + 1}/{whatsNewUrls.Count}");
                //Console.WriteLine("{0}/{1}", i + 1, whatsNewUrls.Count);

                int lastIndex = whatsNewUrl.LastIndexOf('/');
                string fileName = whatsNewUrl.Substring(lastIndex + 1) + ".html";
                string path = Path.Combine(destinationDirectory, fileName);
                Download(whatsNewUrl, path);
            }
        }

        public void RetrieveAllUrls()
        {
            string path = Path.Combine(destinationDirectory, "index.html");
            Download(indexUrl, path);

            if (!File.Exists(path))
            {
                Console.WriteLine("index.html not found");
                return;
            }

            using (var streamReader = new StreamReader(File.OpenRead(path)))
            {
                string line = streamReader.ReadLine();
                bool optionsStart = false;
                while (line != null)
                {
                    if (!optionsStart)
                    {
                        if (line.Contains("<ul class=\"options\">"))
                            optionsStart = true;
                    }
                    else
                    {
                        if (line.Contains("Archive")) break;

                        int index1 = line.IndexOf("href=\"", StringComparison.Ordinal) + "href=\"".Length;
                        int index2 = line.IndexOf("\">Unity", StringComparison.Ordinal);
                        string url = "https://unity3d.com" + line.Substring(index1, index2 - index1);
                        whatsNewUrls.Add(url);
                    }
                    line = streamReader.ReadLine();
                }
            }

            File.Delete(path);
        }
    }
}
