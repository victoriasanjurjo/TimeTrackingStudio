using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using TimeTrackingStudio.WinUI.Models;
using TimeTrackingStudio.WinUI.Services;

namespace TimeTrackingStudio.WinUI.Views;

public sealed partial class TimeEntryDialog : ContentDialog
{
    public TimeLog TimeLogModel { get; private set; }
    private readonly List<Job> _jobs;

    public TimeEntryDialog(JobService? jobService = null)
    {
        this.InitializeComponent();
        TimeLogModel = new TimeLog();

        var js = jobService ?? new JobService();
        _jobs = js.GetAllJobs();
        JobComboBox.ItemsSource = _jobs;
        if (_jobs.Count > 0)
        {
            JobComboBox.SelectedIndex = 0;
        }

        EntryDatePicker.Date = DateTimeOffset.Now;

        this.PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (JobComboBox.SelectedItem is not Job selectedJob || double.IsNaN(HoursNumberBox.Value) || HoursNumberBox.Value <= 0)
        {
            args.Cancel = true;
            return;
        }

        var hours = HoursNumberBox.Value;
        var date = EntryDatePicker.Date.DateTime;
        var startTime = new DateTime(date.Year, date.Month, date.Day, 9, 0, 0);
        var endTime = startTime.AddHours(hours);

        TimeLogModel.JobId = selectedJob.Id;
        TimeLogModel.JobTitle = selectedJob.Title;
        TimeLogModel.ClientId = selectedJob.ClientId;
        TimeLogModel.ClientName = selectedJob.ClientName;
        TimeLogModel.StartTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
        TimeLogModel.EndTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
        TimeLogModel.DurationSeconds = (int)(hours * 3600);
        TimeLogModel.Description = DescriptionTextBox.Text.Trim();
        TimeLogModel.IsBillable = BillableCheckBox.IsChecked ?? true;
        TimeLogModel.HourlyRate = selectedJob.HourlyRate ?? 50.0;
        TimeLogModel.CreatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
    }
}
