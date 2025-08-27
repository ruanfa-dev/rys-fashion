namespace SharedKernel.Models.Sort;
/// <summary>
/// Fluent builder for sorting operations.
/// </summary>
public class SortBuilder<T>
{
    private readonly IQueryable<T> _query;
    private readonly List<SortParams> _sortParams = new();

    internal SortBuilder(IQueryable<T> query)
    {
        _query = query;
    }

    public SortBuilder<T> By(string field, string order = "asc")
    {
        _sortParams.Add(new SortParams(field, order));
        return this;
    }

    public SortBuilder<T> ByDescending(string field)
    {
        return By(field, "desc");
    }

    public SortBuilder<T> ThenBy(string field, string order = "asc")
    {
        return By(field, order);
    }

    public SortBuilder<T> ThenByDescending(string field)
    {
        return By(field, "desc");
    }

    public IQueryable<T> Execute()
    {
        return _query.ApplySort(_sortParams.ToArray());
    }

    public static implicit operator Func<IQueryable<T>>(SortBuilder<T> builder) => builder.Execute;
}

