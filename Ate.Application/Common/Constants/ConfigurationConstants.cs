namespace Ate.Application.Common.Constants;

public static class ConfigurationConstants
{
    public static class ConnectionStrings
    {
        public const string DefaultConnection = "DefaultConnection";
    }

    public static class JwtSettings
    {
        public const string SectionName = "JwtSettings";
        public const string SecretKey = "SecretKey";
        public const string Issuer = "Issuer";
        public const string Audience = "Audience";
        public const string ExpirationInMinutes = "ExpirationInMinutes";
    }
}
