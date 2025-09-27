namespace Core.Identity.Tokens;
public sealed partial class RefreshToken
{
    public static class Constraints
    {
        // Token: Alphanumeric, uppercase/lowercase, recommended for security
        public static int TokenLength => 64;
        public static string TokenAllowedPattern => @"^[a-zA-Z0-9]{64}$"; // 64 alphanumeric chars


        // IpAddress: IPv4 or IPv6, max 45 chars
        public static int IpAddressLength => 45;
        public static string IpAddressAllowedPattern => @"^(([0-9]{1,3}\.){3}[0-9]{1,3}|([a-fA-F0-9:]+))$"; // IPv4 or IPv6
    }
}
