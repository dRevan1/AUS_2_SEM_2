using SEM_2_CORE;
using SEM_2_CORE.Interfaces;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for PersonFileContent.xaml
    /// </summary>
    public partial class FileContent : Window
    {
        public List<BlockViewData> Blocks { get; set; } = new List<BlockViewData>();
        public string Path { get; set; } = "";
        public int BlockSize { get; set; } = 0;
        public int BlockFactor { get; set; } = 0;
        public int BlockCount { get; set; } = 0;
        public FileContent(List<BlockViewData> blocks, string path, int blockSize, int blockFactor, int blockCount)
        {
            InitializeComponent();

            Blocks = blocks;
            Path = path;
            BlockSize = blockSize;
            BlockFactor = blockFactor;
            BlockCount = blockCount;
            DataContext = this;
        }
    }
}
