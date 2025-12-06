using SEM_2_CORE;
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
        private bool Update {  get; set; }
        public InsertUpdatePerson(Person? person = null, bool update = false)
        {
            WindowTitle = (person == null) ? "Insert person" : $"Edit person {person.ID}";
            ButtonText = (person == null) ? "Insert" : "Edit";
            Update = update;
            if (person != null)
            {
                PreFill(person);
                ID_Box.IsEnabled = false;
            }

            InitializeComponent();
            DataContext = this;
        }

        private void PreFill(Person person)
        {
            Name_Box.Text = person.Name;
            Surname_Box.Text = person.Surname;
            DOB_Box.Text = person.DayOfBirth.ToString();
            MOB_Box.Text = person.MonthOfBirth.ToString();
            YOB_Box.Text = person.YearOfBirth.ToString();
            ID_Box.Text = person.ID;
        }

        private void InsertUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Update)
            {

            }
            else
            {

            }
        }
    }
}