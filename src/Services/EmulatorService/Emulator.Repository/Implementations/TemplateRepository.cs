using Emulator.Repository.Configuration;
using Emulator.Repository.Entities;
using Emulator.Repository.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Emulator.Repository.Implementations;

/// <summary>
/// MongoDB implementation of template repository
/// </summary>
public class TemplateRepository : ITemplateRepository
{
    private readonly IMongoCollection<ComponentTemplate> _componentTemplates;
    private readonly IMongoCollection<MaterialTemplate> _materialTemplates;

    public TemplateRepository(IMongoClient mongoClient, IOptions<MongoDbSettings> settings)
    {
        var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
        _componentTemplates = database.GetCollection<ComponentTemplate>("component_templates");
        _materialTemplates = database.GetCollection<MaterialTemplate>("material_templates");
    }

    // ============================================
    // Component Template Operations
    // ============================================

    public async Task<ComponentTemplate> CreateComponentTemplateAsync(ComponentTemplate template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        await _componentTemplates.InsertOneAsync(template);
        return template;
    }

    public async Task<(ComponentTemplate template, bool wasCreated)> UpsertComponentTemplateAsync(ComponentTemplate template)
    {
        // Check if template exists to preserve CreatedAt and _id
        var existing = await GetComponentTemplateByIdAsync(template.TemplateId);

        if (existing != null)
        {
            template.Id = existing.Id;  
            template.CreatedAt = existing.CreatedAt;
            template.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            template.Id = null;
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
        }

        var filter = Builders<ComponentTemplate>.Filter.Eq(t => t.TemplateId, template.TemplateId);
        var options = new ReplaceOptions { IsUpsert = true };

        await _componentTemplates.ReplaceOneAsync(filter, template, options);

        return (template, existing == null);
    }

    public async Task<ComponentTemplate?> GetComponentTemplateByIdAsync(string templateId)
    {
        var filter = Builders<ComponentTemplate>.Filter.Eq(t => t.TemplateId, templateId);
        return await _componentTemplates.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<ComponentTemplate>> GetComponentTemplatesByIdsAsync(List<string> templateIds)
    {
        if (templateIds == null || templateIds.Count == 0)
            return new List<ComponentTemplate>();

        var filter = Builders<ComponentTemplate>.Filter.In(t => t.TemplateId, templateIds);
        return await _componentTemplates.Find(filter).ToListAsync();
    }

    public async Task<List<ComponentTemplate>> GetPublishedComponentTemplatesAsync(
        string? type = null,
        List<string>? tags = null,
        int skip = 0,
        int limit = 100)
    {
        var filterBuilder = Builders<ComponentTemplate>.Filter;
        var filters = new List<FilterDefinition<ComponentTemplate>>
        {
            filterBuilder.Eq(t => t.IsPublished, true)
        };

        if (!string.IsNullOrEmpty(type))
        {
            filters.Add(filterBuilder.Eq(t => t.Type, type));
        }

        if (tags != null && tags.Count > 0)
        {
            filters.Add(filterBuilder.AnyIn(t => t.Tags, tags));
        }

        var combinedFilter = filterBuilder.And(filters);

        return await _componentTemplates
            .Find(combinedFilter)
            .Sort(Builders<ComponentTemplate>.Sort.Descending(t => t.UsageCount))
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<bool> UpdateComponentTemplateAsync(ComponentTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<ComponentTemplate>.Filter.Eq(t => t.TemplateId, template.TemplateId);
        var result = await _componentTemplates.ReplaceOneAsync(filter, template);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteComponentTemplateAsync(string templateId)
    {
        var filter = Builders<ComponentTemplate>.Filter.Eq(t => t.TemplateId, templateId);
        var result = await _componentTemplates.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    public async Task<bool> ComponentTemplateExistsAsync(string templateId)
    {
        var filter = Builders<ComponentTemplate>.Filter.Eq(t => t.TemplateId, templateId);
        var count = await _componentTemplates.CountDocumentsAsync(filter);
        return count > 0;
    }

    public async Task IncrementComponentTemplateUsageAsync(string templateId)
    {
        var filter = Builders<ComponentTemplate>.Filter.Eq(t => t.TemplateId, templateId);
        var update = Builders<ComponentTemplate>.Update.Inc(t => t.UsageCount, 1);
        await _componentTemplates.UpdateOneAsync(filter, update);
    }

    // ============================================
    // Material Template Operations
    // ============================================

    public async Task<MaterialTemplate> CreateMaterialTemplateAsync(MaterialTemplate template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        await _materialTemplates.InsertOneAsync(template);
        return template;
    }

    public async Task<(MaterialTemplate template, bool wasCreated)> UpsertMaterialTemplateAsync(MaterialTemplate template)
    {
        // Check if template exists to preserve CreatedAt and _id
        var existing = await GetMaterialTemplateByIdAsync(template.MaterialId);

        if (existing != null)
        {
            // Update: preserve _id and CreatedAt, update UpdatedAt
            template.Id = existing.Id;  // CRITICAL: Preserve MongoDB _id to avoid "immutable field '_id' was altered" error!
            template.CreatedAt = existing.CreatedAt;
            template.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Insert: MongoDB will auto-generate _id, set both timestamps
            template.Id = null;  // Let MongoDB generate _id for new documents
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
        }

        var filter = Builders<MaterialTemplate>.Filter.Eq(t => t.MaterialId, template.MaterialId);
        var options = new ReplaceOptions { IsUpsert = true };

        await _materialTemplates.ReplaceOneAsync(filter, template, options);

        return (template, existing == null);
    }

    public async Task<MaterialTemplate?> GetMaterialTemplateByIdAsync(string materialId)
    {
        var filter = Builders<MaterialTemplate>.Filter.Eq(t => t.MaterialId, materialId);
        return await _materialTemplates.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<MaterialTemplate>> GetMaterialTemplatesByIdsAsync(List<string> materialIds)
    {
        if (materialIds == null || materialIds.Count == 0)
            return new List<MaterialTemplate>();

        var filter = Builders<MaterialTemplate>.Filter.In(t => t.MaterialId, materialIds);
        return await _materialTemplates.Find(filter).ToListAsync();
    }

    public async Task<List<MaterialTemplate>> GetPublishedMaterialTemplatesAsync(
        List<string>? tags = null,
        int skip = 0,
        int limit = 100)
    {
        var filterBuilder = Builders<MaterialTemplate>.Filter;
        var filters = new List<FilterDefinition<MaterialTemplate>>
        {
            filterBuilder.Eq(t => t.IsPublished, true)
        };

        if (tags != null && tags.Count > 0)
        {
            filters.Add(filterBuilder.AnyIn(t => t.Tags, tags));
        }

        var combinedFilter = filterBuilder.And(filters);

        return await _materialTemplates
            .Find(combinedFilter)
            .Sort(Builders<MaterialTemplate>.Sort.Descending(t => t.UsageCount))
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<bool> UpdateMaterialTemplateAsync(MaterialTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<MaterialTemplate>.Filter.Eq(t => t.MaterialId, template.MaterialId);
        var result = await _materialTemplates.ReplaceOneAsync(filter, template);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteMaterialTemplateAsync(string materialId)
    {
        var filter = Builders<MaterialTemplate>.Filter.Eq(t => t.MaterialId, materialId);
        var result = await _materialTemplates.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    public async Task<bool> MaterialTemplateExistsAsync(string materialId)
    {
        var filter = Builders<MaterialTemplate>.Filter.Eq(t => t.MaterialId, materialId);
        var count = await _materialTemplates.CountDocumentsAsync(filter);
        return count > 0;
    }

    public async Task IncrementMaterialTemplateUsageAsync(string materialId)
    {
        var filter = Builders<MaterialTemplate>.Filter.Eq(t => t.MaterialId, materialId);
        var update = Builders<MaterialTemplate>.Update.Inc(t => t.UsageCount, 1);
        await _materialTemplates.UpdateOneAsync(filter, update);
    }

    // ============================================
    // Batch Operations
    // ============================================

    public async Task<(bool allExist, List<string> missingTemplateIds)> ValidateTemplateReferencesAsync(
        List<string> componentTemplateIds,
        List<string> materialTemplateIds)
    {
        var missingIds = new List<string>();

        // Check component templates
        if (componentTemplateIds != null && componentTemplateIds.Count > 0)
        {
            var componentFilter = Builders<ComponentTemplate>.Filter.In(t => t.TemplateId, componentTemplateIds);
            var existingComponents = await _componentTemplates
                .Find(componentFilter)
                .Project(t => t.TemplateId)
                .ToListAsync();

            var missingComponents = componentTemplateIds.Except(existingComponents).ToList();
            missingIds.AddRange(missingComponents);
        }

        // Check material templates
        if (materialTemplateIds != null && materialTemplateIds.Count > 0)
        {
            var materialFilter = Builders<MaterialTemplate>.Filter.In(t => t.MaterialId, materialTemplateIds);
            var existingMaterials = await _materialTemplates
                .Find(materialFilter)
                .Project(t => t.MaterialId)
                .ToListAsync();

            var missingMaterials = materialTemplateIds.Except(existingMaterials).ToList();
            missingIds.AddRange(missingMaterials);
        }

        return (missingIds.Count == 0, missingIds);
    }
}
