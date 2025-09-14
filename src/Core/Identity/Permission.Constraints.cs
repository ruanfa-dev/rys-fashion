namespace Core.Identity;

public partial class Permission
{
    #region Constants

    public static class Constraints
    {
        /// <summary>
        /// Number of segments in a valid permission name (area.resource.action).
        /// </summary>
        public const int Segments = 3;

        /// <summary>
        /// Minimum length allowed for an individual segment.
        /// </summary>
        public const int MinSegmentLength = 1;

        /// <summary>
        /// Maximum length allowed for an individual segment.
        /// </summary>
        public const int MaxSegmentLength = 64;

        /// <summary>
        /// Minimum total length for the full permission name, including separators.
        /// Computed as Segments * MinSegmentLength + (Segments - 1) separators.
        /// </summary>
        public static readonly int MinNameLength = Segments * MinSegmentLength + (Segments - 1);

        /// <summary>
        /// Maximum total length for the full permission name, including separators.
        /// Computed as Segments * MaxSegmentLength + (Segments - 1) separators.
        /// </summary>
        public static readonly int MaxNameLength = Segments * MaxSegmentLength + (Segments - 1);

        /// <summary>
        /// Maximum length for the display name.
        /// </summary>
        public const int MaxDisplayNameLength = 128;

        /// <summary>
        /// Maximum length for the description.
        /// </summary>
        public const int MaxDescriptionLength = 512;

        /// <summary>
        /// Regex pattern used to validate each segment.
        /// - Allows lowercase letters and digits.
        /// - Allows internal hyphens or underscores between groups of alphanumerics.
        /// - Disallows leading/trailing hyphens/underscores and consecutive separators.
        /// Examples allowed: "user", "invoices-2023", "product_variant1"
        /// </summary>
        public const string SegmentAllowedPattern = "^[a-z0-9]+(?:[-_][a-z0-9]+)*$";
    }
    #endregion
}
