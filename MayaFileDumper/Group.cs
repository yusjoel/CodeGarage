using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MayaFileDumper
{
    /// <summary>
    /// </summary>
    public class Group : BaseNode
    {
        public List<BaseNode> Children = new List<BaseNode>();

        public Group(string groupType, int alignment, int tab)
        {
            GroupType = groupType;
            Alignment = alignment;
            Tab = tab;
        }

        /// <summary>
        ///     4字节的字符串, 代表四种组类型
        ///     FORM, CAT , LIST, PROP
        ///     最后一个字节会替换为字节数, 如FOR4, 代表4字节对齐, FOR8代表8字节对齐
        /// </summary>
        public string GroupType { get; }

        public void Read(BinaryReader binaryReader)
        {
            // 读取完GroupType的4字节, 需要字节对齐, 补齐\0
            for (var i = 4; i < Alignment; i++)
                binaryReader.ReadByte();

            // 读取数据大小, 大尾的数值, 大小即为Alignment
            for (var i = 0; i < Alignment; i++)
            {
                var b = binaryReader.ReadByte();
                Size = (Size << 8) + b;
            }

            // 读取Group Name, 不需要对齐
            Name = ReadString(binaryReader);

            //Console.WriteLine($"{MakeTab()}{GroupType}: {Name} {Size:X8}");

            // 读取子节点, 可以是Group也可以是Node
            ReadChildren(binaryReader);
        }

        /// <summary>
        ///     判断是否是GroupType, 并且获取对齐字节数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        private static bool IsGroupType(string name, out int alignment)
        {
            var isGroupType = false;
            alignment = 2;
            if (name == "FOR4" || name == "CAT4" || name == "LIS4" || name == "PRO4")
            {
                alignment = 4;
                isGroupType = true;
            }
            else if (name == "FOR8" || name == "CAT8" || name == "LIS8" || name == "PRO8")
            {
                alignment = 8;
                isGroupType = true;
            }

            return isGroupType;
        }

        /// <summary>
        ///     读取4字节字符串
        /// </summary>
        /// <param name="binaryReader"></param>
        /// <returns></returns>
        private static string ReadString(BinaryReader binaryReader)
        {
            var bytes = binaryReader.ReadBytes(4);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        ///     读取子节点
        /// </summary>
        /// <param name="binaryReader"></param>
        public void ReadChildren(BinaryReader binaryReader)
        {
            var currentPosition = binaryReader.BaseStream.Position;
            while (binaryReader.BaseStream.Position < currentPosition + Size - 4)
            {
                var name = ReadString(binaryReader);
                if (IsGroupType(name, out var padding))
                {
                    var group = new Group(name, padding, Tab + 1);
                    group.Read(binaryReader);
                    Children.Add(group);
                }
                else
                {
                    var node = new Node(name, Alignment, Tab + 1);
                    node.Read(binaryReader);
                    Children.Add(node);
                }
            }

            // 不需要字节对齐
            //{
            //    int alignment = Alignment - (int)(Size % Alignment);
            //    if (alignment < Alignment)
            //        binaryReader.BaseStream.Seek(alignment, SeekOrigin.Current);
            //}
        }

        public override void Stats(Stats stats)
        {
            stats.AddGroupSize(Name, Size);
            foreach (var baseNode in Children)
                baseNode.Stats(stats);
        }

        /// <summary>
        ///     读取根节点, 必然是一个Group类型
        /// </summary>
        /// <param name="binaryReader"></param>
        /// <returns></returns>
        public static Group ReadRootGroup(BinaryReader binaryReader)
        {
            Group group = null;
            var groupType = ReadString(binaryReader);
            if (IsGroupType(groupType, out var padding))
                group = new Group(groupType, padding, 0);

            return group;
        }
    }
}
