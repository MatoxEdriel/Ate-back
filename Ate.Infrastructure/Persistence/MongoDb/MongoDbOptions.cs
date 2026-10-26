using Ate.Application.Common.Constants;

namespace Ate.Infrastructure.Persistence.MongoDb;

public class MongoDbOptions
{
    public const string SectionName = ConfigurationConstants.MongoDbSettings.SectionName;

    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = "ate_nosql_db";
}
