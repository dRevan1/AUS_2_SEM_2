using SEM_2_CORE;
using SEM_2_CORE.App;
using System;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for InsertUpdatePCRTest.xaml
    /// </summary>
    public partial class InsertUpdatePCRTest : Window
    {
        public string WindowTitle { get; private set; }
        public string ButtonText { get; private set; }
        private PCRTest? Test { get; set; }
        private PCRTestDatabase Database { get; set; }
        public InsertUpdatePCRTest(PCRTestDatabase database, PCRTest? test = null)
        {
            WindowTitle = (test == null) ? "Insert test" : $"Edit test {test.ID}";
            ButtonText = (test == null) ? "Insert" : "Edit";
            Database = database;
            Test = test;
            InitializeComponent();
            DataContext = this;
            if (test != null)
            {
                Test = test.CreateClass();
                PreFill();
                ID_Box.IsEnabled = false;
                PersonID_Box.IsEnabled = false;
            }
        }

        private void PreFill()
        {
            if (Test != null)
            {
                DOT_Box.Text = Test.DayOfTest.ToString();
                MOT_Box.Text = Test.MonthOfTest.ToString();
                YOT_Box.Text = Test.YearOfTest.ToString();
                HOT_Box.Text = Test.HourOfTest.ToString();
                MIOT_Box.Text = Test.MinuteOfTest.ToString();
                ID_Box.Text = Test.ID.ToString();
                PersonID_Box.Text = Test.PersonID;
                Result_Box.Text = Test.Result.ToString();
                Value_Box.Text = Test.TestValue.ToString();
                Note_Box.Text = Test.Note;
            }
        }

        private void InsertUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            string message;
            (byte, byte, ushort, byte, byte) testDateTime = (byte.Parse(DOT_Box.Text), byte.Parse(MOT_Box.Text), ushort.Parse(YOT_Box.Text), byte.Parse(MIOT_Box.Text), byte.Parse(HOT_Box.Text));
            if (Test != null)
            {
                message = Database.EditPCRTest(testDateTime, PersonID_Box.Text, Test.ID, bool.Parse(Result_Box.Text), double.Parse(Value_Box.Text), Note_Box.Text);
            }
            else
            {
                message = Database.InsertPCRTest(testDateTime, PersonID_Box.Text, uint.Parse(ID_Box.Text), bool.Parse(Result_Box.Text), double.Parse(Value_Box.Text), Note_Box.Text);
            }
            MessageBox.Show(message);
        }
    }
}