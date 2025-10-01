using ErrorOr;

using SharedKernel.Domain.Extensions;

namespace SharedKernel.Domain.Attributes.Status;

public static class StatusErrors
{
    private const string Prefix = "STATUS";

    public static Error StatusRequired(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.StatusRequired", GetStatusRequiredMessage(prefix));

    public static Error InvalidStatus(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.InvalidStatus", GetInvalidStatusMessage(prefix));

    public static Error UnsupportedStatus(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.UnsupportedStatus", GetUnsupportedStatusMessage(prefix));

    private static string GetCodePrefix(string? prefix) =>
        !string.IsNullOrWhiteSpace(prefix) ? prefix! : Prefix;

    private static string GetStatusRequiredMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return "Status is required.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} status is required.";
    }

    private static string GetInvalidStatusMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return "Status value is invalid.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} status value is invalid.";
    }

    private static string GetUnsupportedStatusMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return "Status value is not supported.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} status value is not supported.";
    }
}