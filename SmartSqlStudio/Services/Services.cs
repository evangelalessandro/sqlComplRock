using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SmartSqlStudio.Models;

namespace SmartSqlStudio.Services;

public interface ISnippetService
{
    List<SqlSnippet> GetAllSnippets();
    SqlSnippet? GetSnippetById(string id);
    void SaveSnippet(SqlSnippet snippet);
    void DeleteSnippet(string id);
    List<SqlSnippet> SearchSnippets(string query);
    List<SqlSnippet> GetSnippetsByCategory(string category);
}

public interface IIntelliSenseService
{
    List<IntelliSenseSuggestion> GetSuggestions(string text, int caretPosition);
    void LoadSchemaInfo(string connectionString);
    void ClearSchemaInfo();
}

public interface IDatabaseService
{
    Task<bool> TestConnectionAsync(string connectionString);
    Task<QueryResult> ExecuteQueryAsync(string connectionString, string query);
    Task<List<string>> GetTablesAsync(string connectionString);
    Task<List<string>> GetColumnsAsync(string connectionString, string tableName);
}

public class SnippetService : ISnippetService
{
    private readonly List<SqlSnippet> _snippets = new();

    public SnippetService()
    {
        InitializeDefaultSnippets();
    }

    private void InitializeDefaultSnippets()
    {
        _snippets.AddRange(new[]
        {
            new SqlSnippet
            {
                Name = "SELECT Basic",
                Description = "Basic SELECT statement",
                Category = "SELECT",
                Content = "SELECT * \nFROM [TableName]\nWHERE [Condition];",
                Keywords = new[] { "select", "query" }
            },
            new SqlSnippet
            {
                Name = "SELECT JOIN",
                Description = "SELECT with INNER JOIN",
                Category = "SELECT",
                Content = "SELECT a.[Column1], b.[Column2]\nFROM [Table1] a\nINNER JOIN [Table2] b ON a.[Key] = b.[Key]\nWHERE [Condition];",
                Keywords = new[] { "select", "join", "inner" }
            },
            new SqlSnippet
            {
                Name = "INSERT Statement",
                Description = "INSERT INTO statement",
                Category = "INSERT",
                Content = "INSERT INTO [TableName] ([Column1], [Column2])\nVALUES (@Value1, @Value2);",
                Keywords = new[] { "insert", "add" }
            },
            new SqlSnippet
            {
                Name = "UPDATE Statement",
                Description = "UPDATE with WHERE clause",
                Category = "UPDATE",
                Content = "UPDATE [TableName]\nSET [Column1] = @Value1, [Column2] = @Value2\nWHERE [Condition];",
                Keywords = new[] { "update", "modify" }
            },
            new SqlSnippet
            {
                Name = "DELETE Statement",
                Description = "DELETE with WHERE clause",
                Category = "DELETE",
                Content = "DELETE FROM [TableName]\nWHERE [Condition];",
                Keywords = new[] { "delete", "remove" }
            },
            new SqlSnippet
            {
                Name = "CREATE TABLE",
                Description = "Create new table",
                Category = "DDL",
                Content = "CREATE TABLE [TableName]\n(\n    [Id] INT PRIMARY KEY IDENTITY(1,1),\n    [Column1] NVARCHAR(100) NOT NULL,\n    [Column2] DATETIME DEFAULT GETDATE()\n);",
                Keywords = new[] { "create", "table", "ddl" }
            },
            new SqlSnippet
            {
                Name = "Stored Procedure Template",
                Description = "Template for creating stored procedures",
                Category = "Programmability",
                Content = "CREATE PROCEDURE [dbo].[ProcedureName]\n    @Parameter1 DataType,\n    @Parameter2 DataType\nAS\nBEGIN\n    SET NOCOUNT ON;\n    \n    -- Your logic here\n    SELECT * FROM [TableName]\n    WHERE [Condition];\nEND",
                Keywords = new[] { "procedure", "sp", "programmability" }
            },
            new SqlSnippet
            {
                Name = "CTE Example",
                Description = "Common Table Expression example",
                Category = "Advanced",
                Content = "WITH CTE_Name AS (\n    SELECT [Column1], [Column2]\n    FROM [TableName]\n    WHERE [Condition]\n)\nSELECT * FROM CTE_Name;",
                Keywords = new[] { "cte", "with", "advanced" }
            },
            new SqlSnippet
            {
                Name = "Window Function ROW_NUMBER",
                Description = "ROW_NUMBER window function",
                Category = "Advanced",
                Content = "SELECT \n    [Column1],\n    ROW_NUMBER() OVER (PARTITION BY [PartitionColumn] ORDER BY [OrderColumn]) AS RowNum\nFROM [TableName];",
                Keywords = new[] { "window", "row_number", "advanced" }
            },
            new SqlSnippet
            {
                Name = "TRY CATCH Block",
                Description = "Error handling with TRY CATCH",
                Category = "Programmability",
                Content = "BEGIN TRY\n    -- Your SQL statements here\n    SELECT * FROM [TableName];\nEND TRY\nBEGIN CATCH\n    SELECT \n        ERROR_NUMBER() AS ErrorNumber,\n        ERROR_MESSAGE() AS ErrorMessage;\nEND CATCH;",
                Keywords = new[] { "try", "catch", "error", "handling" }
            }
        });
    }

    public List<SqlSnippet> GetAllSnippets() => _snippets.ToList();

    public SqlSnippet? GetSnippetById(string id) => 
        _snippets.FirstOrDefault(s => s.Id == id);

    public void SaveSnippet(SqlSnippet snippet)
    {
        var existing = _snippets.FirstOrDefault(s => s.Id == snippet.Id);
        if (existing != null)
        {
            snippet.ModifiedDate = DateTime.Now;
            var index = _snippets.IndexOf(existing);
            _snippets[index] = snippet;
        }
        else
        {
            _snippets.Add(snippet);
        }
    }

    public void DeleteSnippet(string id)
    {
        var snippet = _snippets.FirstOrDefault(s => s.Id == id);
        if (snippet != null)
        {
            _snippets.Remove(snippet);
        }
    }

    public List<SqlSnippet> SearchSnippets(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _snippets.ToList();

        var lowerQuery = query.ToLower();
        return _snippets.Where(s => 
            s.Name.ToLower().Contains(lowerQuery) ||
            s.Description.ToLower().Contains(lowerQuery) ||
            s.Content.ToLower().Contains(lowerQuery) ||
            s.Keywords.Any(k => k.ToLower().Contains(lowerQuery))
        ).ToList();
    }

    public List<SqlSnippet> GetSnippetsByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return _snippets.ToList();

        return _snippets.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

public class IntelliSenseService : IIntelliSenseService
{
    private readonly SnippetService _snippetService;
    private readonly List<string> _sqlKeywords = new()
    {
        "SELECT", "FROM", "WHERE", "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN",
        "CROSS JOIN", "OUTER JOIN", "ON", "AND", "OR", "NOT", "IN", "BETWEEN", "LIKE", "EXISTS",
        "ORDER BY", "GROUP BY", "HAVING", "DISTINCT", "TOP", "LIMIT", "OFFSET", "AS", "ASC", "DESC",
        "NULL", "IS NULL", "IS NOT NULL", "CASE", "WHEN", "THEN", "ELSE", "END", "CAST", "CONVERT",
        "COALESCE", "NULLIF", "IIF", "CHOOSE", "CONCAT", "SUBSTRING", "LEN", "UPPER", "LOWER",
        "LTRIM", "RTRIM", "TRIM", "REPLACE", "CHARINDEX", "PATINDEX", "FORMAT", "PARSE", "TRY_PARSE",
        "GETDATE", "SYSDATETIME", "SYSUTCDATETIME", "DATEADD", "DATEDIFF", "DATENAME", "DATEPART",
        "DAY", "MONTH", "YEAR", "COUNT", "SUM", "AVG", "MIN", "MAX", "STDEV", "VAR", "ROW_NUMBER",
        "RANK", "DENSE_RANK", "NTILE", "LAG", "LEAD", "FIRST_VALUE", "LAST_VALUE", "PERCENT_RANK",
        "CUME_DIST", "OVER", "PARTITION BY", "UNBOUNDED PRECEDING", "UNBOUNDED FOLLOWING", "CURRENT ROW",
        "PIVOT", "UNPIVOT", "MERGE", "OUTPUT", "TABLESAMPLE", "WAITFOR", "DELAY", "TIME",
        "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE", "TRUNCATE", "DROP", "ALTER", "CREATE",
        "PROCEDURE", "FUNCTION", "VIEW", "INDEX", "PRIMARY KEY", "FOREIGN KEY", "REFERENCES",
        "CONSTRAINT", "DEFAULT", "CHECK", "UNIQUE", "IDENTITY", "COMPUTE", "STATISTICS",
        "EXECUTE", "EXEC", "DECLARE", "SET", "BEGIN", "END", "IF", "ELSE", "WHILE", "BREAK",
        "CONTINUE", "RETURN", "RAISERROR", "THROW", "TRY", "CATCH", "FINALLY", "GOTO", "LABEL",
        "WAITFOR", "BULK INSERT", "OPENQUERY", "OPENDATASOURCE", "OPENROWSET", "OPENXML",
        "READTEXT", "WRITETEXT", "UPDATETEXT", "DBCC", "KILL", "SHUTDOWN", "CHECKPOINT",
        "COMMIT", "ROLLBACK", "SAVE TRANSACTION", "SET TRANSACTION ISOLATION LEVEL",
        "READ UNCOMMITTED", "READ COMMITTED", "REPEATABLE READ", "SERIALIZABLE", "SNAPSHOT",
        "WITH", "CTE", "RECURSIVE", "UNION", "UNION ALL", "INTERSECT", "EXCEPT", "ALL", "ANY", "SOME"
    };

    private readonly List<string> _tables = new();
    private readonly Dictionary<string, List<string>> _columns = new();

    public IntelliSenseService(SnippetService snippetService)
    {
        _snippetService = snippetService;
    }

    public void LoadSchemaInfo(string connectionString)
    {
        // In a real implementation, this would connect to the database
        // and load schema information
        ClearSchemaInfo();
    }

    public void ClearSchemaInfo()
    {
        _tables.Clear();
        _columns.Clear();
    }

    public List<IntelliSenseSuggestion> GetSuggestions(string text, int caretPosition)
    {
        var suggestions = new List<IntelliSenseSuggestion>();
        
        if (caretPosition <= 0 || caretPosition > text.Length)
            return suggestions;

        // Get the word being typed
        var wordStart = caretPosition - 1;
        while (wordStart > 0 && char.IsLetterOrDigit(text[wordStart - 1]))
            wordStart--;

        var currentWord = text.Substring(wordStart, caretPosition - wordStart).ToUpper();

        if (string.IsNullOrWhiteSpace(currentWord))
            return suggestions;

        // Add SQL keywords
        foreach (var keyword in _sqlKeywords.Where(k => k.StartsWith(currentWord)))
        {
            suggestions.Add(new IntelliSenseSuggestion
            {
                Text = keyword,
                DisplayText = keyword,
                Description = $"SQL Keyword: {keyword}",
                Type = SuggestionType.Keyword,
                Priority = 100
            });
        }

        // Add tables
        foreach (var table in _tables.Where(t => t.ToUpper().StartsWith(currentWord)))
        {
            suggestions.Add(new IntelliSenseSuggestion
            {
                Text = table,
                DisplayText = table,
                Description = "Table",
                Type = SuggestionType.Table,
                Priority = 90
            });
        }

        // Add snippets
        var snippetSuggestions = _snippetService.SearchSnippets(currentWord.ToLower())
            .Select(s => new IntelliSenseSuggestion
            {
                Text = s.Content,
                DisplayText = s.Name,
                Description = s.Description,
                Type = SuggestionType.Snippet,
                Priority = 80
            });
        suggestions.AddRange(snippetSuggestions);

        // Sort by priority and relevance
        return suggestions
            .OrderByDescending(s => s.Priority)
            .ThenBy(s => s.DisplayText.Length)
            .Take(20)
            .ToList();
    }
}

public class DatabaseService : IDatabaseService
{
    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();
            return connection.State == System.Data.ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }

    public async Task<QueryResult> ExecuteQueryAsync(string connectionString, string query)
    {
        var result = new QueryResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.CommandTimeout = 300;

            using var reader = await command.ExecuteReaderAsync();
            
            // Get columns
            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(reader.GetName(i));
            }

            // Get rows
            while (await reader.ReadAsync())
            {
                var row = new List<object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row.Add(reader.IsDBNull(i) ? null : reader.GetValue(i));
                }
                result.Rows.Add(row);
            }

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
        }

        return result;
    }

    public async Task<List<string>> GetTablesAsync(string connectionString)
    {
        var tables = new List<string>();
        try
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            var schema = await connection.GetSchemaAsync("Tables");
            foreach (System.Data.DataRow row in schema.Rows)
            {
                var tableName = row["TABLE_NAME"].ToString();
                if (!string.IsNullOrEmpty(tableName))
                    tables.Add(tableName);
            }
        }
        catch
        {
            // Return empty list on error
        }

        return tables;
    }

    public async Task<List<string>> GetColumnsAsync(string connectionString, string tableName)
    {
        var columns = new List<string>();
        try
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            var schema = await connection.GetSchemaAsync("Columns", new[] { null, null, tableName, null });
            foreach (System.Data.DataRow row in schema.Rows)
            {
                var columnName = row["COLUMN_NAME"].ToString();
                if (!string.IsNullOrEmpty(columnName))
                    columns.Add(columnName);
            }
        }
        catch
        {
            // Return empty list on error
        }

        return columns;
    }
}
