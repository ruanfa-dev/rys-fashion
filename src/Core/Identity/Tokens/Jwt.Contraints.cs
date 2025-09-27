namespace Core.Identity.Tokens;

public sealed partial class Jwt
{
    public static class Constraints
    {
        // JWT consists of 3 base64url parts separated by dots
        public const int TokenParts = 3;

        // Secret length recommendations (in bytes)
        public const int MinSecretBytes = 32; // 256 bits (minimum for HMAC-SHA256)
        public const int RecommendedSecretBytes = 64; // 512 bits (recommended)

        // Some reasonable maximums for header/payload/token lengths to protect parsers
        public const int MaxHeaderLength = 1024;
        public const int MaxPayloadLength = 4096;
        public const int MaxTokenLength = 8192;
    }
}
