using ErrorOr;

using SharedKernel.Domain.Primitives;

namespace Core.Todos;

public sealed class Colour(string code) : ValueObject
{
    public static ErrorOr<Colour> Create(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return Color.Errors.EmptyCode;
        }
        var colour = new Colour(code);

        if (!SupportedColours.Contains(colour))
        {
            return Color.Errors.NotSupported;
        }

        return colour;
    }

    public static Colour White => new("#FFFFFF");

    public static Colour Red => new("#FF5733");

    public static Colour Orange => new("#FFC300");

    public static Colour Yellow => new("#FFFF66");

    public static Colour Green => new("#CCFF99");

    public static Colour Blue => new("#6666FF");

    public static Colour Purple => new("#9966CC");

    public static Colour Grey => new("#999999");

    public string Code { get; private set; } = string.IsNullOrWhiteSpace(code) ? "#000000" : code;

    public static implicit operator string(Colour colour)
    {
        return colour.ToString();
    }

    public static explicit operator Colour(string code)
    {
        return Create(code).Value;
    }

    public override string ToString()
    {
        return Code;
    }

    public static IEnumerable<Colour> SupportedColours
    {
        get
        {
            yield return White;
            yield return Red;
            yield return Orange;
            yield return Yellow;
            yield return Green;
            yield return Blue;
            yield return Purple;
            yield return Grey;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
    }


}
public static class Color
{
    public class Errors
    {
        public static readonly Error EmptyCode = Error.Validation(
            code: "Colour.EmptyCode",
            description: "Colour code cannot be empty.");
        public static readonly Error NotSupported = Error.Validation(
            code: "Colour.NotSupported",
            description: $"Colour is not supported.");
    }

    public static bool IsSupportedColor(this string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        // Check if the code matches any of the supported colors' Code values
        return Colour.SupportedColours.Any(colour => colour.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

}