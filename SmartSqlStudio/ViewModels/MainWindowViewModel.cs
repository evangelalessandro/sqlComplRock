using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SmartSqlStudio.Models;
using SmartSqlStudio.Services;

namespace SmartSqlStudio.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly SnippetService _snippetService;
    private readonly IntelliSenseService _intelliSenseService;
    private readonly DatabaseService _databaseService;

    [ObservableProperty]
    private string _sqlQuery = string.Empty;

    [ObservableProperty]
    private string _connectionString = string.Empty;

    [ObservableProperty]
    private string _serverName = string.Empty;

    [ObservableProperty]
    private string _databaseName = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _useWindowsAuthentication = true;

    [ObservableProperty]
    private ObservableCollection<SqlSnippet> _snippets = new();

    [ObservableProperty]
    private SqlSnippet? _selectedSnippet;

    [ObservableProperty]
    private ObservableCollection<QueryResult> _queryResults = new();

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public MainWindowViewModel()
    {
        _snippetService = new SnippetService();
        _intelliSenseService = new IntelliSenseService(_snippetService);
        _databaseService = new DatabaseService();
        
        LoadSnippets();
    }

    private void LoadSnippets()
    {
        Snippets.Clear();
        foreach (var snippet in _snippetService.GetAllSnippets())
        {
            Snippets.Add(snippet);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        Snippets.Clear();
        foreach (var snippet in _snippetService.SearchSnippets(value))
        {
            Snippets.Add(snippet);
        }
    }

    [RelayCommand]
    private async Task ExecuteQueryAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionSting))
        {
            StatusMessage = "Please enter a connection string";
            return;
        }

        if (string.IsNullOrWhiteSpace(SqlQuery))
        {
            StatusMessage = "Please enter a SQL query";
            return;
        }

        IsExecuting = true;
        StatusMessage = "Executing query...";

        try
        {
            var result = await _databaseService.ExecuteQueryAsync(ConnectionSting, SqlQuery);
            
            QueryResults.Clear();
            QueryResults.Add(result);

            if (result.HasError)
            {
                StatusMessage = $"Error: {result.ErrorMessage}";
            }
            else
            {
                StatusMessage = $"Query executed successfully. {result.RowCount} rows returned in {result.ExecutionTime.TotalMilliseconds:F2}ms";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionSting))
        {
            BuildConnectionString();
        }

        IsExecuting = true;
        StatusMessage = "Testing connection...";

        try
        {
            var success = await _databaseService.TestConnectionAsync(ConnectionSting);
            StatusMessage = success ? "Connection successful!" : "Connection failed!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection error: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }

    [RelayCommand]
    private void InsertSnippet()
    {
        if (SelectedSnippet != null)
        {
            SqlQuery += Environment.NewLine + SelectedSnippet.Content;
        }
    }

    [RelayCommand]
    private void ClearQuery()
    {
        SqlQuery = string.Empty;
        QueryResults.Clear();
        StatusMessage = "Query cleared";
    }

    [RelayCommand]
    private void BuildConnectionString()
    {
        if (UseWindowsAuthentication)
        {
            ConnectionSting = $"Server={ServerName};Database={DatabaseName};Integrated Security=true;";
        }
        else
        {
            ConnectionSting = $"Server={ServerName};Database={DatabaseName};User Id={Username};Password={Password};";
        }
    }

    [RelayCommand]
    private void SaveSnippet()
    {
        // Implementation for saving custom snippets
        StatusMessage = "Snippet saved (feature coming soon)";
    }

    [RelayCommand]
    private void OpenSnippetManager()
    {
        // Implementation for opening snippet manager
        StatusMessage = "Snippet Manager (feature coming soon)";
    }
}

public partial class ViewModelBase : ObservableObject
{
}
