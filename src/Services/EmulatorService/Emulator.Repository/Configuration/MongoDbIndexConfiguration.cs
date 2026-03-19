using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Emulator.Repository.Entities;

namespace Emulator.Repository.Configuration;

/// <summary>
/// MongoDB index configuration for performance optimization
/// </summary>
public static class MongoDbIndexConfiguration
{
    public static async Task ConfigureIndexesAsync(
        IMongoDatabase database,
        ILogger logger)
    {
        logger.LogInformation("Creating MongoDB indexes for Emulator service...");

        // Configure Emulation indexes
        await ConfigureEmulationIndexesAsync(database, logger);

        // Configure Template indexes
        await ConfigureTemplateIndexesAsync(database, logger);

        logger.LogInformation("MongoDB indexes created successfully for Emulator service");
    }

    private static async Task ConfigureEmulationIndexesAsync(
        IMongoDatabase database,
        ILogger logger)
    {
        var emulationsCollection = database.GetCollection<Emulation>("emulations");

        logger.LogInformation("Creating indexes for 'emulations' collection...");

        // Index 1: EmulationId (unique)
        var emulationIdIndex = Builders<Emulation>.IndexKeys.Ascending(e => e.EmulationId);
        await emulationsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Emulation>(emulationIdIndex, new CreateIndexOptions { Unique = true })
        );

        // Index 2: Slug (unique)
        var slugIndex = Builders<Emulation>.IndexKeys.Ascending(e => e.Slug);
        await emulationsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Emulation>(slugIndex, new CreateIndexOptions { Unique = true })
        );

        // Index 3: CreatedBy + CreatedAt (for user's emulations)
        var createdByIndex = Builders<Emulation>.IndexKeys
            .Ascending(e => e.CreatedBy)
            .Descending(e => e.CreatedAt);
        await emulationsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Emulation>(createdByIndex)
        );

        // Index 4: Status + Visibility (for listing/filtering)
        var statusVisibilityIndex = Builders<Emulation>.IndexKeys
            .Ascending(e => e.Status)
            .Ascending(e => e.Visibility);
        await emulationsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Emulation>(statusVisibilityIndex)
        );

        // Index 5: Tags (multikey index for searching by tags)
        var tagsIndex = Builders<Emulation>.IndexKeys.Ascending("definition.metadata.tags");
        await emulationsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Emulation>(tagsIndex)
        );

        // Index 6: Text search on Name + Description
        var textIndex = Builders<Emulation>.IndexKeys
            .Text(e => e.Name)
            .Text(e => e.Description);
        await emulationsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Emulation>(textIndex)
        );

        // Index 7: IsDeleted (for soft delete filtering)
        var isDeletedIndex = Builders<Emulation>.IndexKeys.Ascending(e => e.IsDeleted);
        await emulationsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Emulation>(isDeletedIndex)
        );

        logger.LogInformation("Indexes for 'emulations' collection created successfully");
    }

    private static async Task ConfigureTemplateIndexesAsync(
        IMongoDatabase database,
        ILogger logger)
    {
        logger.LogInformation("Creating indexes for template collections...");

        // Component Templates Collection
        var componentTemplates = database.GetCollection<ComponentTemplate>("component_templates");

        // Index 1: TemplateId (unique)
        var componentTemplateIdIndex = Builders<ComponentTemplate>.IndexKeys.Ascending(t => t.TemplateId);
        await componentTemplates.Indexes.CreateOneAsync(
            new CreateIndexModel<ComponentTemplate>(componentTemplateIdIndex, new CreateIndexOptions { Unique = true })
        );

        // Index 2: Type + IsPublished (for filtering by type)
        var componentTypeIndex = Builders<ComponentTemplate>.IndexKeys
            .Ascending(t => t.Type)
            .Ascending(t => t.IsPublished);
        await componentTemplates.Indexes.CreateOneAsync(
            new CreateIndexModel<ComponentTemplate>(componentTypeIndex)
        );

        // Index 3: Tags (multikey index)
        var componentTagsIndex = Builders<ComponentTemplate>.IndexKeys.Ascending(t => t.Tags);
        await componentTemplates.Indexes.CreateOneAsync(
            new CreateIndexModel<ComponentTemplate>(componentTagsIndex)
        );

        // Index 4: UsageCount (for sorting by popularity)
        var componentUsageIndex = Builders<ComponentTemplate>.IndexKeys.Descending(t => t.UsageCount);
        await componentTemplates.Indexes.CreateOneAsync(
            new CreateIndexModel<ComponentTemplate>(componentUsageIndex)
        );

        // Index 5: OrganizationId (for filtering by organization)
        var componentOrgIndex = Builders<ComponentTemplate>.IndexKeys.Ascending(t => t.OrganizationId);
        await componentTemplates.Indexes.CreateOneAsync(
            new CreateIndexModel<ComponentTemplate>(componentOrgIndex)
        );

        logger.LogInformation("Indexes for 'component_templates' collection created successfully");

        // Material Templates Collection
        var materialTemplates = database.GetCollection<MaterialTemplate>("material_templates");

        // Index 1: MaterialId (unique)
        var materialIdIndex = Builders<MaterialTemplate>.IndexKeys.Ascending(t => t.MaterialId);
        await materialTemplates.Indexes.CreateOneAsync(
            new CreateIndexModel<MaterialTemplate>(materialIdIndex, new CreateIndexOptions { Unique = true })
        );

        // Index 2: IsPublished (for filtering)
        var materialPublishedIndex = Builders<MaterialTemplate>.IndexKeys.Ascending(t => t.IsPublished);
        await materialTemplates.Indexes.CreateOneAsync(
            new CreateIndexModel<MaterialTemplate>(materialPublishedIndex)
        );

        // Index 3: Tags (multikey index)
        var materialTagsIndex = Builders<MaterialTemplate>.IndexKeys.Ascending(t => t.Tags);
        await materialTemplates.Indexes.CreateOneAsync(
            new CreateIndexModel<MaterialTemplate>(materialTagsIndex)
        );

        // Index 4: UsageCount (for sorting by popularity)
        var materialUsageIndex = Builders<MaterialTemplate>.IndexKeys.Descending(t => t.UsageCount);
        await materialTemplates.Indexes.CreateOneAsync(
            new CreateIndexModel<MaterialTemplate>(materialUsageIndex)
        );

        // Index 5: OrganizationId (for filtering by organization)
        var materialOrgIndex = Builders<MaterialTemplate>.IndexKeys.Ascending(t => t.OrganizationId);
        await materialTemplates.Indexes.CreateOneAsync(
            new CreateIndexModel<MaterialTemplate>(materialOrgIndex)
        );

        logger.LogInformation("Indexes for 'material_templates' collection created successfully");
    }
}
