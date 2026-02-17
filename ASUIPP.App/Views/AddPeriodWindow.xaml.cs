using System;
using System.Windows;
using ASUIPP.Core.Data;
using ASUIPP.Core.Data.Repositories;

namespace ASUIPP.App.Views
{
    public partial class AddPeriodWindow : Window
    {
        private readonly AcademicPeriodRepository _repo;
        public int CreatedPeriodId { get; private set; }

        public AddPeriodWindow(DatabaseContext dbContext)
        {
            InitializeComponent();
            Helpers.ZoomHelper.Apply(this);

            _repo = new AcademicPeriodRepository(dbContext);

            var now = DateTime.Now;
            YearBox.Text = (now.Month >= 9 ? now.Year : now.Year - 1).ToString();
            SemesterCombo.SelectedIndex = (now.Month >= 2 && now.Month <= 8) ? 1 : 0;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(YearBox.Text, out var year) || year < 2000 || year > 2100)
            {
                MessageBox.Show("Incorrect year.", "ASUIPP");
                return;
            }

            var semItem = SemesterCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
            if (semItem == null)
            {
                MessageBox.Show("Select semester.", "ASUIPP");
                return;
            }
            int semester = int.Parse(semItem.Tag.ToString());

            var period = _repo.GetOrCreate(year, semester);
            CreatedPeriodId = period.PeriodId;
            DialogResult = true;
        }
    }
}