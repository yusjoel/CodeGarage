using System;
using System.Collections.Generic;
using System.Linq;

namespace MayaFileDumper
{
    /// <summary>
    ///     统计
    /// </summary>
    public class Stats
    {
        private readonly Dictionary<string, NodeInfo> groupSizeMap = new Dictionary<string, NodeInfo>();

        private readonly Dictionary<string, NodeInfo> nodeSizeMap = new Dictionary<string, NodeInfo>();

        private long fileSize;

        public Stats(long fileSize)
        {
            this.fileSize = fileSize;
        }

        public void AddGroupSize(string groupName, long size)
        {
            if (groupSizeMap.TryGetValue(groupName, out var nodeInfo))
                nodeInfo.Size += size;
            else
                groupSizeMap[groupName] = new NodeInfo(groupName, size);
        }

        public void AddNodeSize(string nodeName, long size)
        {
            if (nodeSizeMap.TryGetValue(nodeName, out var nodeInfo))
                nodeInfo.Size += size;
            else
                nodeSizeMap[nodeName] = new NodeInfo(nodeName, size);
        }

        private int Compare(NodeInfo a, NodeInfo b)
        {
            return -a.Size.CompareTo(b.Size);
        }

        public void Print()
        {
            var groupList = groupSizeMap.Values.ToList();
            groupList.Sort(Compare);

            Console.WriteLine($"File Size: {fileSize:X8}");

            Console.WriteLine("Group:");
            foreach (var groupInfo in groupList)
                Console.WriteLine($"{groupInfo.Name}: {groupInfo.Size:X8} {groupInfo.Size * 100.0f / fileSize:0.00}%");

            var nodeList = nodeSizeMap.Values.ToList();
            nodeList.Sort(Compare);

            Console.WriteLine("\nNode:");
            foreach (var nodeInfo in nodeList)
                Console.WriteLine($"{nodeInfo.Name}: {nodeInfo.Size:X8} {nodeInfo.Size * 100.0f / fileSize:0.00}%");
        }
    }
}
