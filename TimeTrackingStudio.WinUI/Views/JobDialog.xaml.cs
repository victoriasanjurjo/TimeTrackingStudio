using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using TimeTrackingStudio.WinUI.Models;
using TimeTrackingStudio.WinUI.Services;

namespace TimeTrackingStudio.WinUI.Views;

public sealed partial class JobDialog : ContentDialog
{
    public Job JobModel { get; private set; }
    private readonly List<Client> _clients;

    public JobDialog(Job? existing = null, ClientService? clientService = null)
    {
        this.InitializeComponent();
        JobModel = existing ?? new Job();

        var cs = clientService ?? new ClientService();
        _clients = cs.GetAllClients(onlyActive: true);
        ClientComboBox.ItemsSource = _clients;

        if (existing != null)
        {
            Title = "Editar Trabajo / Tarea";
            TitleTextBox.Text = existing.Title;
            DescriptionTextBox.Text = existing.Description;
            EstimatedHoursNumberBox.Value = existing.EstimatedHours;

            foreach (var c in _clients)
            {
                if (c.Id == existing.ClientId)
                {
                    ClientComboBox.SelectedItem = c;
                    break;
                }
            }

            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (item.Content.ToString() == existing.Status)
                {
                    StatusComboBox.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in PriorityComboBox.Items)
            {
                if (item.Content.ToString() == existing.Priority)
                {
                    PriorityComboBox.SelectedItem = item;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(existing.DueDate) && DateTime.TryParse(existing.DueDate, out var dt))
            {
                DueDatePicker.Date = new DateTimeOffset(dt);
            }
        }
        else
        {
            Title = "Nuevo Trabajo / Tarea";
            if (_clients.Count > 0)
            {
                ClientComboBox.SelectedIndex = 0;
            }
            DueDatePicker.Date = DateTimeOffset.Now.AddDays(7);
        }

        this.PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text) || ClientComboBox.SelectedItem is not Client selectedClient)
        {
            args.Cancel = true;
            return;
        }

        JobModel.Title = TitleTextBox.Text.Trim();
        JobModel.ClientId = selectedClient.Id;
        JobModel.ClientName = selectedClient.Name;
        JobModel.Description = DescriptionTextBox.Text.Trim();
        JobModel.Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Pendiente";
        JobModel.Priority = (PriorityComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Media";
        JobModel.EstimatedHours = double.IsNaN(EstimatedHoursNumberBox.Value) ? 0.0 : EstimatedHoursNumberBox.Value;
        JobModel.DueDate = DueDatePicker.Date.DateTime.ToString("yyyy-MM-dd");
    }
}
