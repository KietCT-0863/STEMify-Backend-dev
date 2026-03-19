using Emulator.Repository.Entities;
using Emulator.Repository.Interfaces;
using Emulator.Service.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Caching.Cache;

namespace Emulator.Service.Services;

/// <summary>
/// Service for managing component and material templates with caching
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _templateRepository;
    private readonly ILogger<TemplateService> _logger;
    private readonly ICacheRedis _cache;

    // Cache TTL: 24 hours for templates (as per architecture-recommendations.md Phase 2)
    private static readonly TimeSpan TemplateCacheTTL = TimeSpan.FromHours(24);

    public TemplateService(
        ITemplateRepository templateRepository,
        ILogger<TemplateService> logger,
        ICacheRedis cache)
    {
        _templateRepository = templateRepository;
        _logger = logger;
        _cache = cache;
    }

    // ============================================
    // Component Template Operations
    // ============================================

    public async Task<ComponentTemplate> CreateComponentTemplateAsync(
        string templateId,
        string type,
        string version,
        Dictionary<string, object> definition,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        string? createdBy = null,
        string? organizationId = null)
    {
        _logger.LogInformation("Creating component template: {TemplateId}", templateId);

        // Check if template already exists
        var exists = await _templateRepository.ComponentTemplateExistsAsync(templateId);
        if (exists)
        {
            throw new InvalidOperationException($"Component template with ID '{templateId}' already exists");
        }

        var template = new ComponentTemplate
        {
            TemplateId = templateId,
            Type = type,
            Version = version,
            Name = name ?? templateId,
            Description = description,
            Definition = definition,
            Tags = tags,
            CreatedBy = createdBy,
            OrganizationId = organizationId,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _templateRepository.CreateComponentTemplateAsync(template);

        // Cache the template in Redis (24h TTL)
        var cacheKey = $"{CacheKeys.Key_Component_Template_By_Id}:{templateId}";
        await _cache.SetAsync(cacheKey, created, TemplateCacheTTL);
        _logger.LogDebug("Cached component template: {CacheKey}", cacheKey);

        _logger.LogInformation("Component template created: {TemplateId}", templateId);
        return created;
    }

    /// <summary>
    /// Upsert component template (insert if not exists, update if exists)
    /// Frontend can call this multiple times with same template ID without error
    /// </summary>
    public async Task<(ComponentTemplate template, bool wasCreated)> UpsertComponentTemplateAsync(
        string templateId,
        string type,
        string version,
        Dictionary<string, object> definition,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        string? createdBy = null,
        string? organizationId = null)
    {
        _logger.LogInformation("Upserting component template: {TemplateId}", templateId);

        var template = new ComponentTemplate
        {
            TemplateId = templateId,
            Type = type,
            Version = version,
            Name = name ?? templateId,
            Description = description,
            Definition = definition,
            Tags = tags,
            CreatedBy = createdBy,
            OrganizationId = organizationId,
            IsPublished = true
        };

        // Upsert in repository (handles CreatedAt/UpdatedAt timestamps)
        var (upserted, wasCreated) = await _templateRepository.UpsertComponentTemplateAsync(template);

        // Update cache
        var cacheKey = $"{CacheKeys.Key_Component_Template_By_Id}:{templateId}";
        await _cache.SetAsync(cacheKey, upserted, TemplateCacheTTL);

        var action = wasCreated ? "created" : "updated";
        _logger.LogInformation("Component template {Action}: {TemplateId}", action, templateId);

        return (upserted, wasCreated);
    }

    public async Task<ComponentTemplate?> GetComponentTemplateAsync(string templateId, bool useCache = true)
    {
        _logger.LogDebug("Getting component template: {TemplateId}", templateId);

        
        var cacheKey = $"{CacheKeys.Key_Component_Template_By_Id}:{templateId}";
        if (useCache)
        {
            var cached = await _cache.GetAsync<ComponentTemplate>(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for component template: {TemplateId}", templateId);
                return cached;
            }
            _logger.LogDebug("Cache miss for component template: {TemplateId}", templateId);
        }

        // Fetch from database
        var template = await _templateRepository.GetComponentTemplateByIdAsync(templateId);

        // Cache the result if found
        if (template != null && useCache)
        {
            await _cache.SetAsync(cacheKey, template, TemplateCacheTTL);
            _logger.LogDebug("Cached component template from DB: {TemplateId}", templateId);
        }

        return template;
    }

    public async Task<List<ComponentTemplate>> GetComponentTemplatesAsync(List<string> templateIds, bool useCache = true)
    {
        if (templateIds == null || templateIds.Count == 0)
            return new List<ComponentTemplate>();

        _logger.LogDebug("Getting {Count} component templates with batch caching", templateIds.Count);

        if (!useCache)
            return await _templateRepository.GetComponentTemplatesByIdsAsync(templateIds);

        // Step 1: Batch GET from cache (parallel)
        var cacheResults = await Task.WhenAll(
            templateIds.Select(async (id, index) =>
            {
                var cacheKey = $"{CacheKeys.Key_Component_Template_By_Id}:{id}";
                var cached = await _cache.GetAsync<ComponentTemplate>(cacheKey);
                return new { Index = index, Id = id, Template = cached };
            })
        );

        // Step 2: Separate cached vs missing
        var cached = cacheResults
            .Where(r => r.Template != null)
            .Select(r => r.Template!)
            .ToList();

        var missingIds = cacheResults
            .Where(r => r.Template == null)
            .Select(r => r.Id)
            .ToList();

        _logger.LogDebug(
            "Component batch cache: {Hits} hits, {Misses} misses ({HitRate:F1}%)",
            cached.Count,
            missingIds.Count,
            cached.Count > 0 ? (cached.Count * 100.0 / templateIds.Count) : 0
        );

        // Step 3: Fetch missing from DB (single query)
        if (missingIds.Count > 0)
        {
            var missing = await _templateRepository.GetComponentTemplatesByIdsAsync(missingIds);

            // Step 4: Batch SET missing to cache (parallel)
            await Task.WhenAll(
                missing.Select(template =>
                {
                    var cacheKey = $"{CacheKeys.Key_Component_Template_By_Id}:{template.TemplateId}";
                    return _cache.SetAsync(cacheKey, template, TemplateCacheTTL);
                })
            );

            _logger.LogDebug("Cached {Count} missing component templates", missing.Count);

            // Step 5: Merge & return
            return cached.Concat(missing).ToList();
        }

        return cached;
    }

    public async Task<List<ComponentTemplate>> ListComponentTemplatesAsync(
        string? type = null,
        List<string>? tags = null,
        int skip = 0,
        int limit = 100)
    {
        _logger.LogDebug("Listing component templates: type={Type}, skip={Skip}, limit={Limit}", type, skip, limit);

        return await _templateRepository.GetPublishedComponentTemplatesAsync(type, tags, skip, limit);
    }

    public async Task<bool> UpdateComponentTemplateAsync(
        string templateId,
        Dictionary<string, object>? definition = null,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        bool? isPublished = null)
    {
        _logger.LogInformation("Updating component template: {TemplateId}", templateId);

        var template = await _templateRepository.GetComponentTemplateByIdAsync(templateId);
        if (template == null)
        {
            _logger.LogWarning("Component template not found: {TemplateId}", templateId);
            return false;
        }

        // Update fields if provided
        if (definition != null) template.Definition = definition;
        if (name != null) template.Name = name;
        if (description != null) template.Description = description;
        if (tags != null) template.Tags = tags;
        if (isPublished.HasValue) template.IsPublished = isPublished.Value;

        template.UpdatedAt = DateTime.UtcNow;

        var updated = await _templateRepository.UpdateComponentTemplateAsync(template);

        if (updated)
        {
            var cacheKey = $"{CacheKeys.Key_Component_Template_By_Id}:{templateId}";
            await _cache.ClearAsync(cacheKey);
            _logger.LogDebug("Invalidated cache for component template: {TemplateId}", templateId);
        }

        _logger.LogInformation("Component template updated: {TemplateId}, success={Success}", templateId, updated);
        return updated;
    }

    public async Task<bool> DeleteComponentTemplateAsync(string templateId)
    {
        _logger.LogInformation("Deleting component template: {TemplateId}", templateId);

        var deleted = await _templateRepository.DeleteComponentTemplateAsync(templateId);

        if (deleted)
        {
            var cacheKey = $"{CacheKeys.Key_Component_Template_By_Id}:{templateId}";
            await _cache.ClearAsync(cacheKey);
            _logger.LogDebug("Invalidated cache for deleted component template: {TemplateId}", templateId);
        }

        _logger.LogInformation("Component template deleted: {TemplateId}, success={Success}", templateId, deleted);
        return deleted;
    }

    // ============================================
    // Material Template Operations
    // ============================================

    public async Task<MaterialTemplate> CreateMaterialTemplateAsync(
        string materialId,
        string version,
        Dictionary<string, object> definition,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        string? createdBy = null,
        string? organizationId = null)
    {
        _logger.LogInformation("Creating material template: {MaterialId}", materialId);

        // Check if template already exists
        var exists = await _templateRepository.MaterialTemplateExistsAsync(materialId);
        if (exists)
        {
            throw new InvalidOperationException($"Material template with ID '{materialId}' already exists");
        }

        var template = new MaterialTemplate
        {
            MaterialId = materialId,
            Version = version,
            Name = name ?? materialId,
            Description = description,
            Definition = definition,
            Tags = tags,
            CreatedBy = createdBy,
            OrganizationId = organizationId,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _templateRepository.CreateMaterialTemplateAsync(template);

        // Cache the template in Redis (Phase 2: 24h TTL)
        var cacheKey = $"{CacheKeys.Key_Material_Template_By_Id}:{materialId}";
        await _cache.SetAsync(cacheKey, created, TemplateCacheTTL);
        _logger.LogDebug("Cached material template: {CacheKey}", cacheKey);

        _logger.LogInformation("Material template created: {MaterialId}", materialId);
        return created;
    }

    /// <summary>
    /// Upsert material template (insert if not exists, update if exists)
    /// Frontend can call this multiple times with same template ID without error
    /// </summary>
    public async Task<(MaterialTemplate template, bool wasCreated)> UpsertMaterialTemplateAsync(
        string materialId,
        string version,
        Dictionary<string, object> definition,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        string? createdBy = null,
        string? organizationId = null)
    {
        _logger.LogInformation("Upserting material template: {MaterialId}", materialId);

        var template = new MaterialTemplate
        {
            MaterialId = materialId,
            Version = version,
            Name = name ?? materialId,
            Description = description,
            Definition = definition,
            Tags = tags,
            CreatedBy = createdBy,
            OrganizationId = organizationId,
            IsPublished = true
        };

        // Upsert in repository (handles CreatedAt/UpdatedAt timestamps)
        var (upserted, wasCreated) = await _templateRepository.UpsertMaterialTemplateAsync(template);

        // Update cache
        var cacheKey = $"{CacheKeys.Key_Material_Template_By_Id}:{materialId}";
        await _cache.SetAsync(cacheKey, upserted, TemplateCacheTTL);

        var action = wasCreated ? "created" : "updated";
        _logger.LogInformation("Material template {Action}: {MaterialId}", action, materialId);

        return (upserted, wasCreated);
    }

    public async Task<MaterialTemplate?> GetMaterialTemplateAsync(string materialId, bool useCache = true)
    {
        _logger.LogDebug("Getting material template: {MaterialId}", materialId);

        var cacheKey = $"{CacheKeys.Key_Material_Template_By_Id}:{materialId}";
        if (useCache)
        {
            var cached = await _cache.GetAsync<MaterialTemplate>(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for material template: {MaterialId}", materialId);
                return cached;
            }
            _logger.LogDebug("Cache miss for material template: {MaterialId}", materialId);
        }

        // Fetch from database
        var template = await _templateRepository.GetMaterialTemplateByIdAsync(materialId);

        // Cache the result if found
        if (template != null && useCache)
        {
            await _cache.SetAsync(cacheKey, template, TemplateCacheTTL);
            _logger.LogDebug("Cached material template from DB: {MaterialId}", materialId);
        }

        return template;
    }

    public async Task<List<MaterialTemplate>> GetMaterialTemplatesAsync(List<string> materialIds, bool useCache = true)
    {
        if (materialIds == null || materialIds.Count == 0)
            return new List<MaterialTemplate>();

        _logger.LogDebug("Getting {Count} material templates with batch caching", materialIds.Count);

        if (!useCache)
            return await _templateRepository.GetMaterialTemplatesByIdsAsync(materialIds);

        // Step 1: Batch GET from cache (parallel)
        var cacheResults = await Task.WhenAll(
            materialIds.Select(async (id, index) =>
            {
                var cacheKey = $"{CacheKeys.Key_Material_Template_By_Id}:{id}";
                var cached = await _cache.GetAsync<MaterialTemplate>(cacheKey);
                return new { Index = index, Id = id, Template = cached };
            })
        );

        // Step 2: Separate cached vs missing
        var cached = cacheResults
            .Where(r => r.Template != null)
            .Select(r => r.Template!)
            .ToList();

        var missingIds = cacheResults
            .Where(r => r.Template == null)
            .Select(r => r.Id)
            .ToList();

        _logger.LogDebug(
            "Material batch cache: {Hits} hits, {Misses} misses ({HitRate:F1}%)",
            cached.Count,
            missingIds.Count,
            cached.Count > 0 ? (cached.Count * 100.0 / materialIds.Count) : 0
        );

        // Step 3: Fetch missing from DB (single query)
        if (missingIds.Count > 0)
        {
            var missing = await _templateRepository.GetMaterialTemplatesByIdsAsync(missingIds);

            // Step 4: Batch SET missing to cache (parallel)
            await Task.WhenAll(
                missing.Select(template =>
                {
                    var cacheKey = $"{CacheKeys.Key_Material_Template_By_Id}:{template.MaterialId}";
                    return _cache.SetAsync(cacheKey, template, TemplateCacheTTL);
                })
            );

            _logger.LogDebug("Cached {Count} missing material templates", missing.Count);

            // Step 5: Merge & return
            return cached.Concat(missing).ToList();
        }

        return cached;
    }

    public async Task<List<MaterialTemplate>> ListMaterialTemplatesAsync(
        List<string>? tags = null,
        int skip = 0,
        int limit = 100)
    {
        _logger.LogDebug("Listing material templates: skip={Skip}, limit={Limit}", skip, limit);

        return await _templateRepository.GetPublishedMaterialTemplatesAsync(tags, skip, limit);
    }

    public async Task<bool> UpdateMaterialTemplateAsync(
        string materialId,
        Dictionary<string, object>? definition = null,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        bool? isPublished = null)
    {
        _logger.LogInformation("Updating material template: {MaterialId}", materialId);

        var template = await _templateRepository.GetMaterialTemplateByIdAsync(materialId);
        if (template == null)
        {
            _logger.LogWarning("Material template not found: {MaterialId}", materialId);
            return false;
        }

        // Update fields if provided
        if (definition != null) template.Definition = definition;
        if (name != null) template.Name = name;
        if (description != null) template.Description = description;
        if (tags != null) template.Tags = tags;
        if (isPublished.HasValue) template.IsPublished = isPublished.Value;

        template.UpdatedAt = DateTime.UtcNow;

        var updated = await _templateRepository.UpdateMaterialTemplateAsync(template);

        if (updated)
        {
            var cacheKey = $"{CacheKeys.Key_Material_Template_By_Id}:{materialId}";
            await _cache.ClearAsync(cacheKey);
            _logger.LogDebug("Invalidated cache for material template: {MaterialId}", materialId);
        }

        _logger.LogInformation("Material template updated: {MaterialId}, success={Success}", materialId, updated);
        return updated;
    }

    public async Task<bool> DeleteMaterialTemplateAsync(string materialId)
    {
        _logger.LogInformation("Deleting material template: {MaterialId}", materialId);

        var deleted = await _templateRepository.DeleteMaterialTemplateAsync(materialId);

        if (deleted)
        {
            var cacheKey = $"{CacheKeys.Key_Material_Template_By_Id}:{materialId}";
            await _cache.ClearAsync(cacheKey);
            _logger.LogDebug("Invalidated cache for deleted material template: {MaterialId}", materialId);
        }

        _logger.LogInformation("Material template deleted: {MaterialId}, success={Success}", materialId, deleted);
        return deleted;
    }

    // ============================================
    // Validation & Utilities
    // ============================================

    public async Task<(bool isValid, List<string> missingTemplates)> ValidateTemplateReferencesAsync(
        List<string> componentTemplateIds,
        List<string> materialTemplateIds)
    {
        _logger.LogDebug("Validating template references: {ComponentCount} components, {MaterialCount} materials",
            componentTemplateIds?.Count ?? 0, materialTemplateIds?.Count ?? 0);

        var (allExist, missingIds) = await _templateRepository.ValidateTemplateReferencesAsync(
            componentTemplateIds ?? new List<string>(),
            materialTemplateIds ?? new List<string>());

        if (!allExist)
        {
            _logger.LogWarning("Missing template references: {MissingIds}", string.Join(", ", missingIds));
        }

        return (!allExist ? false : true, missingIds);
    }

    public async Task IncrementTemplateUsageAsync(List<string> componentTemplateIds, List<string> materialTemplateIds)
    {
        _logger.LogDebug("Incrementing template usage counts");

        // Increment component template usage
        if (componentTemplateIds != null)
        {
            foreach (var templateId in componentTemplateIds)
            {
                await _templateRepository.IncrementComponentTemplateUsageAsync(templateId);
            }
        }

        // Increment material template usage
        if (materialTemplateIds != null)
        {
            foreach (var materialId in materialTemplateIds)
            {
                await _templateRepository.IncrementMaterialTemplateUsageAsync(materialId);
            }
        }
    }

    public async Task<(int componentCount, int materialCount)> ImportTemplatesFromOctahedronAsync(
        Dictionary<string, object> octahedronData,
        string? createdBy = null)
    {
        _logger.LogInformation("Importing templates from octahedron data");

        int componentCount = 0;
        int materialCount = 0;

        try
        {
            // Extract templates section
            if (octahedronData.TryGetValue("templates", out var templatesObj) && templatesObj is JsonElement templatesElement)
            {
                // Import material templates
                if (templatesElement.TryGetProperty("materials", out var materials))
                {
                    foreach (var material in materials.EnumerateArray())
                    {
                        var materialId = material.GetProperty("id").GetString();
                        if (string.IsNullOrEmpty(materialId)) continue;

                        // Check if already exists
                        var exists = await _templateRepository.MaterialTemplateExistsAsync(materialId);
                        if (exists)
                        {
                            _logger.LogDebug("Material template already exists, skipping: {MaterialId}", materialId);
                            continue;
                        }

                        var definition = new Dictionary<string, object>
                        {
                            ["source"] = material.GetProperty("source").GetString() ?? ""
                        };

                        await CreateMaterialTemplateAsync(
                            materialId: materialId,
                            version: "1.0",
                            definition: definition,
                            name: materialId,
                            tags: new List<string> { "imported", "octahedron" },
                            createdBy: createdBy);

                        materialCount++;
                    }
                }

                // Import component templates
                if (templatesElement.TryGetProperty("components", out var components))
                {
                    foreach (var component in components.EnumerateArray())
                    {
                        var componentId = component.GetProperty("id").GetString();
                        if (string.IsNullOrEmpty(componentId)) continue;

                        // Check if already exists
                        var exists = await _templateRepository.ComponentTemplateExistsAsync(componentId);
                        if (exists)
                        {
                            _logger.LogDebug("Component template already exists, skipping: {ComponentId}", componentId);
                            continue;
                        }

                        var source = component.GetProperty("source").GetString() ?? "";
                        var type = source.Contains("Straw") ? "straw" : "connector";

                        var definition = new Dictionary<string, object>
                        {
                            ["source"] = source
                        };

                        await CreateComponentTemplateAsync(
                            templateId: componentId,
                            type: type,
                            version: "1.0",
                            definition: definition,
                            name: componentId,
                            tags: new List<string> { "imported", "octahedron", type },
                            createdBy: createdBy);

                        componentCount++;
                    }
                }
            }

            _logger.LogInformation("Templates imported: {ComponentCount} components, {MaterialCount} materials",
                componentCount, materialCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing templates from octahedron data");
            throw;
        }

        return (componentCount, materialCount);
    }

    /// <summary>
    /// Preload hot templates to cache on startup
    /// </summary>
    public async Task WarmupHotTemplatesAsync(int topCount = 20)
    {
        try
        {
            _logger.LogInformation("Warming up hot templates cache (top {Count})...", topCount);

            // Get top component templates by usage count
            var hotComponents = await ListComponentTemplatesAsync(
                type: null,
                tags: null,
                skip: 0,
                limit: topCount
            );

            // Get top material templates
            var hotMaterials = await ListMaterialTemplatesAsync(
                tags: null,
                skip: 0,
                limit: topCount / 2  // Typically fewer material types
            );

            // Trigger cache by calling batch GET (which will cache missing ones)
            if (hotComponents.Any())
            {
                var componentIds = hotComponents.Select(t => t.TemplateId).ToList();
                await GetComponentTemplatesAsync(componentIds, useCache: true);
            }

            if (hotMaterials.Any())
            {
                var materialIds = hotMaterials.Select(t => t.MaterialId).ToList();
                await GetMaterialTemplatesAsync(materialIds, useCache: true);
            }

            _logger.LogInformation(
                "Cache warmed up: {ComponentCount} components, {MaterialCount} materials",
                hotComponents.Count,
                hotMaterials.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to warmup templates cache. Cache will be populated on-demand.");
        }
    }
}
