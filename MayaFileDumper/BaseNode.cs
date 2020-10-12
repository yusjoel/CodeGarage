namespace MayaFileDumper
{
    public class BaseNode
    {
        /// <summary>
        ///     名称
        /// </summary>
        public string Name;

        /// <summary>
        ///     对齐字节数
        /// </summary>
        public int Alignment;

        /// <summary>
        ///     数据大小 (字节数)
        /// </summary>
        public long Size;

        /// <summary>
        /// 缩进
        /// </summary>
        public int Tab;

        protected string MakeTab()
        {
            string tab = "";
            for (int i = 0; i < Tab; i++)
            {
                tab += "  ";
            }

            return tab;
        }

        public virtual void Stats(Stats stats)
        {

        }
    }
}
