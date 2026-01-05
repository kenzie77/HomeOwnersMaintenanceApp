
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using HomeMaintenanceApp.Models;
using HomeMaintenanceApp.Services;

namespace HomeMaintenanceApp.Pages
{
    public partial class IssuesPage : ContentPage
    {
        private readonly MaintenanceManager _manager;
        private readonly HashSet<Guid> _selectedHistoryIds = new(); // for bulk delete

        private bool _showingHistory = false;

        public IssuesPage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();

            // Load persisted issues
            _manager.LoadIssuesFromPreferences();
            _manager.LoadIssuesHistoryFromPreferences();

            // Bind
            ActiveList.ItemsSource = _manager.Issues;
            HistoryList.ItemsSource = _manager.IssuesHistory;

            ShowActive();
        }

        // -------- Tabs --------
        private void ShowActive()
        {
            _showingHistory = false;

            ActiveList.IsVisible = true;
            HistoryList.IsVisible = false;
            HistoryDeleteBar.IsVisible = false;
            _selectedHistoryIds.Clear();

            ActiveTabBtn.BackgroundColor = Colors.SteelBlue;
            ActiveTabBtn.TextColor = Colors.White;
            HistoryTabBtn.BackgroundColor = Colors.Transparent;
            HistoryTabBtn.TextColor = Colors.White;
        }

        private void ShowHistory()
        {
            _showingHistory = true;

            ActiveList.IsVisible = false;
            HistoryList.IsVisible = true;
            HistoryDeleteBar.IsVisible = true;
            _selectedHistoryIds.Clear();
            UpdateHistoryDeleteBarEnabled();

            HistoryTabBtn.BackgroundColor = Colors.SteelBlue;
            HistoryTabBtn.TextColor = Colors.White;
            ActiveTabBtn.BackgroundColor = Colors.Transparent;
            ActiveTabBtn.TextColor = Colors.White;
        }

        private void OnShowActive(object sender, EventArgs e) => ShowActive();
        private void OnShowHistory(object sender, EventArgs e) => ShowHistory();

        // -------- Add --------
        private void OnAddIssueClicked(object sender, EventArgs e)
        {
            var issue = new IssueRecord
            {
                Title = "New Issue",
                Description = "",
                Severity = IssueSeverity.Minor,
                Resolved = false,
                ResolvedOn = null,
                ReportedOn = DateTime.Now,
                AttemptedSteps = "",
                FixNotes = ""
            };

            _manager.AddIssue(issue);

            // Always show new issues in Active
            ShowActive();
        }

        // -------- Inline edits (Active) --------
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
                if (e.Value)
                {
                    // Resolve: stamp date, move to history, persist
                    _manager.ResolveIssue(issue.Id);
                    ShowHistory();
                }
                else
                {
                    // Unresolve in Active (if you allow it)
                    issue.Resolved = false;
                    issue.ResolvedOn = null;
                    _manager.UpdateIssue(issue);
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

        // -------- Swipe actions (Active) --------
        private void OnResolveSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is IssueRecord issue)
            {
                _manager.ResolveIssue(issue.Id);
                ShowHistory();
            }
        }

        private async void OnDeleteActiveSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is IssueRecord issue)
            {
                bool confirm = await DisplayAlert("Delete issue",
                    $"Delete \"{issue.Title}\" from Active?",
                    "Delete", "Cancel");
                if (!confirm) return;

                _manager.DeleteIssue(issue.Id);
            }
        }

        // ===================== HISTORY AREA =====================

        // Each row has a select checkbox (for bulk delete)
        private void OnHistorySelectChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox cb && cb.BindingContext is IssueRecord issue)
            {
                if (e.Value) _selectedHistoryIds.Add(issue.Id);
                else _selectedHistoryIds.Remove(issue.Id);
                UpdateHistoryDeleteBarEnabled();
            }
        }

        // Enable/disable top red delete bar
        private void UpdateHistoryDeleteBarEnabled()
        {
            HistoryDeleteBar.Opacity = _selectedHistoryIds.Count > 0 ? 1.0 : 0.4;
            HistoryDeleteBar.Stroke = _selectedHistoryIds.Count > 0 ? Colors.Red : Colors.DarkRed;
        }

        // Top: Delete selected (bulk)
        private async void OnDeleteSelectedHistoryTapped(object sender, EventArgs e)
        {
            if (_selectedHistoryIds.Count == 0)
            {
                await DisplayAlert("Delete selected", "No items are selected.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Delete selected",
                $"Delete {_selectedHistoryIds.Count} item(s) from History?",
                "Delete", "Cancel");
            if (!confirm) return;

            // Remove selected items from history
            var toDelete = _manager.IssuesHistory
                .Where(i => _selectedHistoryIds.Contains(i.Id))
                .ToList();

            foreach (var item in toDelete)
                _manager.IssuesHistory.Remove(item);

            _manager.SaveIssuesHistoryToPreferences();
            _selectedHistoryIds.Clear();
            UpdateHistoryDeleteBarEnabled();
        }

        // Per-row Delete button
        private async void OnDeleteHistoryRowClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is IssueRecord issue)
            {
                bool confirm = await DisplayAlert("Delete issue",
                    $"Delete \"{issue.Title}\" from History?",
                    "Delete", "Cancel");
                if (!confirm) return;

                _manager.IssuesHistory.Remove(issue);
                _manager.SaveIssuesHistoryToPreferences();

                _selectedHistoryIds.Remove(issue.Id);
                UpdateHistoryDeleteBarEnabled();
            }
        }

        // Swipe Delete in History
        private async void OnDeleteHistorySwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is IssueRecord issue)
            {
                bool confirm = await DisplayAlert("Delete issue",
                    $"Delete \"{issue.Title}\" from History?",
                    "Delete", "Cancel");
                if (!confirm) return;

                _manager.IssuesHistory.Remove(issue);
                _manager.SaveIssuesHistoryToPreferences();

                _selectedHistoryIds.Remove(issue.Id);
                UpdateHistoryDeleteBarEnabled();
            }
        }
    }
}
