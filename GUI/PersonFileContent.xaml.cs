using SEM_2_CORE;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for PersonFileContent.xaml
    /// </summary>
    public partial class PersonFileContent : Window
    {
        public List<BlockViewData> Blocks { get; set; } = new List<BlockViewData>();
        public string Path { get; set; } = "";
        public int BlockSize { get; set; } = 0;
        public int BlockFactor { get; set; } = 0;
        public int BlockCount { get; set; } = 0;
        public PersonFileContent(HeapFile<Person> heapFile, List<Block<Person>> blocks)
        {
            InitializeComponent();
            for (int i = 0; i < blocks.Count; i++)
            {
                Blocks.Add(new BlockViewData(i, blocks[i].RecordsCount, blocks[i].ValidCount, blocks[i].ToString()));
            }
            Path = heapFile.FilePath;
            BlockSize = heapFile.BlockSize;
            BlockFactor = heapFile.BlockFactor;
            BlockCount = Blocks.Count;
            DataContext = this;
        }
    }
}
