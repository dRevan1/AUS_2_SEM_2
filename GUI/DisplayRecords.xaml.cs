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
        private Person Person { get; set; }
        public string PersonData { get; private set; }
        public uint Test1 { get; private set; }
        public uint Test2 { get; private set; }
        public uint Test3 { get; private set; }
        public uint Test4 { get; private set; }
        public uint Test5 { get; private set; }
        public uint Test6 { get; private set; }
        public List<PCRTest> Tests { get; private set; }
        public int TestCount { get; private set; }
        public DisplayRecords(Person person, List<PCRTest> tests, bool personTests)
        {
            InitializeComponent();
            WindowTitle = (personTests) ? $"Person {person.ID}" : $"Test {tests[0].ID}";
            Person = person;
            PersonData = person.ToString();
            Test1 = Person.Tests[0];
            Test2 = Person.Tests[1];
            Test3 = Person.Tests[2];
            Test4 = Person.Tests[3];
            Test5 = Person.Tests[4];
            Test6 = Person.Tests[5];
            Tests = tests;
            TestCount = tests.Count;
            DataContext = this;
        }
    }
}