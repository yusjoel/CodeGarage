using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityReleaseNotesTool
{
    public class Parser
    {
        private readonly string destinationDirectory;

        private readonly string sourceDirectory;

        public Parser(string sourceDirectory, string destinationDirectory)
        {
            this.sourceDirectory = sourceDirectory;
            this.destinationDirectory = destinationDirectory;
        }

        public void Execute()
        {
            var files = Directory.GetFiles(sourceDirectory, "*.html");

            var versionNames = new List<string>();

            foreach (string sourceFilePath in files)
            {
                // 版本号, 默认取文件名, 但是文件名不规范, 有2020.1.4.html, unity-2018.1.9f2.html
                string versionName = Path.GetFileNameWithoutExtension(sourceFilePath);
                versionName = versionName.Replace("unity-", "");
                string estimatedVersionName = versionName;

                string year;
                if (versionName.StartsWith("3."))
                {
                    year = "\\3\\";
                }
                else if (versionName.StartsWith("4."))
                {
                    year = "\\4\\";
                }
                else if (versionName.StartsWith("5."))
                {
                    year = "\\5\\";
                }
                else if (versionName.Contains("2017"))
                {
                    year = "\\2017\\";
                }
                else if (versionName.Contains("2018"))
                {
                    year = "\\2018\\";
                }
                else if (versionName.Contains("2019"))
                {
                    year = "\\2019\\";
                }
                else if (versionName.Contains("2020"))
                {
                    year = "\\2020\\";
                }
                else if (versionName.Contains("2021"))
                {
                    year = "\\2021\\";
                }
                else
                {
                    Console.WriteLine("Unknown version " + versionName);
                    continue;
                }

                string destinationPath = destinationDirectory + year + estimatedVersionName + ".html";
                string yearFolder = destinationDirectory + year;
                if (!Directory.Exists(yearFolder))
                    Directory.CreateDirectory(yearFolder);

                using (var streamReader = new StreamReader(File.OpenRead(sourceFilePath)))
                using (var streamWriter = new StreamWriter(File.Open(destinationPath, FileMode.Create)))
                {
                    string line = streamReader.ReadLine();

                    streamWriter.WriteLine("<html>");
                    streamWriter.WriteLine("<head>");
                    // found title
                    while (line != null && !line.Contains("<title>"))
                        line = streamReader.ReadLine();

                    streamWriter.WriteLine(line);
                    streamWriter.WriteLine("</head>");

                    streamWriter.WriteLine("<body>");

                    // found <div class=\"g9 nest flex-column\">
                    while (line != null && !line.Contains("<div class=\"g9 nest flex-column\">"))
                        line = streamReader.ReadLine();

                    if (line != null)
                        // until <div class=\"g3 right\">
                        do
                        {
                            streamWriter.WriteLine(line);

                            if (line.Contains("Android Target Support"))
                            {
                                string pattern = @"Editor-([\d|\.|f|p|b]*)exe";
                                var regexOptions = RegexOptions.None;
                                var regex = new Regex(pattern, regexOptions);
                                var match = regex.Match(line);
                                {
                                    if (match.Success)
                                    {
                                        // 2018.3.0f2.
                                        versionName = match.Groups[1].Value;
                                        // 移除最后的.
                                        versionName = versionName.Substring(0, versionName.Length - 1);
                                    }
                                }
                            }

                            line = streamReader.ReadLine();
                        } while (line != null && !line.Contains("<div class=\"g3 right\">"));

                    streamWriter.WriteLine("</body>");
                    streamWriter.WriteLine("</html>");
                }

                if (versionName != estimatedVersionName)
                {
                    //Console.WriteLine($"{estimatedVersionName} => {versionName}");
                    string newPath = destinationDirectory + year + versionName + ".html";
                    if (File.Exists(newPath))
                        File.Delete(newPath);
                    File.Move(destinationPath, newPath);
                    destinationPath = newPath;
                }

                versionNames.Add(versionName);

                //var converter = new Converter();
                //string markdown = converter.ConvertFile(destinationPath);
                //string markdownPath = destinationFolder + year + versionName + ".md";
                //File.WriteAllText(markdownPath, markdown);
            }

            var html = new StringBuilder();
            html.Append(@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <title>Unity发布日志</title>
</head>
<body>
");
            versionNames = versionNames.OrderByAlphaNumeric(s => s).ToList();
            string lastYear = "";
            foreach (string versionName in versionNames)
            {
                string year;
                if (versionName.StartsWith("3.")) year = "3";
                else if (versionName.StartsWith("4.")) year = "4";
                else if (versionName.StartsWith("5.")) year = "5";
                else if (versionName.Contains("2017")) year = "2017";
                else if (versionName.Contains("2018")) year = "2018";
                else if (versionName.Contains("2019")) year = "2019";
                else if (versionName.Contains("2020")) year = "2020";
                else if (versionName.Contains("2021")) year = "2021";
                else year = "";

                if (year != lastYear)
                {
                    html.AppendFormat("<h1>Unity {0}</h1>\n", year);
                    lastYear = year;
                }
                html.AppendFormat("<p><a href=\"{0}/{1}.html\">Unity {1}</a></p>\n", year, versionName);
            }
            html.AppendLine("</body>");

            File.WriteAllText(Path.Combine(destinationDirectory, "index.html"), html.ToString());
        }
    }
}
