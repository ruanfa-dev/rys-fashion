namespace SharedKernel.Domain.Attributes.Status
{
    /// <summary>
    /// Null-safe helpers for <see cref="IHasStatus{TStatus}"/>.
    /// </summary>
    public static class IHasStatusExtensions
    {
        /// <summary>
        /// Apply a new status safely.
        /// </summary>
        public static void ApplyStatus<TStatus>(this IHasStatus<TStatus>? target, TStatus newStatus)
            where TStatus : struct, Enum
        {
            if (target is null) return;
            target.Status = newStatus;
        }

        /// <summary>
        /// Returns true if the current status matches the provided one.
        /// </summary>
        public static bool IsStatus<TStatus>(this IHasStatus<TStatus>? target, TStatus status)
            where TStatus : struct, Enum =>
            target is not null && EqualityComparer<TStatus>.Default.Equals(target.Status, status);

        /// <summary>
        /// Returns true if the status is any of the provided statuses.
        /// </summary>
        public static bool IsAnyStatus<TStatus>(this IHasStatus<TStatus>? target, params TStatus[] statuses)
            where TStatus : struct, Enum =>
            target is not null && statuses.Contains(target.Status);

        /// <summary>
        /// Returns the status name (string).
        /// </summary>
        public static string? GetStatusName<TStatus>(this IHasStatus<TStatus>? target)
            where TStatus : struct, Enum =>
            target is null ? null : target.Status.ToString();
    }
}
