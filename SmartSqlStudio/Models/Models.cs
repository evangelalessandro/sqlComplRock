using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSqlStudio.Models;

public class SqlSnippet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Content { get; set; } = string.Empty;
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
}

public class DatabaseConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; } = true;
}

public class IntelliSenseSuggestion
{
    public string Text { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SuggestionType Type { get; set; } = SuggestionType.Keyword;
    public int Priority { get; set; } = 0;
}

public enum SuggestionType
{
    Keyword,
    Table,
    Column,
    Function,
    StoredProcedure,
    Snippet,
    Variable
}

public class QueryResult
{
    public List<string> Columns { get; set; } = new();
    public List<List<object?>> Rows { get; set; } = new();
    public int RowCount => Rows.Count;
    public TimeSpan ExecutionTime { get; set; }
    public string? ErrorMessage { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
}
