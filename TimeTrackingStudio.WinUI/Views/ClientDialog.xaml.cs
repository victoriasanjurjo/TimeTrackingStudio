using Microsoft.UI.Xaml.Controls;
using TimeTrackingStudio.WinUI.Models;

namespace TimeTrackingStudio.WinUI.Views;

public sealed partial class ClientDialog : ContentDialog
{
    public Client ClientModel { get; private set; }

    public ClientDialog(Client? existing = null)
    {
        this.InitializeComponent();
        ClientModel = existing ?? new Client();

        if (existing != null)
        {
            Title = "Editar Cliente";
            NameTextBox.Text = existing.Name;
            CompanyTextBox.Text = existing.Company;
            EmailTextBox.Text = existing.Email;
            PhoneTextBox.Text = existing.Phone;
            HourlyRateNumberBox.Value = existing.HourlyRate;
            NotesTextBox.Text = existing.Notes;

            foreach (ComboBoxItem item in CurrencyComboBox.Items)
            {
                if (item.Content.ToString() == existing.Currency)
                {
                    CurrencyComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        else
        {
            Title = "Nuevo Cliente";
        }

        this.PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            args.Cancel = true;
            return;
        }

        ClientModel.Name = NameTextBox.Text.Trim();
        ClientModel.Company = CompanyTextBox.Text.Trim();
        ClientModel.Email = EmailTextBox.Text.Trim();
        ClientModel.Phone = PhoneTextBox.Text.Trim();
        ClientModel.HourlyRate = double.IsNaN(HourlyRateNumberBox.Value) ? 0.0 : HourlyRateNumberBox.Value;
        ClientModel.Currency = (CurrencyComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "USD";
        ClientModel.Notes = NotesTextBox.Text.Trim();
    }
}
