using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SmartSqlStudio.Models;
using SmartSqlStudio.Services;
using Microsoft.Data.SqlClient;

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

    // Nuove proprietà per i tab e lo schema del database
    [ObservableProperty]
    private ObservableCollection<QueryTab> _queryTabs = new();

    [ObservableProperty]
    private QueryTab? _selectedTab;

    [ObservableProperty]
    private ObservableCollection<string> _availableTables = new();

    [ObservableProperty]
    private ObservableCollection<DatabaseColumn> _selectedTableColumns = new();

    [ObservableProperty]
    private string? _selectedTable;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _showLoginWindow = true;

    public MainWindowViewModel()
    {
        _snippetService = new SnippetService();
        _intelliSenseService = new IntelliSenseService(_snippetService);
        _databaseService = new DatabaseService();
        
        LoadSnippets();
        InitializeNewTab();
    }

    private void InitializeNewTab()
    {
        var newTab = new QueryTab
        {
            Id = Guid.NewGuid().ToString(),
            Title = $"Query {QueryTabs.Count + 1}",
            Query = string.Empty,
            Results = new ObservableCollection<QueryResult>()
        };
        
        QueryTabs.Add(newTab);
        SelectedTab = newTab;
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

    partial void OnSelectedTabChanged(QueryTab? value)
    {
        if (value != null)
        {
            SqlQuery = value.Query;
            QueryResults = value.Results;
        }
    }

    partial void OnSqlQueryChanged(string value)
    {
        if (SelectedTab != null)
        {
            SelectedTab.Query = value;
        }
    }

    [RelayCommand]
    private async Task ExecuteQueryAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
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
            var result = await _databaseService.ExecuteQueryAsync(ConnectionString, SqlQuery);
            
            if (SelectedTab != null)
            {
                SelectedTab.Results.Clear();
                SelectedTab.Results.Add(result);
                QueryResults = SelectedTab.Results;
            }

            if (result.HasError)
            {
                StatusMessage = $"Error: {result.ErrorMessage}";
            }
            else
            {
                StatusMessage = $"Query executed successfully. {result.RowCount} rows returned in {result.ExecutionTime.TotalMilliseconds:F2}ms";
                
                // Aggiorna lo schema dopo un'esecuzione riuscita
                await LoadDatabaseSchemaAsync();
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
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            BuildConnectionString();
        }

        IsExecuting = true;
        StatusMessage = "Testing connection...";

        try
        {
            var success = await _databaseService.TestConnectionAsync(ConnectionString);
            StatusMessage = success ? "Connection successful!" : "Connection failed!";
            
            if (success)
            {
                IsConnected = true;
                ShowLoginWindow = false;
                await LoadDatabaseSchemaAsync();
            }
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
    private async Task ConnectAsync()
    {
        BuildConnectionString();
        await TestConnectionAsync();
    }

    private async Task LoadDatabaseSchemaAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return;

        try
        {
            var tables = await _databaseService.GetTablesAsync(ConnectionString);
            
            AvailableTables.Clear();
            foreach (var table in tables)
            {
                AvailableTables.Add(table);
            }

            // Carica lo schema nell'IntelliSense
            _intelliSenseService.LoadSchemaInfo(ConnectionString, tables);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading schema: {ex.Message}";
        }
    }

    partial void OnSelectedTableChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(ConnectionString))
        {
            LoadTableColumnsAsync(value);
        }
    }

    private async void LoadTableColumnsAsync(string tableName)
    {
        try
        {
            var columns = await _databaseService.GetColumnsAsync(ConnectionString, tableName);
            
            SelectedTableColumns.Clear();
            foreach (var column in columns)
            {
                SelectedTableColumns.Add(new DatabaseColumn { Name = column, TableName = tableName });
            }

            // Aggiorna l'IntelliSense con le colonne della tabella
            _intelliSenseService.UpdateTableColumns(tableName, columns);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading columns: {ex.Message}";
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
    private void NewQueryTab()
    {
        InitializeNewTab();
        StatusMessage = "New query tab created";
    }

    [RelayCommand]
    private void CloseTab(QueryTab? tab)
    {
        if (tab != null && QueryTabs.Count > 1)
        {
            var index = QueryTabs.IndexOf(tab);
            QueryTabs.Remove(tab);
            
            if (SelectedTab == tab)
            {
                SelectedTab = QueryTabs[Math.Max(0, index - 1)];
            }
        }
        StatusMessage = "Tab closed";
    }

    [RelayCommand]
    private void ClearQuery()
    {
        SqlQuery = string.Empty;
        if (SelectedTab != null)
        {
            SelectedTab.Results.Clear();
        }
        QueryResults.Clear();
        StatusMessage = "Query cleared";
    }

    [RelayCommand]
    private void BuildConnectionString()
    {
        if (UseWindowsAuthentication)
        {
            ConnectionString = $"Server={ServerName};Database={DatabaseName};Integrated Security=true;";
        }
        else
        {
            ConnectionString = $"Server={ServerName};Database={DatabaseName};User Id={Username};Password={Password};";
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

    [RelayCommand]
    private void InsertTableName()
    {
        if (!string.IsNullOrEmpty(SelectedTable))
        {
            SqlQuery += $" [{SelectedTable}]";
        }
    }

    [RelayCommand]
    private void InsertColumnName()
    {
        if (SelectedTableColumns.Count > 0)
        {
            // In una implementazione reale, si potrebbe avere una colonna selezionata
            StatusMessage = "Select a column from the list (feature coming soon)";
        }
    }
}

public partial class ViewModelBase : ObservableObject
{
}
