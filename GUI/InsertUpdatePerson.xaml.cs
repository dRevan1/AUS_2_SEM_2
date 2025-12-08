using SEM_2_CORE;
using SEM_2_CORE.App;
using System;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for InsertUpdatePerson.xaml
    /// </summary>
    public partial class InsertUpdatePerson : Window
    {
        public string WindowTitle { get; private set; }
        public string ButtonText { get; private set; }
        private Person? Person {  get; set; }
        private PCRTestDatabase Database { get; set; }
        public InsertUpdatePerson(PCRTestDatabase database, Person? person = null)
        {
            WindowTitle = (person == null) ? "Insert person" : $"Edit person {person.ID}";
            ButtonText = (person == null) ? "Insert" : "Edit";
            Person = person;
            Database = database;
            InitializeComponent();
            DataContext = this;
            if (person != null)
            {
                person = person.CreateClass();
                PreFill();
                ID_Box.IsEnabled = false;
            }
        }

        private void PreFill()
        {
            if (Person != null)
            {
                Name_Box.Text = Person.Name;
                Surname_Box.Text = Person.Surname;
                DOB_Box.Text = Person.DayOfBirth.ToString();
                MOB_Box.Text = Person.MonthOfBirth.ToString();
                YOB_Box.Text = Person.YearOfBirth.ToString();
                ID_Box.Text = Person.ID;
            }
        }

        private void InsertUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            string message;
            (byte, byte, ushort) DOB = (byte.Parse(DOB_Box.Text), byte.Parse(MOB_Box.Text), ushort.Parse(YOB_Box.Text));
            if (Person != null)
            {
                message = Database.EditPerson(Name_Box.Text, Surname_Box.Text, DOB, Person.ID, Person.Tests);
            }
            else
            {
                message = Database.InsertPerson(Name_Box.Text, Surname_Box.Text, DOB, ID_Box.Text);
            }
            MessageBox.Show(message);
        }
    }
}