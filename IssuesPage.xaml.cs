
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

            // Load previously saved issues
            _manager.LoadIssuesFromPreferences();

            IssuesView.ItemsSource = _manager.Issues;
        }

        // ------------------ Add ------------------
        private void OnAddIssueClicked(object sender, EventArgs e)
        {
            // Create a new editable issue row
            var issue = new IssueRecord
            {
                Title = "New Issue",   // user will immediately overwrite this via Entry
                Description = "",
                Severity = IssueSeverity.Minor,
                Resolved = false,
                ReportedOn = DateTime.Now,
                AttemptedSteps = "",
                FixNotes = ""
            };
            _manager.AddIssue(issue);
            // NOTE: focusing the Entry inside a DataTemplate is tricky cross-platform;
            // user taps the Title field to edit. If you want auto-focus, we can add a popup/editor later.
        }

        // ------------------ Inline edits ------------------
        private void OnTitleCompleted(object sender, EventArgs e)
        {
            if (sender is Entry entry && entry.BindingContext is IssueRecord issue)
            {
                // Update title and persist
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
                issue.Resolved = e.Value;
                _manager.UpdateIssue(issue);
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
                issue.Resolved = true;
                _manager.UpdateIssue(issue);
            }
        }

        private void OnDeleteSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is IssueRecord issue)
            {
                _manager.DeleteIssue(issue.Id);
            }
        }
    }
}
