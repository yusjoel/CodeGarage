using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Translator
{
    public class Translator
    {
        private class Entry
        {
            /// <summary>
            /// 特殊符号 格式如##nnn##
            /// </summary>
            public string SpecialText;
            /// <summary>
            /// 原文本
            /// </summary>
            public string RawText;
            /// <summary>
            /// 翻译文本
            /// </summary>
            public string TranslatedText;

            /// <summary>
            /// 是否有空格 (是不是词组)
            /// </summary>
            public bool HasSpace;
        }

        private List<Entry> dictionnary = new List<Entry>();

        public void Sort()
        {
            foreach (var entry in dictionnary)
            {
                entry.HasSpace = entry.RawText.Contains(" ");
            }


            dictionnary.Sort(Comparison);


            for (int i = 0; i < dictionnary.Count; i++)
            {
                dictionnary[i].SpecialText = $"##{i}##";
            }
        }

        private int Comparison(Entry x, Entry y)
        {
            // 1. 词组优先, 即Body Slam排在Slam前面
            // 2. 逆序排列, 即Flying排在Fly前面
            if(x.HasSpace && y.HasSpace || !x.HasSpace && !y.HasSpace)
                return string.Compare(y.RawText, x.RawText, StringComparison.Ordinal);

            if (x.HasSpace)
                return -1;

            return 1;
        }

        public void Load(string path)
        {
            if (!File.Exists(path))
                return;

            using (var streamReader = new StreamReader(File.OpenRead(path)))
            {
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        continue;
                    var subStrings = line.Split(',');
                    AddEntry(subStrings);
                }
            }
        }

        private void AddEntry(string[] subStrings)
        {
            if (subStrings.Length != 2)
                return;

            string rawText = subStrings[0].Trim();
            string translatedText = subStrings[1].Trim();
            var entry = new Entry
            {
                RawText = rawText,
                TranslatedText = translatedText
            };
            dictionnary.Add(entry);

            if (rawText == rawText.ToUpper()) return;
            // 加一套全大写的
            entry = new Entry
            {
                RawText = rawText.ToUpper(),
                TranslatedText = translatedText
            };
            dictionnary.Add(entry);
        }

        public void Translate(string path)
        {
            if (!File.Exists(path))
                return;

            string filename = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(filename))
                return;

            string newPath = path.Replace(filename, filename + "(cn)");
            using (var streamReader = new StreamReader(File.OpenRead(path)))
            using (var streamWriter = new StreamWriter(File.Open(newPath, FileMode.Create), Encoding.UTF8))
            {
                streamWriter.NewLine = "\r\n";
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();
                    string tranlatedLine = TranslateText(line);
                    streamWriter.WriteLine(tranlatedLine);
                }
            }
        }

        /// <summary>
        /// 根据字典进行翻译
        /// </summary>
        /// <param name="rawText"></param>
        /// <returns></returns>
        private string TranslateText(string rawText)
        {
            // 现将查找的文字替换成{#nnn}之类的符号
            // 最后将{#nnn}替换成翻译后的文字
            // 如果直接替换会出现以下的情况:
            // Body Slam => 泰山压顶(Body Slam) => 泰山压顶(Body 摔打(Slam))
            foreach (var entry in dictionnary)
            {
                rawText = rawText.Replace(entry.RawText, entry.SpecialText);
            }

            foreach (var entry in dictionnary)
            {
                rawText = rawText.Replace(entry.SpecialText, entry.TranslatedText);
            }

            return rawText;
        }

    }
}
