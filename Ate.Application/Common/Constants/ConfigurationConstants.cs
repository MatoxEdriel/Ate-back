namespace Ate.Application.Common.Constants;

public static class ConfigurationConstants
{
    public static class ConnectionStrings
    {
        public const string DefaultConnection = "DefaultConnection";
        public const string MongoDbConnection = "MongoDbConnection";
    }

    public static class MongoDbSettings
    {
        public const string SectionName = "MongoDbSettings";
        public const string ConnectionString = "ConnectionString";
        public const string DatabaseName = "DatabaseName";
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
