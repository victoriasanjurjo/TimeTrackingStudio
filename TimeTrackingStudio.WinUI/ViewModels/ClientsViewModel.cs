using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTrackingStudio.WinUI.Models;
using TimeTrackingStudio.WinUI.Services;

namespace TimeTrackingStudio.WinUI.ViewModels;

/// <summary>
/// ViewModel para el catálogo y administración de clientes comerciales.
/// </summary>
public partial class ClientsViewModel : ObservableObject
{
    private readonly ClientService _clientService;

    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public ClientsViewModel(ClientService? clientService = null)
    {
        _clientService = clientService ?? new ClientService();
        LoadClients();
    }

    [RelayCommand]
    public void LoadClients()
    {
        var list = _clientService.GetAllClients(SearchQuery);
        Clients.Clear();
        foreach (var c in list)
        {
            Clients.Add(c);
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        LoadClients();
    }

    public void AddClient(Client client)
    {
        _clientService.AddClient(client);
        LoadClients();
    }

    public void UpdateClient(Client client)
    {
        _clientService.UpdateClient(client);
        LoadClients();
    }

    [RelayCommand]
    public void DeleteClient(Client? client)
    {
        if (client != null)
        {
            _clientService.DeleteClient(client.Id);
            LoadClients();
        }
    }
}
