namespace Core.Identity;
public sealed partial class RefreshToken
{
    public static class Constraints
    {
        // Token: Alphanumeric, uppercase/lowercase, recommended for security
        public const int TokenLength = 64;
        public const string TokenAllowedPattern = @"^[a-zA-Z0-9]{64}$"; // 64 alphanumeric chars
        public const int JwtTokenLength = 2048;
        public const string JwtTokenAllowedPattern = @"^[A-Za-z0-9-_\.=]+$"; // JWT format, base64url chars

        // UserId: Guid format, 36 characters
        public const int UserIdLength = 36;
        public const string UserIdAllowedPattern = @"^[a-fA-F0-9\-]{36}$"; // Guid

        // IpAddress: IPv4 or IPv6, max 45 chars
        public const int IpAddressLength = 45;
        public const string IpAddressAllowedPattern = @"^(([0-9]{1,3}\.){3}[0-9]{1,3}|([a-fA-F0-9:]+))$"; // IPv4 or IPv6

        // UserAgent: Allow most printable characters, restrict length
        public const int UserAgentLength = 512;
        public const string UserAgentAllowedPattern = @"^[\u0020-\u007E]{1,512}$"; // Printable ASCII
    }
}
