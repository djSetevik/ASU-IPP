using System.Windows.Controls;

namespace ASUIPP.App.Views
{
    public partial class SectionDetailView : UserControl
    {
        public SectionDetailView()
        {
            InitializeComponent();
        }

        private void StatusCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo
                && combo.Tag is ASUIPP.Core.Models.PlannedWork work
                && combo.SelectedValue is ASUIPP.Core.Models.WorkStatus newStatus
                && newStatus != work.Status)
            {
                var vm = DataContext as ViewModels.SectionDetailViewModel;
                vm?.ChangeWorkStatus(work.WorkId, newStatus);
            }
        }
    }
}