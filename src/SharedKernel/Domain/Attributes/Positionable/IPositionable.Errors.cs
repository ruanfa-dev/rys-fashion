using ErrorOr;

using SharedKernel.Domain.Extensions;

namespace SharedKernel.Domain.Attributes.Positionable;

public static class PositionableErrors
{
    private const string Prefix = "POSITIONING";

    public static Error PositionRequired(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.PositionRequired", GetPositionRequiredMessage(prefix));

    public static Error PositionOutOfRange(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.PositionOutOfRange", GetPositionOutOfRangeMessage(prefix));

    public static Error InvalidPosition(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.InvalidPosition", GetInvalidPositionMessage(prefix));

    private static string GetCodePrefix(string? prefix) =>
        !string.IsNullOrWhiteSpace(prefix) ? prefix! : Prefix;

    private static string GetPositionRequiredMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return "Position is required.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} position is required.";
    }

    private static string GetPositionOutOfRangeMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Position must be between {PositionableConstraints.PositionMin} and {PositionableConstraints.PositionMax}.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} position must be between {PositionableConstraints.PositionMin} and {PositionableConstraints.PositionMax}.";
    }

    private static string GetInvalidPositionMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return "Position must be a non-negative integer.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} position must be a non-negative integer.";
    }
}