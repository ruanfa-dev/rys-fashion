namespace SharedKernel.Domain.Attributes.Parameterizable;

public static class ParameterizableConstraints
{
    // Name constraints
    public const int NameMinLength = 1;
    public const int NameMaxLength = 100;

    // Presentation constraints
    public const int PresentationMinLength = 1;
    public const int PresentationMaxLength = 255;

    // Allowed name pattern (alphanumeric, underscore, hyphen)
    public const string NameAllowedPattern = "^[A-Za-z0-9_-]+$";
}