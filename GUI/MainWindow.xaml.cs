using SEM_2_CORE;
using SEM_2_CORE.Testers;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<HeapFile<Person>> HeapFile = new List<HeapFile<Person>>();
        public MainWindow()
        {
            Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
            HeapFile.Add(new HeapFile<Person>("test.bin", 363, dataInstance));
            InitializeComponent();
        }


        private void PopulateBtn_Click(object sender, RoutedEventArgs e)
        {
            HeapFileTester tester = new HeapFileTester();
            tester.InsertData(HeapFile[0], 10);
            MessageBox.Show("File(s) has/have been populated.");
        }

        private void ViewContentBtn_Click(object sender, RoutedEventArgs e)
        {
            Person dataInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
            for (int i = 0; i < HeapFile.Count; i++)
            {
                PersonFileContent contentWindow = new PersonFileContent(HeapFile[i], HeapFile[i].GetFileContents<Block<Person>>(dataInstance));
                contentWindow.Show();
            }
        }
    }
}