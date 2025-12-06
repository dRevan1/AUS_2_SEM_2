using SEM_2_CORE;
using SEM_2_CORE.App;
using SEM_2_CORE.Files;
using SEM_2_CORE.Testers;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PCRTestDatabase database {  get; set; }
        public MainWindow()
        {
            database = new PCRTestDatabase(4, "people.bin", "people_overflow.bin", 500, 220, 4, "test.bin", "test_overflow.bin", 500, 220);
            InitializeComponent();
            DataContext = this;
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

        // # 1 - Vloženie výsledku PCR testu
        private void InsertTestBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        // # 2 - Vyhľadanie osoby + jej testy
        private void FindPersonBtn_Click(object sender, RoutedEventArgs e)
        {
            List<PCRTest> tests = new List<PCRTest>();
            Person? person = database.GetPerson(ID.Text, out tests);
            if (person != null)
            {
                var window = new DisplayRecords(person, tests, true);
                window.ShowDialog();
            }
            else
            {
                MessageBox.Show($"Person with ID {ID.Text} wasn't found in the databse!");
            }
        }

        // # 3 - Vyhľadanie PCR testu + osoba
        private void FindTestBtn_Click(object sender, RoutedEventArgs e)
        {
            Person? person;
            PCRTest? test = database.GetPCRTest(uint.Parse(ID.Text), out person);
            if (test != null)
            {
                if (person == null)
                {
                    MessageBox.Show($"Person with ID {test.PersonID} attached to the test wasn't found in the databse!");
                    return;
                }
                List<PCRTest> tests = new List<PCRTest>();
                tests.Add(test);
                var window = new DisplayRecords(person, tests, false);
                window.ShowDialog();
            }
            else
            {
                MessageBox.Show($"PCR test with ID {ID.Text} wasn't found in the databse!");
            }
        }

        // # 4 - Vloženie osoby
        private void InsertPersonBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new InsertUpdatePerson();
            window.ShowDialog();
        }

        // # 5 - Vymazanie výsledku PCR testu
        private void DeleteTestBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(database.DeletePCRTest(uint.Parse(ID.Text)));
        }

        // # 6 - Vymazanie osoby + jej testov
        private void DeletePersonBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(database.DeletePerson(ID.Text));
        }

        // # 7 - Editácia údajov osoby
        private void EditPersonBtn_Click(object sender, RoutedEventArgs e)
        {
            Person? person = database.GetPerson(ID.Text, out _);
            if (person != null)
            {

            }
            else
            {
                MessageBox.Show($"Person with ID {ID.Text} wasn't found in the databse for edit!");
            }
        }

        // # 8 - Editácia údajov PCR testu
        private void EditTestBtn_Click(object sender, RoutedEventArgs e)
        {
            PCRTest? test = database.GetPCRTest(uint.Parse(ID.Text), out _);
            if (test != null)
            {

            }
            else
            {
                MessageBox.Show($"PCR test with ID {ID.Text} wasn't found in the databse for edit!");
            }
        }
    }
}