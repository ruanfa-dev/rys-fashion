using ErrorOr;

using Microsoft.AspNetCore.Identity;

namespace UseCases.Accounts.Common;
public static partial class Account
{
    public static List<Error> ToApplicationResult(
        this IEnumerable<IdentityError> errors,
        string prefix = "Auth",
        string fallbackCode = "UnknownError",
        ErrorType errorType = ErrorType.Validation)
    {
        return [.. errors.Select(error => Error.Custom(
            type: (int)errorType,
            code: !string.IsNullOrWhiteSpace(error.Code) ? $"{prefix}.{error.Code}" : $"{prefix}.{fallbackCode}",
            description: error.Description))];
    }
}
