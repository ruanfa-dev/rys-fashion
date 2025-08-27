using System.Linq.Expressions;

namespace SharedKernel.Models.Search;
/// <summary>
/// Fluent builder for search operations.
/// </summary>
public class SearchBuilder<T>
{
    private readonly IQueryable<T> _query;
    private readonly string _searchTerm;
    private readonly List<Expression<Func<T, string>>> _searchFields = new();
    private SearchOptions _options = new();

    internal SearchBuilder(IQueryable<T> query, string searchTerm)
    {
        _query = query;
        _searchTerm = searchTerm;
    }

    public SearchBuilder<T> In(Expression<Func<T, string>> field)
    {
        _searchFields.Add(field);
        return this;
    }

    public SearchBuilder<T> In(params Expression<Func<T, string>>[] fields)
    {
        _searchFields.AddRange(fields);
        return this;
    }

    public SearchBuilder<T> CaseSensitive(bool caseSensitive = true)
    {
        _options = _options with { CaseSensitive = caseSensitive };
        return this;
    }

    public SearchBuilder<T> ExactMatch(bool exactMatch = true)
    {
        _options = _options with { ExactMatch = exactMatch };
        return this;
    }

    public SearchBuilder<T> StartsWith(bool startsWith = true)
    {
        _options = _options with { StartsWith = startsWith };
        return this;
    }

    public IQueryable<T> Execute()
    {
        if (string.IsNullOrWhiteSpace(_searchTerm))
            return _query;

        if (_searchFields.Count != 0)
        {
            return _query.ApplySearch(_searchTerm, _searchFields.ToArray());
        }
        else
        {
            var searchParams = new SearchParameter(_searchTerm);
            return _query.ApplySearch(searchParams, _options);
        }
    }

    public static implicit operator Func<IQueryable<T>>(SearchBuilder<T> builder) => builder.Execute;
}