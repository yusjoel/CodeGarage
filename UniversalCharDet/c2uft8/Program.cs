using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mozilla.NUniversalCharDet;
using System.IO;

namespace c2utf8
{
    class Program
    {
        static void help()
        {
            Console.WriteLine("c2utf8 = convert to utf-8 file");
            Console.WriteLine("Usage: c2utf8 path pattern");
            Console.WriteLine("Example: c2utf8 c:/scripts *.cs");
            Console.WriteLine();
        }

        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                help();
                return -1;
            }

            string path = args[0];
            if (!Directory.Exists(path))
            {
                help();
                Console.WriteLine("Error: Directory {0} not exists.", path);
                return -1;
            }

            string searchPattern = args[1];
            string[] files = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                convert(file);
            }

            return 0;
        }

        static void convert(string file)
        {
            string stringEncoded = null;
            using (BinaryReader br = new BinaryReader(File.OpenRead(file)))
            {
                int length = (int)br.BaseStream.Length;
                byte[] buffer = br.ReadBytes(length);
                UniversalDetector uDetecter = new UniversalDetector(null);

                uDetecter.HandleData(buffer, 0, length);

                uDetecter.DataEnd();
                string detectedCharset = uDetecter.GetDetectedCharset();
                if (string.IsNullOrEmpty(detectedCharset))
                {
                    Console.WriteLine("Warning: {0} not detected", file);
                }
                else
                {
                    Console.WriteLine("Detected: {0} - {1}", file, detectedCharset);
                    if (detectedCharset != "UTF-8")
                    {
                        Encoding encoding = Encoding.GetEncoding(detectedCharset);
                        stringEncoded = encoding.GetString(buffer);
                    }
                }
            }

            if (!string.IsNullOrEmpty(stringEncoded))
            {
                using(StreamWriter sw = new StreamWriter(File.Open(file, FileMode.Create), Encoding.UTF8))
                {
                    sw.Write(stringEncoded);
                }
            }

        }
    }
}
