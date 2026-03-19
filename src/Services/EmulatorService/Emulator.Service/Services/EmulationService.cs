using Emulator.Repository.Models;
using Emulator.Service.Interfaces;
using Emulator.Repository.Entities;
using Shared.SeedWork;
using Emulator.Repository.Interfaces;
using Caching.Cache;
using Microsoft.Extensions.Logging;

namespace Emulator.Service.Services;

/// <summary>
/// Service implementation for Emulation business logic
/// </summary>
public class EmulationService : IEmulationService
{
    private readonly IEmulationRepository _repository;
    private readonly IValidationService _validationService;
    private readonly Contracts.Abstractions.Services.ICloudinaryService _cloudinaryService;
    private readonly ITemplateService _templateService;
    private readonly ICacheRedis _cache;
    private readonly ILogger<EmulationService> _logger;

    // - Published emulations: 1 hour
    // - Draft emulations: 15 minutes
    private static readonly TimeSpan PublishedEmulationCacheTTL = TimeSpan.FromHours(1);
    private static readonly TimeSpan DraftEmulationCacheTTL = TimeSpan.FromMinutes(15);

    public EmulationService(
        IEmulationRepository repository,
        IValidationService validationService,
        ITemplateService templateService,
        ICacheRedis cache,
        ILogger<EmulationService> logger,
        Contracts.Abstractions.Services.ICloudinaryService cloudinaryService)
    {
        _repository = repository;
        _validationService = validationService;
        _templateService = templateService;
        _cache = cache;
        _logger = logger;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<ApiResult<EmulationDto>> CreateEmulationAsync(CreateEmulationRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Creating emulation: {Name} for user: {UserId}", request.Name, userId);

            // Generate unique IDs
            var emulationId = $"emu_{Guid.NewGuid().ToString("N")[..12]}";
            var slug = GenerateSlug(request.Name);

            // Calculate statistics
            var statistics = CalculateStatistics(request.Definition);

            var emulation = new Emulation
            {
                EmulationId = emulationId,
                Name = request.Name,
                Slug = slug,
                Description = request.Description,
                CreatedBy = userId,
                Visibility = request.Visibility,
                Definition = request.Definition,
                Statistics = statistics,
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Upload thumbnail to Cloudinary if provided
            if (!string.IsNullOrWhiteSpace(request.ThumbnailImageBase64) && !string.IsNullOrWhiteSpace(request.ThumbnailFileName))
            {
                try
                {
                    var bytes = Convert.FromBase64String(request.ThumbnailImageBase64);
                    var uploadReq = new Shared.DTOs.Cloudinary.UploadImageBytesRequest
                    {
                        FileBytes = bytes,
                        FileName = request.ThumbnailFileName
                    };
                    var uploadRes = await _cloudinaryService.UploadImageAsync(uploadReq);
                    emulation.ThumbnailUrl = uploadRes.AssetUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Thumbnail upload failed; continuing without thumbnail for emulation {EmulationId}", emulationId);
                }
            }

            await _repository.CreateAsync(emulation);

            var cacheKey = $"{CacheKeys.Key_Emulation_By_Id}:{emulationId}";
            await _cache.SetAsync(cacheKey, emulation, DraftEmulationCacheTTL);
            _logger.LogDebug("Cached emulation: {CacheKey} with TTL: {TTL}min", cacheKey, DraftEmulationCacheTTL.TotalMinutes);

            _logger.LogInformation("Emulation created: {EmulationId}", emulationId);
            return ApiResult<EmulationDto>.Succeeded(MapToDto(emulation), "Emulation created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create emulation: {Name}", request.Name);
            return ApiResult<EmulationDto>.Failed($"Failed to create emulation: {ex.Message}");
        }
    }

    public async Task<ApiResult<EmulationDetailDto?>> GetEmulationByIdAsync(
        string emulationId,
        bool includeTemplates = false,
        bool includeStatistics = false,
        string? version = null)
    {
        try
        {
            _logger.LogDebug("Getting emulation: {EmulationId}, includeTemplates: {IncludeTemplates}",
                emulationId, includeTemplates);

            // Phase 4 CQRS: Separate cache keys for different query variations
            var cacheKey = $"{CacheKeys.Key_Emulation_By_Id}:{emulationId}:{version ?? "latest"}:{includeTemplates}";

            // Check cache first (Query-side optimization)
            if (includeTemplates)
            {
                var cachedDetail = await _cache.GetAsync<EmulationDetailDto>(cacheKey);
                if (cachedDetail != null)
                {
                    _logger.LogDebug("Cache hit for emulation detail with templates: {EmulationId}", emulationId);
                    return ApiResult<EmulationDetailDto?>.Succeeded(cachedDetail);
                }
            }

            // Fetch emulation from database
            // TODO Phase 5: Implement version history support in repository
            var emulation = await _repository.GetByIdAsync(emulationId);

            if (emulation == null)
                return ApiResult<EmulationDetailDto?>.Failed("Emulation not found", 404);

            // Phase 4: Resolve templates if requested (CQRS Query optimization)
            if (includeTemplates && emulation.Definition != null)
            {
                await ResolveTemplatesAsync(emulation);
            }

            // Map to DTO
        var result = new EmulationDetailDto
            {
                EmulationId = emulation.EmulationId,
                Name = emulation.Name,
                Slug = emulation.Slug,
                Version = emulation.Version,
                Status = emulation.Status,
                ThumbnailUrl = emulation.ThumbnailUrl,
                Statistics = emulation.Statistics,
                CreatedAt = emulation.CreatedAt,
                UpdatedAt = emulation.UpdatedAt,
                Definition = emulation.Definition,
                Description = emulation.Description,
                CreatedBy = new UserInfo
                    {
                        UserId = emulation.CreatedBy,
                        Name = "Unknown User" // TODO: Fetch from Identity service
                    }
                };

            // Cache result (Query-side caching)
            if (includeTemplates)
            {
                var cacheTTL = emulation.Status == "published"
                    ? PublishedEmulationCacheTTL  // 1 hour for published
                    : DraftEmulationCacheTTL;      // 15 minutes for drafts

                await _cache.SetAsync(cacheKey, result, cacheTTL);
                _logger.LogDebug("Cached emulation detail with templates: {EmulationId}, TTL: {TTL}min",
                    emulationId, cacheTTL.TotalMinutes);
            }

            return ApiResult<EmulationDetailDto?>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get emulation: {EmulationId}", emulationId);
            return ApiResult<EmulationDetailDto?>.Failed($"Failed to get emulation: {ex.Message}");
        }
    }

    /// <summary>
    /// Phase 4 CQRS: Resolve template references in emulation definition
    /// Populates _resolved fields in TemplateReference objects
    /// Uses batch loading with caching for optimal performance
    /// </summary>
    private async Task ResolveTemplatesAsync(Emulation emulation)
    {
        if (emulation.Definition?.Templates == null)
            return;

        // Extract all template IDs from definition
        var materialIds = emulation.Definition.Templates.Materials
            ?.Select(m => m.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList() ?? new List<string>();

        var componentIds = emulation.Definition.Templates.Components
            ?.Select(c => c.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList() ?? new List<string>();

        if (!materialIds.Any() && !componentIds.Any())
        {
            _logger.LogDebug("No templates to resolve for emulation: {EmulationId}", emulation.EmulationId);
            return;
        }

        _logger.LogDebug("Resolving {MaterialCount} materials and {ComponentCount} components for emulation: {EmulationId}",
            materialIds.Count, componentIds.Count, emulation.EmulationId);

        // Phase 4: Batch load templates with caching (already implemented in Phase 2!)
        // This will use parallel cache GET/SET and single DB query if cache miss
        var materialsTask = materialIds.Any()
            ? _templateService.GetMaterialTemplatesAsync(materialIds, useCache: true)
            : Task.FromResult(new List<MaterialTemplate>());

        var componentsTask = componentIds.Any()
            ? _templateService.GetComponentTemplatesAsync(componentIds, useCache: true)
            : Task.FromResult(new List<ComponentTemplate>());

        await Task.WhenAll(materialsTask, componentsTask);

        var materials = await materialsTask;
        var components = await componentsTask;

        // Populate _resolved fields in template references
        if (emulation.Definition.Templates.Materials != null)
        {
            foreach (var materialRef in emulation.Definition.Templates.Materials)
            {
                var template = materials.FirstOrDefault(m => m.MaterialId == materialRef.Id);
                if (template != null)
                {
                    // Convert template definition to BsonDocument for _resolved field
                    materialRef.Resolved = MongoDB.Bson.BsonDocument.Parse(
                        System.Text.Json.JsonSerializer.Serialize(template.Definition)
                    );

                    _logger.LogTrace("Resolved material template: {MaterialId}", materialRef.Id);
                }
                else
                {
                    _logger.LogWarning("Material template not found: {MaterialId} for emulation: {EmulationId}",
                        materialRef.Id, emulation.EmulationId);
                }
            }
        }

        if (emulation.Definition.Templates.Components != null)
        {
            foreach (var componentRef in emulation.Definition.Templates.Components)
            {
                var template = components.FirstOrDefault(c => c.TemplateId == componentRef.Id);
                if (template != null)
                {
                    // Convert template definition to BsonDocument for _resolved field
                    componentRef.Resolved = MongoDB.Bson.BsonDocument.Parse(
                        System.Text.Json.JsonSerializer.Serialize(template.Definition)
                    );

                    _logger.LogTrace("Resolved component template: {TemplateId}", componentRef.Id);
                }
                else
                {
                    _logger.LogWarning("Component template not found: {TemplateId} for emulation: {EmulationId}",
                        componentRef.Id, emulation.EmulationId);
                }
            }
        }

        _logger.LogInformation("Templates resolved for emulation: {EmulationId} - {MaterialCount} materials, {ComponentCount} components",
            emulation.EmulationId, materials.Count, components.Count);
    }

    /// <summary>
    /// Phase 4 CQRS: Invalidate all cache variations for an emulation
    /// Must be called after any Write operation (Update, Delete, Publish)
    /// Ensures Read cache stays consistent with Write operations
    /// </summary>
    private async Task InvalidateEmulationCacheAsync(string emulationId)
    {
        // Invalidate all cache key variations
        // Pattern: key_emulation_by_id:{id}:{version}:{includeTemplates}
        var cacheKeysToInvalidate = new[]
        {
            $"{CacheKeys.Key_Emulation_By_Id}:{emulationId}",  // Old simple key
            $"{CacheKeys.Key_Emulation_By_Id}:{emulationId}:latest:true",  // With templates
            $"{CacheKeys.Key_Emulation_By_Id}:{emulationId}:latest:false", // Without templates
            // Note: Version-specific caches will expire naturally via TTL
        };

        var invalidationTasks = cacheKeysToInvalidate.Select(key => _cache.ClearAsync(key));
        await Task.WhenAll(invalidationTasks);

        _logger.LogDebug("Invalidated {Count} cache entries for emulation: {EmulationId}",
            cacheKeysToInvalidate.Length, emulationId);
    }

    public async Task<ApiResult<PagedResult<EmulationListDto>>> ListEmulationsAsync(EmulationFilterParams filters)
    {
        try
        {
            var result = await _repository.GetPagedAsync(filters);

            var pagedResult = new PagedResult<EmulationListDto>(
                result.Items.Select(e => new EmulationListDto
                {
                    EmulationId = e.EmulationId,
                    Name = e.Name,
                    Slug = e.Slug,
                    Description = e.Description,
                    Difficulty = e.Definition?.Metadata?.Difficulty ?? string.Empty,
                    ThumbnailUrl = e.ThumbnailUrl,
                    Statistics = e.Statistics,
                    CreatedAt = e.CreatedAt,
                    Status = e.Status,
                    CreatedBy = new UserInfo
                    {
                        UserId = e.CreatedBy,
                        Name = "Unknown User" // TODO: Fetch from Identity service
                    }
                }).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );

            return ApiResult<PagedResult<EmulationListDto>>.Succeeded(pagedResult);
        }
        catch (Exception ex)
        {
            return ApiResult<PagedResult<EmulationListDto>>.Failed($"Failed to list emulations: {ex.Message}");
        }
    }

    public async Task<ApiResult<EmulationDto>> UpdateEmulationAsync(string emulationId, UpdateEmulationRequest request)
    {
        try
        {
            _logger.LogInformation("Updating emulation: {EmulationId}", emulationId);

            var emulation = await _repository.GetByIdAsync(emulationId);
            if (emulation == null)
                return ApiResult<EmulationDto>.Failed("Emulation not found", 404);

            // Update fields
            if (!string.IsNullOrEmpty(request.Name))
                emulation.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Description))
                emulation.Description = request.Description;

            if (request.Definition != null)
            {
                emulation.Definition = request.Definition;
                emulation.Statistics = CalculateStatistics(request.Definition);
            }

            // Optional thumbnail update via Cloudinary
            if (!string.IsNullOrWhiteSpace(request.ThumbnailImageBase64))
            {
                try
                {
                    var bytes = Convert.FromBase64String(request.ThumbnailImageBase64);
                    var uploadReq = new Shared.DTOs.Cloudinary.UploadImageBytesRequest
                    {
                        FileBytes = bytes,
                        FileName = request.ThumbnailFileName ?? $"emulation_{emulationId}_thumbnail.png"
                    };
                    var uploadRes = await _cloudinaryService.UploadImageAsync(uploadReq);
                    emulation.ThumbnailUrl = uploadRes.AssetUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Thumbnail upload failed during update; keeping existing thumbnail for {EmulationId}", emulationId);
                }
            }

            if (!string.IsNullOrEmpty(request.Status))
                emulation.Status = request.Status;

            emulation.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(emulation);

            // Phase 4 CQRS: Invalidate ALL cache variations (Command invalidates Read cache)
            await InvalidateEmulationCacheAsync(emulationId);

            _logger.LogInformation("Emulation updated: {EmulationId}", emulationId);
            return ApiResult<EmulationDto>.Succeeded(MapToDto(emulation), "Emulation updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update emulation: {EmulationId}", emulationId);
            return ApiResult<EmulationDto>.Failed($"Failed to update emulation: {ex.Message}");
        }
    }

    public async Task<ApiResult> DeleteEmulationAsync(string emulationId, bool permanent = false)
    {
        try
        {
            _logger.LogInformation("Deleting emulation: {EmulationId} (permanent: {Permanent})", emulationId, permanent);

            var exists = await _repository.ExistsAsync(emulationId);
            if (!exists)
                return ApiResult.Failed("Emulation not found", 404);

            await _repository.DeleteAsync(emulationId, permanent);

            // Phase 4 CQRS: Invalidate all cache variations
            await InvalidateEmulationCacheAsync(emulationId);

            _logger.LogInformation("Emulation deleted: {EmulationId}", emulationId);
            return ApiResult.Success(permanent ? "Emulation permanently deleted" : "Emulation soft deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete emulation: {EmulationId}", emulationId);
            return ApiResult.Failed($"Failed to delete emulation: {ex.Message}");
        }
    }

    public async Task<ApiResult<object>> PublishEmulationAsync(string emulationId)
    {
        try
        {
            _logger.LogInformation("Publishing emulation: {EmulationId}", emulationId);

            var emulation = await _repository.GetByIdAsync(emulationId);
            if (emulation == null)
                return ApiResult<object>.Failed("Emulation not found", 404);

            var validationResult = await _validationService.ValidateEmulationIntegrityAsync(emulation.Definition);
            if (!validationResult.Valid)
            {
                return ApiResult<object>.Failed($"Cannot publish emulation with validation errors: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
            }

            var existingDescription = emulation.Description;

            emulation.Status = "published";
            emulation.PublishedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(emulation.Description) && !string.IsNullOrEmpty(existingDescription))
            {
                emulation.Description = existingDescription;
            }

            await _repository.UpdateAsync(emulation);

            // Phase 4 CQRS: Invalidate all cache variations (status changed from draft to published)
            await InvalidateEmulationCacheAsync(emulationId);

            var result = new
            {
                emulation.EmulationId,
                emulation.Status,
                emulation.PublishedAt
            };

            _logger.LogInformation("Emulation published: {EmulationId}", emulationId);
            return ApiResult<object>.Succeeded(result, "Emulation published successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish emulation: {EmulationId}", emulationId);
            return ApiResult<object>.Failed($"Failed to publish emulation: {ex.Message}");
        }
    }

    public async Task<ApiResult<ValidationResult>> ValidateEmulationAsync(string emulationId)
    {
        try
        {
            var emulation = await _repository.GetByIdAsync(emulationId);
            if (emulation == null)
                return ApiResult<ValidationResult>.Failed("Emulation not found", 404);

            var result = await _validationService.ValidateEmulationIntegrityAsync(emulation.Definition);
            return ApiResult<ValidationResult>.Succeeded(result);
        }
        catch (Exception ex)
        {
            return ApiResult<ValidationResult>.Failed($"Failed to validate emulation: {ex.Message}");
        }
    }

    public async Task<ApiResult<List<VersionHistory>>> GetVersionHistoryAsync(string emulationId)
    {
        try
        {
            var emulation = await _repository.GetByIdAsync(emulationId);
            if (emulation == null)
                return ApiResult<List<VersionHistory>>.Failed("Emulation not found", 404);

            return ApiResult<List<VersionHistory>>.Succeeded(emulation.VersionHistory);
        }
        catch (Exception ex)
        {
            return ApiResult<List<VersionHistory>>.Failed($"Failed to get version history: {ex.Message}");
        }
    }

    public async Task<ApiResult<EmulationDto>> DuplicateEmulationAsync(string emulationId, string userId)
    {
        try
        {
            var original = await _repository.GetByIdAsync(emulationId);
            if (original == null)
                return ApiResult<EmulationDto>.Failed("Emulation not found", 404);

            var duplicateId = $"emu_{Guid.NewGuid().ToString("N")[..12]}";
            var duplicateName = $"{original.Name} (Copy)";
            var slug = GenerateSlug(duplicateName);

            var duplicate = new Emulation
            {
                EmulationId = duplicateId,
                Name = duplicateName,
                Slug = slug,
                Description = original.Description,
                CreatedBy = userId,
                Visibility = "private",
                Definition = original.Definition,
                Statistics = original.Statistics,
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.CreateAsync(duplicate);

            return ApiResult<EmulationDto>.Succeeded(MapToDto(duplicate), "Emulation duplicated successfully");
        }
        catch (Exception ex)
        {
            return ApiResult<EmulationDto>.Failed($"Failed to duplicate emulation: {ex.Message}");
        }
    }

    // Helper methods
    private EmulationDto MapToDto(Emulation emulation)
    {
        return new EmulationDto
        {
            EmulationId = emulation.EmulationId,
            Name = emulation.Name,
            Slug = emulation.Slug,
            Version = emulation.Version,
            Status = emulation.Status,
            Statistics = emulation.Statistics,
            CreatedAt = emulation.CreatedAt,
            UpdatedAt = emulation.UpdatedAt
        };
    }

    private string GenerateSlug(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("_", "-")
            + "-" + Guid.NewGuid().ToString("N")[..6];
    }

    private EmulationStatistics CalculateStatistics(EmulationDefinition definition)
    {
        var strawCount = definition.Instances.Straws.Sum(g => g.Instances.Count);
        var connectorCount = definition.Instances.Connectors.Sum(g => g.Instances.Count);
        var connectionCount = definition.Connections.Values.Sum(c => c.Count);

        return new EmulationStatistics
        {
            InstanceCount = new InstanceCount
            {
                Straws = strawCount,
                Connectors = connectorCount,
                Total = strawCount + connectorCount
            },
            ConnectionCount = connectionCount,
            ActionCount = definition.Actions.Count,
            ActivityCount = definition.Activities.Count,
            EstimatedComplexity = CalculateComplexity(strawCount + connectorCount, connectionCount)
        };
    }

    private string CalculateComplexity(int instanceCount, int connectionCount)
    {
        var totalComplexity = instanceCount + (connectionCount * 2);
        return totalComplexity switch
        {
            < 50 => "low",
            < 150 => "medium",
            _ => "high"
        };
    }
}