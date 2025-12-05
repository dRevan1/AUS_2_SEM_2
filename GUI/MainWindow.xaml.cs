using SEM_2_CORE;
using SEM_2_CORE.App;
using SEM_2_CORE.Data_classes;
using SEM_2_CORE.Files;
using SEM_2_CORE.Interfaces;
using SEM_2_CORE.Testers;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PCRTestDatabase database;
        public MainWindow()
        {
            database = new PCRTestDatabase(4, "people.bin", "people_overflow.bin", 500, 220, 4, "test.bin", "test_overflow.bin", 500, 220);
            InitializeComponent();
        }

        private void PopulateBtn_Click(object sender, RoutedEventArgs e)
        {
            LinearHashFileTester tester = new LinearHashFileTester();
            
            MessageBox.Show("File(s) has/have been populated.");
        }

        private void ViewContentBtn_Click(object sender, RoutedEventArgs e)
        {
            Person personInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
            PCRTest testInstance = new PCRTest(1, 1, 2025, 1, 1, "0", 0, false, 0.0, "empty");

            List<BlockViewData> viewData = database.GetBlockViewData<PrimaryHashBlock<Person>, Person>(database.peopleFile.PrimaryFile, personInstance);
            FileContent contentWindow = new FileContent(viewData, database.peopleFile.PrimaryFile.FilePath, database.peopleFile.PrimaryFile.BlockSize, database.peopleFile.PrimaryFile.BlockFactor, viewData.Count);
            contentWindow.Show();

            viewData = database.GetBlockViewData<OverflowHashBlock<Person>, Person>(database.peopleFile.OverflowFile, personInstance);
            FileContent contentWindow2 = new FileContent(viewData, database.peopleFile.OverflowFile.FilePath, database.peopleFile.OverflowFile.BlockSize, database.peopleFile.OverflowFile.BlockFactor, viewData.Count);
            contentWindow2.Show();

            viewData = database.GetBlockViewData<PrimaryHashBlock<PCRTest>, PCRTest>(database.pcrTestFile.PrimaryFile, testInstance);
            FileContent contentWindow3 = new FileContent(viewData, database.pcrTestFile.PrimaryFile.FilePath, database.pcrTestFile.PrimaryFile.BlockSize, database.pcrTestFile.PrimaryFile.BlockFactor, viewData.Count);
            contentWindow3.Show();

            viewData = database.GetBlockViewData<OverflowHashBlock<PCRTest>, PCRTest>(database.pcrTestFile.OverflowFile, testInstance);
            FileContent contentWindow4 = new FileContent(viewData, database.pcrTestFile.OverflowFile.FilePath, database.pcrTestFile.OverflowFile.BlockSize, database.pcrTestFile.OverflowFile.BlockFactor, viewData.Count);
            contentWindow4.Show();
        }
    }
}