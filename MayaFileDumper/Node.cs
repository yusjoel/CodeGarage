using System.IO;

namespace MayaFileDumper
{
    /// <summary>
    ///     数据节点
    /// </summary>
    public class Node : BaseNode
    {
        public byte[] Data;

        public Node(string name, int alignment, int tab)
        {
            Name = name;
            Alignment = alignment;
            Tab = tab;
        }

        public void Read(BinaryReader binaryReader)
        {
            // 读取完Node Name的4字节, 需要字节对齐, 补齐\0
            for (var i = 4; i < Alignment; i++)
                binaryReader.ReadByte();

            // 读取数据大小, 大尾的数值, 大小即为Alignment
            for (var i = 0; i < Alignment; i++)
            {
                var b = binaryReader.ReadByte();
                Size = (Size << 8) + b;
            }

            //Console.WriteLine($"{MakeTab()}Node: {Name} {Size:X8}");

            // 读取数据, 数据类型和Node Name有关, 并没有其他字段来表明
            Data = binaryReader.ReadBytes((int)Size);

            // 需要对齐数据
            var padding = Alignment - (int)(Size % Alignment);
            if (padding < Alignment)
                binaryReader.BaseStream.Seek(padding, SeekOrigin.Current);
        }

        public override void Stats(Stats stats)
        {
            stats.AddNodeSize(Name, Size);
        }
    }
}
