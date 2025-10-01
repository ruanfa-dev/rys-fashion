namespace SharedKernel.Domain.Attributes.Seo;

public static class SlugSupportConstraints
{
    public const int SlugMinLength = 1;
    public const int SlugMaxLength = 200;
    public const string SlugAllowedPattern = "^[a-z0-9\\-]+$"; // lowercase, numbers, hyphens
}