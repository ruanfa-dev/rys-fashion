using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Resolvers;

public static class ViewableResolver
{
    /// <summary>
    /// Resolve a polymorphic viewable (stored as a string type and id) to the actual entity instance.
    /// Uses assembly/type name heuristics then DbContext.FindAsync(Type, key).
    /// </summary>
    public static async Task<object?> ResolveAsync(DbContext dbContext, string viewableType, Guid? viewableId)
    {
        if (dbContext is null) throw new ArgumentNullException(nameof(dbContext));
        if (viewableId == null) return null;
        if (string.IsNullOrWhiteSpace(viewableType)) return null;

        // Try several name forms. Example conversion: "Spree::Variant" -> "Variant"
        string shortName = viewableType.Split([':', '/', '.'], StringSplitOptions.RemoveEmptyEntries).Last();

        // Look for a CLR type with matching short name or full name
        Type? type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
            })
            .FirstOrDefault(t =>
                string.Equals(t.Name, shortName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.FullName, viewableType, StringComparison.OrdinalIgnoreCase) ||
                (t.FullName?.EndsWith("." + shortName, StringComparison.OrdinalIgnoreCase) ?? false));

        if (type == null) return null;

        // Use DbContext.FindAsync(Type, key) to load the entity by primary key (works for tracked/untracked)
        object? found = await dbContext.FindAsync(type, [viewableId.Value]);
        return found;
    }
}