using SEM_2_CORE;
using SEM_2_CORE.App;
using SEM_2_CORE.Files;
using System.ComponentModel;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PCRTestDatabase? Database {  get; set; }  // na začiatku sa musí vybrať alebo vytvoriť nová inštancia, aby fungovali všetky metódy ovládania aplikácie
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            this.Closing += OnClosing;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (Database != null)
            {
                Database.SaveControlData();
            }
        }

        private void PopulateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Database == null)
            {
                MessageBox.Show($"Cannot populate - no databse loaded!");
                return;
            }
            var window = new GenerateRecords(Database);
            window.ShowDialog();
        }

        private void CreateDatabaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Database != null)
            {
                Database.SaveControlData();
            }
            // súbory
            string name = Dat_name.Text;
            string pplPrim = name + "_ppl_primary.bin";
            string pplOvrf = name + "_ppl_overflow.bin";
            string tPrim = name + "_test_primary.bin";
            string tOvrf = name + "_test_overflow.bin";

            // info pre súbor ľudí
            int peopleMod = int.Parse(P_mod.Text);
            int pplPrimSize = int.Parse(P_prim_size.Text);
            int pplOvrfSize = int.Parse(P_ovrf_size.Text);

            // info pre súbor PCR testov
            int testsMod = int.Parse(T_mod.Text);
            int tPrimSize = int.Parse(T_prim_size.Text);
            int tOvrfSize = int.Parse(T_ovrf_size.Text);

            Database = new PCRTestDatabase(peopleMod, pplPrim, pplOvrf, pplPrimSize, pplOvrfSize, testsMod, tPrim, tOvrf, tPrimSize, tOvrfSize, name);
            MessageBox.Show($"Database {name} created.");
        }

        private void LoadDatabaseBtn_Click( object sender, RoutedEventArgs e)
        {
            if (Database != null)
            {
                Database.SaveControlData();
            }
            string name = Dat_name.Text;
            string pplCtrl = name + "_ppl_control.csv";
            string tCtrl = name + "_test_control.csv";
            Database = new PCRTestDatabase(pplCtrl, tCtrl, name);
            MessageBox.Show($"Database {name} loaded.");
        }

        private void ViewContentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Database == null)
            {
                MessageBox.Show($"Cannot view contents - no databse loaded!");
                return;
            }
            Person personInstance = new Person("Gordon", "Freeman", 9, 4, 1995, "3");
            PCRTest testInstance = new PCRTest(1, 1, 2025, 1, 1, "0", 0, false, 0.0, "empty");

            List<BlockViewData> viewData = Database.GetBlockViewData<PrimaryHashBlock<Person>, Person>(Database.PeopleFile.PrimaryFile, personInstance);
            FileContent contentWindow = new FileContent(viewData, Database.PeopleFile.PrimaryFile.FilePath, Database.PeopleFile.PrimaryFile.BlockSize, Database.PeopleFile.PrimaryFile.BlockFactor, viewData.Count);
            contentWindow.Show();

            viewData = Database.GetBlockViewData<OverflowHashBlock<Person>, Person>(Database.PeopleFile.OverflowFile, personInstance);
            FileContent contentWindow2 = new FileContent(viewData, Database.PeopleFile.OverflowFile.FilePath, Database.PeopleFile.OverflowFile.BlockSize, Database.PeopleFile.OverflowFile.BlockFactor, viewData.Count);
            contentWindow2.Show();

            viewData = Database.GetBlockViewData<PrimaryHashBlock<PCRTest>, PCRTest>(Database.PcrTestFile.PrimaryFile, testInstance);
            FileContent contentWindow3 = new FileContent(viewData, Database.PcrTestFile.PrimaryFile.FilePath, Database.PcrTestFile.PrimaryFile.BlockSize, Database.PcrTestFile.PrimaryFile.BlockFactor, viewData.Count);
            contentWindow3.Show();

            viewData = Database.GetBlockViewData<OverflowHashBlock<PCRTest>, PCRTest>(Database.PcrTestFile.OverflowFile, testInstance);
            FileContent contentWindow4 = new FileContent(viewData, Database.PcrTestFile.OverflowFile.FilePath, Database.PcrTestFile.OverflowFile.BlockSize, Database.PcrTestFile.OverflowFile.BlockFactor, viewData.Count);
            contentWindow4.Show();
        }

        // # 1 - Vloženie výsledku PCR testu
        private void InsertTestBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Database == null)
            {
                MessageBox.Show($"Cannot insert test - no databse loaded!");
                return;
            }
            var window = new InsertUpdatePCRTest(Database);
            window.ShowDialog();
        }

        // # 2 - Vyhľadanie osoby + jej testy
        private void FindPersonBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Database == null)
            {
                MessageBox.Show($"Cannot find person - no databse loaded!");
                return;
            }
            List<PCRTest> tests = new List<PCRTest>();
            if (string.IsNullOrWhiteSpace(ID.Text))
            {
                MessageBox.Show($"Person with empty ID isn't in the database!");
                return;
            }
            Person? person = Database.GetPerson(ID.Text, out tests);
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
            if (Database == null)
            {
                MessageBox.Show($"Cannot find test - no databse loaded!");
                return;
            }
            Person? person;
            if (string.IsNullOrWhiteSpace(ID.Text))
            {
                MessageBox.Show($"PCR test with empty ID isn't in the database!");
                return;
            }
            PCRTest? test = Database.GetPCRTest(uint.Parse(ID.Text), out person);
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
            if (Database == null)
            {
                MessageBox.Show($"Cannot insert person - no databse loaded!");
                return;
            }
            var window = new InsertUpdatePerson(Database);
            window.ShowDialog();
        }

        // # 7 - Editácia údajov osoby
        private void EditPersonBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Database == null)
            {
                MessageBox.Show($"Cannot edit person - no databse loaded!");
                return;
            }
            if (string.IsNullOrWhiteSpace(ID.Text))
            {
                MessageBox.Show($"Person with empty ID isn't in the database!");
                return;
            }
            Person? person = Database.GetPerson(ID.Text, out _);
            if (person != null)
            {
                var window = new InsertUpdatePerson(Database, person);
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
            if (Database == null)
            {
                MessageBox.Show($"Cannot edit test - no databse loaded!");
                return;
            }
            if (string.IsNullOrWhiteSpace(ID.Text))
            {
                MessageBox.Show($"PCR test with empty ID isn't in the database!");
                return;
            }
            PCRTest? test = Database.GetPCRTest(uint.Parse(ID.Text), out _);
            if (test != null)
            {
                var window = new InsertUpdatePCRTest(Database, test);
                window.ShowDialog();
            }
            else
            {
                MessageBox.Show($"PCR test with ID {ID.Text} wasn't found in the database for edit!");
            }
        }
    }
}