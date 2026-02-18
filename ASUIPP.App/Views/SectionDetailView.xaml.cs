using System;
using System.Windows.Controls;

namespace ASUIPP.App.Views
{
    public partial class SectionDetailView : UserControl
    {
        public SectionDetailView()
        {
            InitializeComponent();
        }

        private bool _isUpdating;

        private void StatusCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating) return;
            if (e.AddedItems.Count == 0) return;
            if (!(sender is ComboBox combo)) return;
            if (!(combo.Tag is Core.Models.PlannedWork work)) return;
            if (!(combo.SelectedValue is Core.Models.WorkStatus newStatus)) return;
            if (newStatus == work.Status) return;

            _isUpdating = true;
            try
            {
                var vm = DataContext as ViewModels.SectionDetailViewModel;
                vm?.ChangeWorkStatus(work.WorkId, newStatus);
            }
            finally
            {
                // Даём UI обновиться перед снятием флага
                Dispatcher.BeginInvoke(new Action(() => _isUpdating = false),
                    System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }
}