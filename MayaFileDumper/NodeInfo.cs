namespace MayaFileDumper
{
    public class NodeInfo
    {
        public string Name;

        public long Size;

        public NodeInfo(string name, long size)
        {
            Name = name;
            Size = size;
        }
    }
}