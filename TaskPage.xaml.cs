
using System;
using System.Linq;
using Microsoft.Maui.Controls;
using HomeMaintenanceApp.Models;
using HomeMaintenanceApp.Services;
// Alias to avoid ambiguity with System.Threading.Tasks.TaskStatus
using ModelsTaskStatus = HomeMaintenanceApp.Models.TaskStatus;

namespace HomeMaintenanceApp.Pages
{
    public partial class TaskPage : ContentPage
    {
        private readonly MaintenanceManager _manager;

        public TaskPage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();
            _manager.LoadTasksFromPreferences();

            TaskList.ItemsSource = _manager.Tasks;
        }

        // ------------------ Add ------------------
        private void OnAddTaskClicked(object sender, EventArgs e)
        {
            // Quick add a blank task, then user edits inline
            var t = new MaintenanceTask
            {
                Title = "New task",
                Description = "",
                Priority = TaskPriority.Low,
                Status = ModelsTaskStatus.NotStarted,
                DueDate = DateTime.Today.AddDays(7),
                Recurrence = TaskRecurrence.None
            };

            _manager.AddTask(t);
        }

        // ------------------ Inline actions ------------------
        private void OnStartClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is MaintenanceTask task)
            {
                task.Status = ModelsTaskStatus.InProgress;
                _manager.UpdateTask(task);
            }
        }

        private void OnDoneClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is MaintenanceTask task)
            {
                // Use manager's CompleteTask so recurrence advances
                _manager.CompleteTask(task.Id);
            }
        }

        private void OnPriorityChanged(object sender, EventArgs e)
        {
            if (sender is Picker picker && picker.BindingContext is MaintenanceTask task)
            {
                var val = picker.SelectedItem as string;
                var prio = val switch
                {
                    "Low" => TaskPriority.Low,
                    "Medium" => TaskPriority.Medium,
                    "High" => TaskPriority.High,
                    "Critical" => TaskPriority.Critical,
                    _ => task.Priority
                };

                if (prio != task.Priority)
                {
                    task.Priority = prio;
                    _manager.UpdateTask(task);
                }
            }
        }

        private void OnStatusChanged(object sender, EventArgs e)
        {
            if (sender is Picker picker && picker.BindingContext is MaintenanceTask task)
            {
                var val = picker.SelectedItem as string;
                var status = val switch
                {
                    "NotStarted" => ModelsTaskStatus.NotStarted,
                    "InProgress" => ModelsTaskStatus.InProgress,
                    "Completed" => ModelsTaskStatus.Completed,
                    "Deferred" => ModelsTaskStatus.Deferred,
                    _ => task.Status
                };

                // If Completed chosen, go through CompleteTask to auto-reschedule
                if (status == ModelsTaskStatus.Completed)
                {
                    _manager.CompleteTask(task.Id);
                }
                else if (status != task.Status)
                {
                    task.Status = status;
                    _manager.UpdateTask(task);
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

        // ------------------ Swipe actions ------------------
        private void OnCompleteSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is MaintenanceTask task)
            {
                _manager.CompleteTask(task.Id);
            }
        }

        private void OnDeleteSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is MaintenanceTask task)
            {
                _manager.DeleteTask(task.Id);
            }
        }
    }
}
