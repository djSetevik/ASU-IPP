using System.Windows;
using ASUIPP.App.Helpers;
using ASUIPP.App.ViewModels;
using ASUIPP.Core.Data;

namespace ASUIPP.App.Views
{
    public partial class SetupWindow : Window
    {
        public SetupWindow(DatabaseContext dbContext)
        {
            InitializeComponent();
            ZoomHelper.Apply(this);

            var vm = new SetupViewModel(dbContext);
            vm.SetupCompleted += () =>
            {
                DialogResult = true;
                Close();
            };
            DataContext = vm;
        }
    }
}