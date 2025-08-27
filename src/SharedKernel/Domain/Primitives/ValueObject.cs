namespace SharedKernel.Domain.Primitives;

/// <summary>
/// Base class for Value Objects that provides equality comparison and hashing.
/// For simple value objects, prefer using record/record struct instead.
/// Use this class when you need complex value objects with custom behavior.
/// 
/// For record usage:
/// - Simple: public record struct Money(decimal Amount, string Currency);
/// - Complex: public record Money(decimal Amount, string Currency) : ValueObject;
/// 
/// References:
/// - https://nietras.com/2021/06/14/csharp-10-record-struct/
/// - https://enterprisecraftsmanship.com/posts/value-object-better-implementation/
/// </summary>
[Serializable]
public abstract class ValueObject : IEquatable<ValueObject>, IComparable<ValueObject>, IComparable
{
    private int? _cachedHashCode;

    /// <summary>
    /// Override this method to return the components that determine equality.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public virtual bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetUnproxiedType(this) != GetUnproxiedType(other)) return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ValueObject);
    }

    public override int GetHashCode()
    {
        if (_cachedHashCode.HasValue)
            return _cachedHashCode.Value;

        _cachedHashCode = GetEqualityComponents()
            .Aggregate(1, (current, obj) =>
            {
                unchecked
                {
                    return current * 23 + (obj?.GetHashCode() ?? 0);
                }
            });

        return _cachedHashCode.Value;
    }

    public virtual int CompareTo(ValueObject? other)
    {
        return CompareTo(other as object);
    }

    public virtual int CompareTo(object? obj)
    {
        if (obj is null) return 1;

        var thisType = GetUnproxiedType(this);
        var otherType = GetUnproxiedType(obj);

        if (thisType != otherType)
            return string.Compare(thisType.ToString(), otherType.ToString(), StringComparison.Ordinal);

        if (obj is not ValueObject other)
            return 1;

        var components = GetEqualityComponents().ToArray();
        var otherComponents = other.GetEqualityComponents().ToArray();

        for (var i = 0; i < Math.Min(components.Length, otherComponents.Length); i++)
        {
            var comparison = CompareComponents(components[i], otherComponents[i]);
            if (comparison != 0)
                return comparison;
        }

        return components.Length.CompareTo(otherComponents.Length);
    }

    private static int CompareComponents(object? object1, object? object2)
    {
        if (object1 is null && object2 is null) return 0;
        if (object1 is null) return -1;
        if (object2 is null) return 1;

        if (object1 is IComparable comparable1 && object2 is IComparable comparable2)
            return comparable1.CompareTo(comparable2);

        return object1.Equals(object2) ? 0 : string.Compare(object1.ToString(), object2.ToString(), StringComparison.Ordinal);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }

    public static bool operator <(ValueObject? left, ValueObject? right)
    {
        return left?.CompareTo(right) < 0;
    }

    public static bool operator <=(ValueObject? left, ValueObject? right)
    {
        return left?.CompareTo(right) <= 0;
    }

    public static bool operator >(ValueObject? left, ValueObject? right)
    {
        return left?.CompareTo(right) > 0;
    }

    public static bool operator >=(ValueObject? left, ValueObject? right)
    {
        return left?.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Gets the actual type, handling EF Core and NHibernate proxies.
    /// </summary>
    internal static Type GetUnproxiedType(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        const string EFCoreProxyPrefix = "Castle.Proxies.";
        const string NHibernateProxyPostfix = "Proxy";

        var type = obj.GetType();
        var typeString = type.ToString();

        if (typeString.Contains(EFCoreProxyPrefix) || typeString.EndsWith(NHibernateProxyPostfix))
            return type.BaseType!;

        return type;
    }

    public override string ToString()
    {
        return $"{GetType().Name} {{ {string.Join(", ", GetEqualityComponents().Select(x => x?.ToString() ?? "null"))} }}";
    }
}

/// <summary>
/// Extension methods to help with ValueObject usage in records.
/// </summary>
public static class ValueObjectExtensions
{
    /// <summary>
    /// Helper method for records to implement GetEqualityComponents easily.
    /// </summary>
    public static IEnumerable<object?> ToEqualityComponents<T>(this T value) where T : notnull
    {
        return typeof(T).GetProperties()
            .Where(p => p.CanRead)
            .Select(p => p.GetValue(value));
    }
}