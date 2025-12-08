using SEM_2_CORE.App;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for GenerateRecords.xaml
    /// </summary>
    public partial class GenerateRecords : Window
    {
        private PCRTestDatabase Database {  get; set; }
        public GenerateRecords(PCRTestDatabase database)
        {
            Database = database;
            InitializeComponent();
            DataContext = this;
        }

        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            DateTime pplStart = new DateTime(int.Parse(Ppl_yfrom.Text), int.Parse(Ppl_mfrom.Text), int.Parse(Ppl_dfrom.Text));
            DateTime pplEnd = new DateTime(int.Parse(Ppl_yto.Text), int.Parse(Ppl_mto.Text), int.Parse(Ppl_dto.Text));
            DateTime tStart = new DateTime(int.Parse(T_yfrom.Text), int.Parse(T_mfrom.Text), int.Parse(T_dfrom.Text));
            DateTime tEnd = new DateTime(int.Parse(T_yto.Text), int.Parse(T_mto.Text), int.Parse(T_dto.Text));

            Database.PopulateDatabase(int.Parse(Ppl_count.Text), uint.Parse(T_count.Text), pplStart, pplEnd, tStart, tEnd);
            MessageBox.Show("File(s) has/have been populated.");
        }
    }
}
