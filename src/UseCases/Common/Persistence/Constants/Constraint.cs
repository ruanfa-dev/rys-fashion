namespace UseCases.Common.Persistence.Constants;

public static class Constraints
{
    public const int CreatedByMaxLength = 100;
    public const int UpdatedByMaxLength = 100;
    public const int DefaultMinLength = 1;
    public const int MaxCodeLength = 50;
    public const int MaxDescriptionLength = 500;
    public const int DefaultMaxLength = 1000;
    public const int EmailMinLength = 5;
    public const int EmailMaxLength = 100;
    public const int UrlMinLength = 10;
    public const int UrlMaxLength = 2048;
    public const int PhoneMinLength = 10;
    public const int PhoneMaxLength = 15;
    public const int DateLength = 10;
    public const int BooleanMinLength = 4;
    public const int BooleanMaxLength = 5;
    public const int ColorMinLength = 4;
    public const int ColorMaxLength = 7;
    public const int ToggleMinLength = 4;
    public const int ToggleMaxLength = 5;
    public const int CustomTextMaxLength = 200;
    public const int FilenameMaxLength = 255;
    public const int MultiSelectMinSelections = 0;
    public const int MultiSelectMaxSelections = 10;
    public const decimal DefaultMinValue = 0;
    public const decimal DefaultMaxValue = 1000000;
    public const decimal DecimalMaxValue = 999999.99m;
    public const decimal OptionNumberMaxValue = 1000;
    public const decimal FileMaxSizeBytes = 10485760; // 10MB
    public const decimal MultipleOfInteger = 1;
    public const decimal DecimalMultipleOf = 0.01m;
    public const decimal OptionNumberMultipleOf = 1;

    public static readonly IReadOnlyList<string> NoZipCodeIsoCodes = new List<string>
    {
        "AO", "AG", "AW", "BS", "BZ", "BJ", "BM", "BO", "BW", "BF", "BI", "CM", "CF", "KM", "CG",
        "CD", "CK", "CUW", "CI", "DJ", "DM", "GQ", "ER", "FJ", "TF", "GAB", "GM", "GH", "GD", "GN",
        "GY", "HK", "IE", "KI", "KP", "LY", "MO", "MW", "ML", "MR", "NR", "AN", "NU", "KP", "PA",
        "QA", "RW", "KN", "LC", "ST", "SC", "SL", "SB", "SO", "SR", "SY", "TZ", "TL", "TK", "TG",
        "TO", "TV", "UG", "AE", "VU", "YE", "ZW"
    }.AsReadOnly();

    public static readonly IReadOnlyList<string> StatesRequiredIsoCodes = new List<string>
    {
        "AU", "AE", "BR", "CA", "CN", "ES", "HK", "IE", "IN",
        "IT", "MY", "MX", "NZ", "PT", "RO", "TH", "US", "ZA"
    }.AsReadOnly();
}