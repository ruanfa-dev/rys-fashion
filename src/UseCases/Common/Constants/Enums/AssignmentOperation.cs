using ErrorOr;

namespace UseCases.Common.Constants.Enums;

// PSEUDOCODE / PLAN:
// 1. Replace the existing wide-ranging batch operations with a compact set focused only on multi-assignment scenarios.
// 2. Provide flags for: None, Assign (add multiple assignments), Unassign (remove multiple assignments),
//    Replace (replace the entire set of assignments), Sync (synchronize assignments), Validate.
// 3. Provide an All mask that combines all multi-assignment operations.
// 4. Update the error helper to reflect the new valid operation names and to use a matching error code.
// 5. Keep the ErrorOr dependency and the namespace intact.


/// <summary>
/// Represents multi-assignment operations that can be combined using bitwise operations.
/// </summary>
[Flags]
public enum AssignmentOperation : int
{
    /// <summary>
    /// No operation.
    /// </summary>
    None = 0,

    /// <summary>
    /// Assign items (add one or more assignments).
    /// </summary>
    Add = 1 << 0,

    /// <summary>
    /// Unassign items (remove one or more assignments).
    /// </summary>
    Remove = 1 << 1,

    /// <summary>
    /// Replace assignments (replace the full set of assignments).
    /// </summary>
    Replace = 1 << 2,

    /// <summary>
    /// Sync assignments (synchronize differences between sources).
    /// </summary>
    Sync = 1 << 3,

    /// <summary>
    /// Validate assignments (check constraints without applying changes).
    /// </summary>
    Validate = 1 << 4,

    /// <summary>
    /// All supported multi-assignment operations.
    /// </summary>
    All = Add | Remove | Replace | Sync | Validate
}

/// <summary>
/// Standardized errors related to AssignmentOperation parsing/validation.
/// </summary>
public static class AssignmentOperationErrors
{
    public static Error InvalidOperation(string operation = "Unknown") =>
        Error.Validation(
            "AssignmentOperation.Invalid",
            $"The assignment operation '{operation}' is not recognized. Valid operations are: Assign, Unassign, Replace, Sync, Validate.");
}
