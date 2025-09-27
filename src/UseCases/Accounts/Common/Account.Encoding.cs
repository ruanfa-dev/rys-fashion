using System.Text;

using Core.Identity;
using Core.Identity.Users;

using ErrorOr;

using Microsoft.AspNetCore.WebUtilities;

using Serilog;

namespace UseCases.Accounts.Common;
public static partial class Account
{
    public static ErrorOr<string> DecodeToken(this string code)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException ex)
        {
            Log.Error(ex, "Failed to decode token");
            return User.Errors.DecodeTokenFailed;
        }
    }
}
