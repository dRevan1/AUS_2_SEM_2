using SEM_2_CORE;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for DisplayRecords.xaml
    /// </summary>
    public partial class DisplayRecords : Window
    {
        public string WindowTitle { get; private set; }
        public List<Person> People { get; private set; } = new List<Person>();
        public List<PCRTest> Tests { get; private set; }
        public int TestCount { get; private set; }
        public DisplayRecords(Person person, List<PCRTest> tests, bool personTests)
        {
            WindowTitle = (personTests) ? $"Person {person.ID}" : "Test";
            People.Add(person);
            Tests = tests;
            TestCount = tests.Count;
            InitializeComponent();
            DataContext = this;
        }
    }
}