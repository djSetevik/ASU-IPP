using System;
using System.Windows;
using ASUIPP.App.ViewModels;
using ASUIPP.Core.Data;

namespace ASUIPP.App.Views
{
    public partial class SetupWindow : Window
    {
        private readonly SetupViewModel _viewModel;

        public event Action SetupCompleted;

        public SetupWindow(DatabaseContext dbContext)
        {
            InitializeComponent();
            Helpers.ZoomHelper.Apply(this);

            _viewModel = new SetupViewModel(dbContext);
            _viewModel.SetupCompleted += () => SetupCompleted?.Invoke();
            DataContext = _viewModel;
        }
    }
}