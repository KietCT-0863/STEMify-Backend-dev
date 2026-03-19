namespace Emulator.Repository.Configuration;

/// <summary>
/// MongoDB configuration settings
/// </summary>
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "stemify-emulator";
    public string EmulationsCollectionName { get; set; } = "emulations";
    public string TemplatesCollectionName { get; set; } = "templates";
}
