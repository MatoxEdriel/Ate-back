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

    public static class SmtpSettings
    {
        public const string SectionName = "SmtpSettings";
        public const string Host = "Host";
        public const string Port = "Port";
        public const string UserName = "UserName";
        public const string Password = "Password";
        public const string SenderEmail = "SenderEmail";
        public const string SenderName = "SenderName";
        public const string EnableSsl = "EnableSsl";
        public const string SupportEmail = "SupportEmail";
        public const string DefaultCcEmail = "DefaultCcEmail";
    }
}
