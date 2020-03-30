using System;
using System.IO;

namespace Translator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var translator = new Translator();

            if (!Directory.Exists("dictionary"))
            {
                Console.WriteLine("Error: Not found directory <dictionary>.");
                Environment.Exit(-1);
            }

            var files = Directory.GetFiles("dictionary", "*.csv", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                Console.WriteLine("Error: Not found csv file under directory <dictionary>.");
                Environment.Exit(-2);
            }

            foreach (string file in files) translator.Load(file);
            translator.Sort();

            if (args.Length == 0)
            {
                Console.WriteLine("Error: Missing target text file.");
                Console.WriteLine("Usage: Translator <path>");
                Environment.Exit(-3);
            }

            string targetPath = args[0];
            if (!File.Exists(targetPath))
            {
                Console.WriteLine("Error: Not found file {0}", targetPath);
                Console.WriteLine("Usage: Translator <path>");
                Environment.Exit(-4);
            }

            translator.Translate(targetPath);
        }
    }
}
