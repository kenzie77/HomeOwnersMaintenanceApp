
using System;
using Microsoft.Maui.Controls;
using HomeMaintenanceApp.Models;
using HomeMaintenanceApp.Services;
using TaskStatus = HomeMaintenanceApp.Models.TaskStatus;

namespace HomeMaintenanceApp.Pages
{
    public partial class TaskPage : ContentPage
    {
        private readonly MaintenanceManager _manager;

        public TaskPage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();

            // Initial load + bind
            _manager.LoadTasksFromPreferences();
            TaskList.ItemsSource = _manager.Tasks; // NOTE: matches x:Name in XAML
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Reflect any changes (e.g., Onboarding resets)
            _manager.LoadTasksFromPreferences();
            TaskList.ItemsSource = _manager.Tasks;

            // Optional: if user turned Pool OFF, hide any seeded pool tasks still present
            // (manager.ResetEverything() already clears tasks; this is a second guard)
            if (!(_manager.Property?.HasPool ?? false))
            {
                // Show only non-pool tasks. If you prefer removing them permanently,
                // do it in SetHasPool(false) in the manager.
                // TaskList.ItemsSource = new ObservableCollection<MaintenanceTask>(
                //     _manager.Tasks.Where(t => !t.Title.StartsWith("Pool:", StringComparison.OrdinalIgnoreCase)));
            }
        }

        // -------------------- Header: Add Task --------------------
        private void OnAddTaskClicked(object sender, EventArgs e)
        {
            var t = new MaintenanceTask
            {
                Title = "New Task",
                Description = "",
                Priority = TaskPriority.Medium,
                Status = TaskStatus.NotStarted,
                DueDate = DateTime.Today.AddDays(7),   // default due; user can change
                Recurrence = TaskRecurrence.None
            };

            _manager.AddTask(t);
        }

        // -------------------- Swipe: Complete / Delete --------------------
        private void OnCompleteSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is MaintenanceTask task)
            {
                _manager.CompleteTask(task.Id);
                // Optional toast
                DisplayAlert("Completed",
                    $"{task.Title} completed on {DateTime.Today:MMM d, yyyy}. Next due: {task.DueDate:MMM d, yyyy}",
                    "OK");
            }
        }

        private async void OnDeleteSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is MaintenanceTask task)
            {
                bool confirm = await DisplayAlert("Delete task",
                    $"Delete \"{task.Title}\"?",
                    "Delete", "Cancel");
                if (!confirm) return;

                _manager.DeleteTask(task.Id);
            }
        }

        // -------------------- Row: Start / Done buttons --------------------
        private void OnStartClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is MaintenanceTask task)
            {
                _manager.StartTask(task.Id);
            }
        }

        private void OnDoneClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is MaintenanceTask task)
            {
                _manager.CompleteTask(task.Id);

                // Optional toast with completed + next due
                DisplayAlert("Completed",
                    $"{task.Title} completed on {DateTime.Today:MMM d, yyyy}. Next due: {task.DueDate:MMM d, yyyy}",
                    "OK");
            }
        }

        // -------------------- Inline editors (Priority / Status / Due) --------------------
        private void OnPriorityChanged(object sender, EventArgs e)
        {
            if (sender is Picker picker && picker.BindingContext is MaintenanceTask task)
            {
                var val = picker.SelectedItem as string;
                var newPriority = val switch
                {
                    "Low" => TaskPriority.Low,
                    "Medium" => TaskPriority.Medium,
                    "High" => TaskPriority.High,
                    "Critical" => TaskPriority.Critical,
                    _ => task.Priority
                };

                if (newPriority != task.Priority)
                {
                    task.Priority = newPriority;
                    _manager.UpdateTask(task);
                }
            }
        }

        private void OnStatusChanged(object sender, EventArgs e)
        {
            if (sender is Picker picker && picker.BindingContext is MaintenanceTask task)
            {
                var val = picker.SelectedItem as string;
                var newStatus = val switch
                {
                    "NotStarted" => TaskStatus.NotStarted,
                    "InProgress" => TaskStatus.InProgress,
                    "Completed" => TaskStatus.Completed,
                    "Deferred" => TaskStatus.Deferred,
                    _ => task.Status
                };

                if (newStatus != task.Status)
                {
                    // If user manually sets Completed via picker, do the complete flow (stamp + reschedule)
                    if (newStatus == TaskStatus.Completed)
                    {
                        _manager.CompleteTask(task.Id);
                    }
                    else
                    {
                        task.Status = newStatus;
                        _manager.UpdateTask(task);
                    }
                }
            }
        }

        private void OnDueDateSelected(object sender, DateChangedEventArgs e)
        {
            if (sender is DatePicker dp && dp.BindingContext is MaintenanceTask task)
            {
                task.DueDate = e.NewDate;
                _manager.UpdateTask(task);
            }
        }
    }
}
