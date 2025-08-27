namespace SharedKernel.Domain.Extensions;
public static class EnumDescriptionExtensions
{
    private static List<string> GetEnumDescriptions<TEnum>() where TEnum : Enum
    {
        List<string> descriptions = [];

        foreach (TEnum status in Enum.GetValues(typeof(TEnum)))
        {
            string description = $"{status}({Convert.ToInt32(status)})";
            descriptions.Add(description);
        }

        return descriptions;
    }

    public static string GetEnumDescription<TEnum>(this TEnum value) where TEnum : Enum
    {
        return $"{value}({Convert.ToInt32(value)})";
    }

    public static string GetEnumContextDescription<TEnum>() where TEnum : Enum
    {
        return string.Join(", ", GetEnumDescriptions<TEnum>());
    }
}
