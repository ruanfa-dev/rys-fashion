namespace SharedKernel.Domain.Extensions;

internal static class StringHumanize
{
    /// <summary>
    /// Convert a PascalCase or camelCase entity name to a human-friendly label.
    /// Examples: "OptionType" -> "Option type", "TodoList" -> "Todo list"
    /// Optimized implementation: minimizes allocations and copies by precomputing
    /// output size and writing directly into a char buffer.
    /// </summary>
    public static string ToEntityLabel(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name ?? string.Empty;

        int len = name.Length;
        if (len == 1)
            return name;

        // Count how many extra spaces we need to insert. We only insert a space
        // when a char is uppercase and the previous character is not uppercase.
        int extra = 0;
        for (int i = 1; i < len; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                extra++;
        }

        int outLen = len + extra;
        char[] buffer = new char[outLen];

        // Fill output buffer in a single pass
        int dest = 0;
        buffer[dest++] = name[0];

        for (int i = 1; i < len; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && !char.IsUpper(name[i - 1]))
            {
                buffer[dest++] = ' ';
            }
            buffer[dest++] = c;
        }

        // Lowercase the first character of the second word if present
        for (int i = 0; i < outLen - 1; i++)
        {
            if (buffer[i] == ' ')
            {
                buffer[i + 1] = char.ToLowerInvariant(buffer[i + 1]);
                break;
            }
        }

        return new string(buffer);
    }
}
