using System.IO;

namespace MayaFileDumper
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var mayaModelPath = @"qixi.mb";

            using var binaryReader = new BinaryReader(File.OpenRead(mayaModelPath));
            var root = Group.ReadRootGroup(binaryReader);
            root.Read(binaryReader);

            var stats = new Stats(binaryReader.BaseStream.Length);
            root.Stats(stats);
            stats.Print();
        }
    }
}
