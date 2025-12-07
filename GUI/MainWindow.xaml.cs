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
            database.PopulateDatabase(1_000, 2_000);
            MessageBox.Show("File(s) has/have been populated.");
        }

        private void ViewContentBtn_Click(object sender, RoutedEventArgs e)
        {
            Person personInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
            PCRTest testInstance = new PCRTest(1, 1, 2025, 1, 1, "0", 0, false, 0.0, "empty");

            List<BlockViewData> viewData = database.GetBlockViewData<PrimaryHashBlock<Person>, Person>(database.PeopleFile.PrimaryFile, personInstance);
            FileContent contentWindow = new FileContent(viewData, database.PeopleFile.PrimaryFile.FilePath, database.PeopleFile.PrimaryFile.BlockSize, database.PeopleFile.PrimaryFile.BlockFactor, viewData.Count);
            contentWindow.Show();

            viewData = database.GetBlockViewData<OverflowHashBlock<Person>, Person>(database.PeopleFile.OverflowFile, personInstance);
            FileContent contentWindow2 = new FileContent(viewData, database.PeopleFile.OverflowFile.FilePath, database.PeopleFile.OverflowFile.BlockSize, database.PeopleFile.OverflowFile.BlockFactor, viewData.Count);
            contentWindow2.Show();

            viewData = database.GetBlockViewData<PrimaryHashBlock<PCRTest>, PCRTest>(database.PcrTestFile.PrimaryFile, testInstance);
            FileContent contentWindow3 = new FileContent(viewData, database.PcrTestFile.PrimaryFile.FilePath, database.PcrTestFile.PrimaryFile.BlockSize, database.PcrTestFile.PrimaryFile.BlockFactor, viewData.Count);
            contentWindow3.Show();

            viewData = database.GetBlockViewData<OverflowHashBlock<PCRTest>, PCRTest>(database.PcrTestFile.OverflowFile, testInstance);
            FileContent contentWindow4 = new FileContent(viewData, database.PcrTestFile.OverflowFile.FilePath, database.PcrTestFile.OverflowFile.BlockSize, database.PcrTestFile.OverflowFile.BlockFactor, viewData.Count);
            contentWindow4.Show();
        }

        // # 1 - Vloženie výsledku PCR testu
        private void InsertTestBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new InsertUpdatePCRTest(database);
            window.ShowDialog();
        }

        // # 2 - Vyhľadanie osoby + jej testy
        private void FindPersonBtn_Click(object sender, RoutedEventArgs e)
        {
            List<PCRTest> tests = new List<PCRTest>();
            if (string.IsNullOrWhiteSpace(ID.Text))
            {
                MessageBox.Show($"Person with empty ID isn't in the database!");
                return;
            }
            Person? person = database.GetPerson(ID.Text, out tests);
            if (person != null)
            {
                var window = new DisplayRecords(person, tests, true);
                window.ShowDialog();
            }
            else
            {
                MessageBox.Show($"Person with ID {ID.Text} wasn't found in the database!");
            }
        }

        // # 3 - Vyhľadanie PCR testu + osoba
        private void FindTestBtn_Click(object sender, RoutedEventArgs e)
        {
            Person? person;
            if (string.IsNullOrWhiteSpace(ID.Text))
            {
                MessageBox.Show($"PCR test with empty ID isn't in the database!");
                return;
            }
            PCRTest? test = database.GetPCRTest(uint.Parse(ID.Text), out person);
            if (test != null)
            {
                if (person == null)
                {
                    MessageBox.Show($"Person with ID {test.PersonID} attached to the test wasn't found in the database!");
                    return;
                }
                List<PCRTest> tests = new List<PCRTest>();
                tests.Add(test);
                var window = new DisplayRecords(person, tests, false);
                window.ShowDialog();
            }
            else
            {
                MessageBox.Show($"PCR test with ID {ID.Text} wasn't found in the database!");
            }
        }

        // # 4 - Vloženie osoby
        private void InsertPersonBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new InsertUpdatePerson(database);
            window.ShowDialog();
        }

        // # 7 - Editácia údajov osoby
        private void EditPersonBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ID.Text))
            {
                MessageBox.Show($"Person with empty ID isn't in the database!");
                return;
            }
            Person? person = database.GetPerson(ID.Text, out _);
            if (person != null)
            {
                var window = new InsertUpdatePerson(database, person);
                window.ShowDialog();
            }
            else
            {
                MessageBox.Show($"Person with ID {ID.Text} wasn't found in the database for edit!");
            }
        }

        // # 8 - Editácia údajov PCR testu
        private void EditTestBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ID.Text))
            {
                MessageBox.Show($"PCR test with empty ID isn't in the database!");
                return;
            }
            PCRTest? test = database.GetPCRTest(uint.Parse(ID.Text), out _);
            if (test != null)
            {
                var window = new InsertUpdatePCRTest(database, test);
                window.ShowDialog();
            }
            else
            {
                MessageBox.Show($"PCR test with ID {ID.Text} wasn't found in the database for edit!");
            }
        }
    }
}