namespace UseCases.Todos.Lists.Common;

public record TodoListResult : TodoListParam
{
    public int Id { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public record TodoResult : TodoListResult
{
    #region Audits
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    #endregion
}

public record TodoListSelectItemResult
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}
