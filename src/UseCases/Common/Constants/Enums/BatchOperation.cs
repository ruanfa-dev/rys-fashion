using ErrorOr;

namespace UseCases.Common.Constants.Enums;

/// <summary>
/// Represents batch operations that can be combined using bitwise operations.
/// </summary>
[Flags]
public enum BatchOperation : int
{
    /// <summary>
    /// No operation.
    /// </summary>
    None = 0,

    /// <summary>
    /// Create items.
    /// </summary>
    Create = 1 << 0,

    /// <summary>
    /// Update items.
    /// </summary>
    Update = 1 << 1,

    /// <summary>
    /// Delete items.
    /// </summary>
    Delete = 1 << 2,

    /// <summary>
    /// Upsert items (create or update).
    /// </summary>
    Upsert = 1 << 3,

    /// <summary>
    /// Read items.
    /// </summary>
    Read = 1 << 4,

    /// <summary>
    /// Patch items (partial update).
    /// </summary>
    Patch = 1 << 5,

    /// <summary>
    /// Archive items.
    /// </summary>
    Archive = 1 << 6,

    /// <summary>
    /// Restore archived items.
    /// </summary>
    Restore = 1 << 7,

    /// <summary>
    /// Validate items.
    /// </summary>
    Validate = 1 << 8,

    /// <summary>
    /// All supported operations.
    /// </summary>
    All = Create | Update | Delete | Upsert | Read | Patch | Archive | Restore | Validate
}
/// <summary>
/// Standardized errors related to BatchOperation parsing/validation.
/// </summary>
public static class BatchOperationErrors
{
    public static Error InvalidOperation(string operation) =>
        Error.Validation(
            "BatchOperation.Invalid",
            $"The batch operation '{operation}' is not recognized. Valid operations are: Create, Update, Delete, Upsert, Read, Patch, Archive, Restore, Validate.");
}