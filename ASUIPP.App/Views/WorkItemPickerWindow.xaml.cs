using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ASUIPP.Core.Helpers;
using ASUIPP.Core.Models;

namespace ASUIPP.App.Views
{
    public partial class WorkItemPickerWindow : Window
    {
        private readonly List<WorkItemDisplay> _allItems;

        public WorkItem SelectedWorkItem { get; private set; }
        public string WorkName { get; private set; }
        public int Points { get; private set; }
        public DateTime? DueDate { get; private set; }

        public WorkItemPickerWindow(List<WorkItem> workItems)
        {
            InitializeComponent();
            Helpers.ZoomHelper.Apply(this);

            _allItems = workItems.Select(wi => new WorkItemDisplay
            {
                WorkItem = wi,
                DisplayText = $"{wi.DisplayId} {wi.Name}  [{wi.MaxPoints}]"
            }).ToList();

            ItemsList.ItemsSource = _allItems;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchBox.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(query))
            {
                ItemsList.ItemsSource = _allItems;
            }
            else
            {
                ItemsList.ItemsSource = _allItems
                    .Where(i => i.DisplayText.ToLowerInvariant().Contains(query))
                    .ToList();
            }
        }

        private void ItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemsList.SelectedItem is WorkItemDisplay display)
            {
                var wi = display.WorkItem;
                WorkNameBox.Text = wi.Name;
                MaxPointsLabel.Text = PointsValidator.GetDisplayString(wi.MaxPoints, wi.MaxPointsNumeric);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(ItemsList.SelectedItem is WorkItemDisplay display))
            {
                MessageBox.Show("Выберите пункт работы.", "АСУИПП");
                return;
            }

            if (string.IsNullOrWhiteSpace(WorkNameBox.Text))
            {
                MessageBox.Show("Введите название работы.", "АСУИПП");
                return;
            }

            if (!int.TryParse(PointsBox.Text, out var points))
            {
                MessageBox.Show("Введите корректное количество баллов.", "АСУИПП");
                return;
            }

            var wi = display.WorkItem;
            if (!PointsValidator.Validate(points, wi.MaxPointsNumeric))
            {
                MessageBox.Show(
                    $"Баллы должны быть в допустимом диапазоне: {PointsValidator.GetDisplayString(wi.MaxPoints, wi.MaxPointsNumeric)}",
                    "АСУИПП");
                return;
            }

            SelectedWorkItem = wi;
            WorkName = WorkNameBox.Text.Trim();
            Points = points;
            DueDate = DueDatePicker.SelectedDate;
            DialogResult = true;
        }

        private class WorkItemDisplay
        {
            public WorkItem WorkItem { get; set; }
            public string DisplayText { get; set; }
        }
    }
}