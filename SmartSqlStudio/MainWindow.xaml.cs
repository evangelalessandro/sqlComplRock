using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace SmartSQLStudio
{
    public class ColumnInfo
    {
        public string Table { get; set; } = "";
        public string Column { get; set; } = "";
    }

    public partial class MainWindow : Window
    {
        private readonly string _connectionString;
        private readonly string _server;
        private readonly string _initialDatabase;
        private int _tabCounter = 0;
        
        // IntelliSense data
        private List<string> _tables = new();
        private Dictionary<string, List<string>> _tableColumns = new();
        private List<string> _keywords = new();
        private List<Snippet> _snippets = new();

        public MainWindow(string connectionString, string server, string database)
        {
            InitializeComponent();
            
            _connectionString = connectionString;
            _server = server;
            _initialDatabase = database;
            
            Title = $"Smart SQL Studio - {_server} ({_initialDatabase})";
            
            InitializeKeywords();
            InitializeSnippets();
            LoadDatabaseSchema();
            
            // Crea primo tab
            CreateNewTab();
            
            StatusTextBlock.Text = "Pronto";
        }

        private void InitializeKeywords()
        {
            _keywords = new List<string>
            {
                // SQL Keywords
                "SELECT", "FROM", "WHERE", "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN",
                "INSERT", "UPDATE", "DELETE", "CREATE", "ALTER", "DROP", "TRUNCATE",
                "TABLE", "VIEW", "INDEX", "PROCEDURE", "FUNCTION", "TRIGGER",
                "DATABASE", "SCHEMA", "COLUMN", "KEY", "CONSTRAINT", "FOREIGN KEY", "PRIMARY KEY",
                "GROUP BY", "ORDER BY", "HAVING", "DISTINCT", "TOP", "LIMIT",
                "AS", "AND", "OR", "NOT", "IN", "BETWEEN", "LIKE", "EXISTS",
                "NULL", "IS NULL", "IS NOT NULL", "CASE", "WHEN", "THEN", "ELSE", "END",
                "UNION", "UNION ALL", "INTERSECT", "EXCEPT",
                "COUNT", "SUM", "AVG", "MIN", "MAX", "CAST", "CONVERT",
                "ASC", "DESC", "EXEC", "EXECUTE", "DECLARE", "SET",
                "BEGIN", "COMMIT", "ROLLBACK", "TRANSACTION", "TRY", "CATCH", "THROW",
                "IF", "ELSE", "WHILE", "FOR", "RETURN", "BREAK", "CONTINUE",
                "WITH", "CTE", "RECURSIVE", "MERGE", "OUTPUT", "PIVOT", "UNPIVOT"
            };
        }

        private void InitializeSnippets()
        {
            _snippets = new List<Snippet>
            {
                new Snippet { Name = "SELECT *", Code = "SELECT * \nFROM [TableName]\nWHERE [Condition];", Description = "Select base da tabella" },
                new Snippet { Name = "INSERT", Code = "INSERT INTO [TableName] ([Column1], [Column2])\nVALUES (@Value1, @Value2);", Description = "Insert con parametri" },
                new Snippet { Name = "UPDATE", Code = "UPDATE [TableName]\nSET [Column1] = @Value1\nWHERE [KeyColumn] = @KeyValue;", Description = "Update con WHERE" },
                new Snippet { Name = "DELETE", Code = "DELETE FROM [TableName]\nWHERE [Condition];", Description = "Delete con condizione" },
                new Snippet { Name = "CREATE TABLE", Code = "CREATE TABLE [TableName]\n(\n    [Id] INT IDENTITY(1,1) PRIMARY KEY,\n    [Name] NVARCHAR(100) NOT NULL,\n    [CreatedDate] DATETIME DEFAULT GETDATE()\n);", Description = "Crea nuova tabella" },
                new Snippet { Name = "JOIN", Code = "SELECT a.*, b.*\nFROM [TableA] a\nINNER JOIN [TableB] b ON a.[KeyId] = b.[Id]\nWHERE [Condition];", Description = "Inner Join tra tabelle" },
                new Snippet { Name = "GROUP BY", Code = "SELECT [Column], COUNT(*) AS Count\nFROM [TableName]\nGROUP BY [Column]\nHAVING COUNT(*) > 1;", Description = "Group By con Having" },
                new Snippet { Name = "CTE", Code = "WITH CTE_Name AS (\n    SELECT [Column]\n    FROM [TableName]\n    WHERE [Condition]\n)\nSELECT * FROM CTE_Name;", Description = "Common Table Expression" },
                new Snippet { Name = "STORED PROC", Code = "CREATE PROCEDURE [dbo].[ProcedureName]\n    @Parameter1 DataType,\n    @Parameter2 DataType\nAS\nBEGIN\n    SET NOCOUNT ON;\n    -- Codice qui\nEND", Description = "Stored Procedure template" },
                new Snippet { Name = "TRY CATCH", Code = "BEGIN TRY\n    -- Codice che può fallire\nEND TRY\nBEGIN CATCH\n    SELECT \n        ERROR_NUMBER() AS ErrorNumber,\n        ERROR_MESSAGE() AS ErrorMessage;\nEND CATCH", Description = "Gestione errori Try Catch" }
            };
            
            SnippetListBox.ItemsSource = _snippets;
        }

        private async void LoadDatabaseSchema()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Carica tabelle
                var tablesQuery = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME";
                
                using var cmd = new SqlCommand(tablesQuery, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                
                DatabaseTreeView.Items.Clear();
                var tablesNode = new TreeViewItem { Header = "📊 Tabelle", IsExpanded = true };
                
                while (await reader.ReadAsync())
                {
                    var tableName = reader.GetString(0);
                    _tables.Add(tableName);
                    
                    var tableNode = new TreeViewItem 
                    { 
                        Header = $"📋 {tableName}",
                        Tag = tableName
                    };
                    
                    // Carica colonne per ogni tabella
                    var columnsQuery = @"
                        SELECT COLUMN_NAME, DATA_TYPE 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = @TableName
                        ORDER BY ORDINAL_POSITION";
                    
                    using var colCmd = new SqlCommand(columnsQuery, connection);
                    colCmd.Parameters.AddWithValue("@TableName", tableName);
                    
                    using var colReader = await colCmd.ExecuteReaderAsync();
                    var columns = new List<string>();
                    
                    while (await colReader.ReadAsync())
                    {
                        var columnName = colReader.GetString(0);
                        var dataType = colReader.GetString(1);
                        columns.Add(columnName);
                        
                        var columnNode = new TreeViewItem 
                        { 
                            Header = $"{columnName} ({dataType})",
                            Tag = new ColumnInfo { Table = tableName, Column = columnName }
                        };
                        tableNode.Items.Add(columnNode);
                    }
                    
                    _tableColumns[tableName] = columns;
                    tablesNode.Items.Add(tableNode);
                }
                
                DatabaseTreeView.Items.Add(tablesNode);
                
                // Aggiungi nodo per le viste
                var viewsQuery = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.VIEWS 
                    ORDER BY TABLE_NAME";
                
                using var viewsCmd = new SqlCommand(viewsQuery, connection);
                using var viewsReader = await viewsCmd.ExecuteReaderAsync();
                
                var viewsNode = new TreeViewItem { Header = "👁️ Viste", IsExpanded = false };
                while (await viewsReader.ReadAsync())
                {
                    var viewName = viewsReader.GetString(0);
                    viewsNode.Items.Add(new TreeViewItem { Header = $"👁️ {viewName}", Tag = viewName });
                }
                DatabaseTreeView.Items.Add(viewsNode);
                
                StatusTextBlock.Text = $"Schema caricato: {_tables.Count} tabelle";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Errore caricamento schema: {ex.Message}";
            }
        }

        private void CreateNewTab()
        {
            _tabCounter++;
            var tabItem = new TabItem 
            { 
                Header = $"Query {_tabCounter}",
                Content = CreateEditorControl(),
                Tag = new QueryTabData { Id = _tabCounter }
            };
            
            MainTabControl.Items.Add(tabItem);
            MainTabControl.SelectedItem = tabItem;
        }

        private Grid CreateEditorControl()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Editor ScintillaNET (placeholder - in produzione usare Scintilla vero)
            var textBox = new TextBox
            {
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 14,
                AcceptsReturn = true,
                AcceptsTab = true,
                TextWrapping = TextWrapping.NoWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = FindResource("PanelBrush") as System.Windows.Media.Brush,
                Foreground = FindResource("TextBrush") as System.Windows.Media.Brush,
                CaretBrush = System.Windows.Media.Brushes.White,
                Padding = new Thickness(10),
                Tag = "SQLEditor"
            };
            
            textBox.TextChanged += SqlEditor_TextChanged;
            textBox.PreviewKeyDown += SqlEditor_PreviewKeyDown;
            
            grid.Children.Add(textBox);
            Grid.SetRow(textBox, 0);
            
            // Status bar per il tab
            var statusBar = new StackPanel 
            { 
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5)
            };
            
            var lineInfo = new TextBlock 
            { 
                Text = "Ln 1, Col 1",
                Opacity = 0.7,
                FontSize = 12
            };
            
            textBox.TextChanged += (s, e) => {
                var caretIndex = textBox.CaretIndex;
                var lines = textBox.Text.Substring(0, caretIndex).Split('\n');
                var line = lines.Length;
                var col = lines.Last().Length + 1;
                lineInfo.Text = $"Ln {line}, Col {col}";
            };
            
            statusBar.Children.Add(lineInfo);
            grid.Children.Add(statusBar);
            Grid.SetRow(statusBar, 1);
            
            return grid;
        }

        private void SqlEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Qui si implementerebbe l'evidenziazione sintassi
            // Per ora lasciamo il testo normale
        }

        private void SqlEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                // Inserisci spazi invece di tab
                if (sender is TextBox textBox)
                {
                    var selectedStart = textBox.SelectionStart;
                    var selectedEnd = textBox.SelectionStart + textBox.SelectionLength;
                    textBox.Text = textBox.Text.Remove(selectedStart, textBox.SelectionLength)
                                                        .Insert(selectedStart, "    ");
                    textBox.SelectionStart = selectedStart + 4;
                    textBox.SelectionLength = 0;
                    e.Handled = true;
                }
            }
            
            // Ctrl+Spazio per IntelliSense
            if (e.Key == Key.Space && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                ShowIntelliSense();
                e.Handled = true;
            }
        }

        private void ShowIntelliSense()
        {
            // Trova la TextBox corrente nel tab selezionato
            if (MainTabControl.SelectedItem is not TabItem selectedTab)
                return;
                
            var editorGrid = selectedTab.Content as Grid;
            var textBox = editorGrid?.Children.OfType<TextBox>().FirstOrDefault(t => t.Tag?.ToString() == "SQLEditor");
            
            if (textBox == null)
                return;

            // Mostra popup con suggerimenti
            var suggestionsWindow = new IntelliSenseWindow(_keywords, _tables, _snippets);
            suggestionsWindow.Owner = this;
            suggestionsWindow.ShowDialog();
            
            if (suggestionsWindow.SelectedValue != null)
            {
                textBox.SelectedText = suggestionsWindow.SelectedValue;
            }
        }

        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainTabControl.SelectedItem is not TabItem selectedTab)
                return;
                
            var editorGrid = selectedTab.Content as Grid;
            var textBox = editorGrid?.Children.OfType<TextBox>().FirstOrDefault(t => t.Tag?.ToString() == "SQLEditor");
            
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text))
                return;
            
            var query = textBox.Text;
            ExecuteButton.IsEnabled = false;
            StatusTextBlock.Text = "Esecuzione in corso...";
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new SqlCommand(query, connection);
                command.CommandTimeout = 300;
                
                using var adapter = new SqlDataAdapter(command);
                var dataTable = new DataTable();
                await Task.Run(() => adapter.Fill(dataTable));
                
                stopwatch.Stop();
                
                // Mostra risultati
                ResultsDataGrid.ItemsSource = dataTable.DefaultView;
                ResultTab.IsSelected = true;
                
                StatusTextBlock.Text = $"Eseguito in {stopwatch.ElapsedMilliseconds}ms - {dataTable.Rows.Count} righe";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                StatusTextBlock.Text = $"Errore: {ex.Message}";
                MessageBox.Show(ex.Message, "Errore esecuzione", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ExecuteButton.IsEnabled = true;
            }
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewTab();
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TabItem tabItem)
            {
                MainTabControl.Items.Remove(tabItem);
            }
        }

        private void SnippetListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SnippetListBox.SelectedItem is Snippet snippet && 
                MainTabControl.SelectedItem is TabItem selectedTab)
            {
                var editorGrid = selectedTab.Content as Grid;
                var textBox = editorGrid?.Children.OfType<TextBox>().FirstOrDefault(t => t.Tag?.ToString() == "SQLEditor");
                
                if (textBox != null)
                {
                    textBox.SelectedText = snippet.Code;
                }
            }
        }

        private void SearchSnippetTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox searchBox)
            {
                var filter = searchBox.Text.ToLower();
                var filtered = _snippets.Where(s => 
                    s.Name.ToLower().Contains(filter) || 
                    s.Description.ToLower().Contains(filter)
                ).ToList();
                
                SnippetListBox.ItemsSource = filtered;
            }
        }

        private void DatabaseTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DatabaseTreeView.SelectedItem is TreeViewItem item && 
                item.Tag != null &&
                MainTabControl.SelectedItem is TabItem selectedTab)
            {
                var editorGrid = selectedTab.Content as Grid;
                var textBox = editorGrid?.Children.OfType<TextBox>().FirstOrDefault(t => t.Tag?.ToString() == "SQLEditor");
                
                if (textBox != null)
                {
                    if (item.Tag is string tableName)
                    {
                        textBox.SelectedText = $"[{tableName}]";
                    }
                    else if (item.Tag is ColumnInfo columnInfo)
                    {
                        textBox.SelectedText = $"[{columnInfo.Table}].[{columnInfo.Column}]";
                    }
                }
            }
        }
    }

    public class Snippet
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class QueryTabData
    {
        public int Id { get; set; }
    }
}
