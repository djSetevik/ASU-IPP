using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ASUIPP.App.ViewModels;
using ASUIPP.Core.Helpers;
using ASUIPP.Core.Models;

namespace ASUIPP.App.Views
{
    public partial class EditWorkWindow : Window
    {
        private readonly PlannedWork _work;
        private readonly List<WorkItemDisplay> _allItems;

        public string EditedWorkName { get; private set; }
        public int EditedPoints { get; private set; }
        public DateTime? EditedDueDate { get; private set; }
        public WorkStatus EditedStatus { get; private set; }
        public WorkItem EditedWorkItem { get; private set; }

        private readonly List<PlannedWork> _allWorks;
        private readonly int _sectionId;

        public EditWorkWindow(PlannedWork work, WorkItem currentWorkItem, List<WorkItem> allSectionItems)
        {
            InitializeComponent();
            Helpers.ZoomHelper.Apply(this);

            _work = work;
            _sectionId = work.SectionId;

            // Список пунктов
            _allItems = allSectionItems.Select(wi => new WorkItemDisplay
            {
                WorkItem = wi,
                DisplayText = $"п.{wi.DisplayId}  {wi.Name}  [{wi.MaxPoints}]"
            }).ToList();

            ItemsList.ItemsSource = _allItems;

            // Выбираем текущий пункт
            var currentDisplay = _allItems.FirstOrDefault(d =>
                d.WorkItem.SectionId == work.SectionId && d.WorkItem.ItemId == work.ItemId);
            if (currentDisplay != null)
                ItemsList.SelectedItem = currentDisplay;

            // Заполняем поля
            WorkNameBox.Text = work.WorkName;
            PointsBox.Text = work.Points.ToString();
            DueDatePicker.SelectedDate = work.DueDate;

            if (currentWorkItem != null)
            {
                MaxPointsLabel.Text = PointsValidator.GetDisplayString(
                    currentWorkItem.MaxPoints, currentWorkItem.MaxPointsNumeric);
            }

            // Статусы
            StatusCombo.ItemsSource = new List<StatusOption>
            {
                new StatusOption { Value = WorkStatus.Planned, DisplayName = "Запланирована" },
                new StatusOption { Value = WorkStatus.InProgress, DisplayName = "Выполняется" },
                new StatusOption { Value = WorkStatus.Done, DisplayName = "Ожидает подтверждения" },
                new StatusOption { Value = WorkStatus.Confirmed, DisplayName = "Подтверждена" }
            };
            StatusCombo.SelectedValue = work.Status;
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
                MaxPointsLabel.Text = PointsValidator.GetDisplayString(wi.MaxPoints, wi.MaxPointsNumeric);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
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

            WorkItem selectedItem = null;
            if (ItemsList.SelectedItem is WorkItemDisplay display)
            {
                selectedItem = display.WorkItem;
                if (!PointsValidator.Validate(points, selectedItem.MaxPointsNumeric))
                {
                    MessageBox.Show(
                        $"Баллы должны быть в допустимом диапазоне: {PointsValidator.GetDisplayString(selectedItem.MaxPoints, selectedItem.MaxPointsNumeric)}",
                        "АСУИПП");
                    return;
                }
            }

            // Проверка лимитов
            int targetSectionId = _sectionId;
            if (selectedItem != null)
                targetSectionId = selectedItem.SectionId;

            EditedWorkName = WorkNameBox.Text.Trim();
            EditedPoints = points;
            EditedDueDate = DueDatePicker.SelectedDate;
            EditedStatus = (WorkStatus)StatusCombo.SelectedValue;
            EditedWorkItem = selectedItem;
            DialogResult = true;
        }

        private class WorkItemDisplay
        {
            public WorkItem WorkItem { get; set; }
            public string DisplayText { get; set; }
        }
    }
}