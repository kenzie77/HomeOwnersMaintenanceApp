
using System;
using Microsoft.Maui.Controls;
using HomeMaintenanceApp.Models;
using HomeMaintenanceApp.Services;

namespace HomeMaintenanceApp.Pages
{
    public partial class IssuesPage : ContentPage
    {
        private readonly MaintenanceManager _manager;

        public IssuesPage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();

            _manager.LoadIssuesFromPreferences();
            _manager.LoadIssuesHistoryFromPreferences();

            ShowActive();
        }

        private void ShowActive()
        {
            IssuesView.ItemsSource = _manager.Issues;

            ActiveBtn.BackgroundColor = Colors.SteelBlue;
            ActiveBtn.TextColor = Colors.White;
            HistoryBtn.BackgroundColor = Colors.Transparent;
            HistoryBtn.TextColor = Colors.White;
        }

        private void ShowHistory()
        {
            IssuesView.ItemsSource = _manager.IssuesHistory;

            HistoryBtn.BackgroundColor = Colors.SteelBlue;
            HistoryBtn.TextColor = Colors.White;
            ActiveBtn.BackgroundColor = Colors.Transparent;
            ActiveBtn.TextColor = Colors.White;
        }

        private void OnShowActive(object sender, EventArgs e) => ShowActive();
        private void OnShowHistory(object sender, EventArgs e) => ShowHistory();

        // ------------------ Add ------------------
        private void OnAddIssueClicked(object sender, EventArgs e)
        {
            var issue = new IssueRecord
            {
                Title = "New Issue",
                Description = "",
                Severity = IssueSeverity.Minor,
                Resolved = false,
                ReportedOn = DateTime.Now,
                AttemptedSteps = "",
                FixNotes = ""
            };
            _manager.AddIssue(issue);

            // Ensure user sees it in Active
            ShowActive();
        }

        // ------------------ Inline edits ------------------
        private void OnTitleCompleted(object sender, EventArgs e)
        {
            if (sender is Entry entry && entry.BindingContext is IssueRecord issue)
            {
                issue.Title = entry.Text?.Trim() ?? "";
                _manager.UpdateIssue(issue);
            }
        }

        private void OnDescriptionCompleted(object sender, EventArgs e)
        {
            if (sender is Editor editor && editor.BindingContext is IssueRecord issue)
            {
                issue.Description = editor.Text?.Trim() ?? "";
                _manager.UpdateIssue(issue);
            }
        }

        private void OnSeverityChanged(object sender, EventArgs e)
        {
            if (sender is Picker picker && picker.BindingContext is IssueRecord issue)
            {
                var val = picker.SelectedItem as string;
                var sev = val switch
                {
                    "Minor" => IssueSeverity.Minor,
                    "Moderate" => IssueSeverity.Moderate,
                    "Major" => IssueSeverity.Major,
                    "Critical" => IssueSeverity.Critical,
                    _ => issue.Severity
                };

                if (sev != issue.Severity)
                {
                    issue.Severity = sev;
                    _manager.UpdateIssue(issue);
                }
            }
        }

        private void OnResolvedCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox cb && cb.BindingContext is IssueRecord issue)
            {
                if (e.Value == true)
                {
                    _manager.ResolveIssue(issue.Id);
                    ShowActive(); // refresh Active list since the item moved to History
                }
                else
                {
                    // If you ever want "unresolve" behavior, you can move it back;
                    // For now we keep History strict.
                }
            }
        }

        private void OnAttemptedCompleted(object sender, EventArgs e)
        {
            if (sender is Editor editor && editor.BindingContext is IssueRecord issue)
            {
                issue.AttemptedSteps = editor.Text?.Trim();
                _manager.UpdateIssue(issue);
            }
        }

        private void OnFixNotesCompleted(object sender, EventArgs e)
        {
            if (sender is Editor editor && editor.BindingContext is IssueRecord issue)
            {
                issue.FixNotes = editor.Text?.Trim();
                _manager.UpdateIssue(issue);
            }
        }

        // ------------------ Swipe actions ------------------
        private void OnResolveSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is IssueRecord issue)
            {
                _manager.ResolveIssue(issue.Id);
                ShowActive();
            }
        }

        private void OnDeleteSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is IssueRecord issue)
            {
                // Delete from whichever list is currently showing
                if (IssuesView.ItemsSource == _manager.Issues)
                    _manager.DeleteIssue(issue.Id);
                else
                {
                    _manager.IssuesHistory.Remove(issue);
                    _manager.SaveIssuesHistoryToPreferences();
                }
            }
        }
    }
}
