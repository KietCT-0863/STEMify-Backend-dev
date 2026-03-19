using Emulator.Repository.Entities;

namespace Emulator.Service.Interfaces;

/// <summary>
/// Service interface for managing component and material templates
/// </summary>
public interface ITemplateService
{
    // ============================================
    // Component Template Operations
    // ============================================

    /// <summary>
    /// Create a new component template
    /// </summary>
    Task<ComponentTemplate> CreateComponentTemplateAsync(
        string templateId,
        string type,
        string version,
        Dictionary<string, object> definition,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        string? createdBy = null,
        string? organizationId = null);

    /// <summary>
    /// Upsert component template (insert if not exists, update if exists)
    /// </summary>
    Task<(ComponentTemplate template, bool wasCreated)> UpsertComponentTemplateAsync(
        string templateId,
        string type,
        string version,
        Dictionary<string, object> definition,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        string? createdBy = null,
        string? organizationId = null);

    /// <summary>
    /// Get component template by ID with optional caching
    /// </summary>
    Task<ComponentTemplate?> GetComponentTemplateAsync(string templateId, bool useCache = true);

    /// <summary>
    /// Get multiple component templates (batch operation)
    /// </summary>
    Task<List<ComponentTemplate>> GetComponentTemplatesAsync(List<string> templateIds, bool useCache = true);

    /// <summary>
    /// List published component templates with filtering
    /// </summary>
    Task<List<ComponentTemplate>> ListComponentTemplatesAsync(
        string? type = null,
        List<string>? tags = null,
        int skip = 0,
        int limit = 100);

    /// <summary>
    /// Update component template
    /// </summary>
    Task<bool> UpdateComponentTemplateAsync(
        string templateId,
        Dictionary<string, object>? definition = null,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        bool? isPublished = null);

    /// <summary>
    /// Delete component template
    /// </summary>
    Task<bool> DeleteComponentTemplateAsync(string templateId);

    // ============================================
    // Material Template Operations
    // ============================================

    /// <summary>
    /// Create a new material template
    /// </summary>
    Task<MaterialTemplate> CreateMaterialTemplateAsync(
        string materialId,
        string version,
        Dictionary<string, object> definition,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        string? createdBy = null,
        string? organizationId = null);

    /// <summary>
    /// Upsert material template (insert if not exists, update if exists)
    /// </summary>
    Task<(MaterialTemplate template, bool wasCreated)> UpsertMaterialTemplateAsync(
        string materialId,
        string version,
        Dictionary<string, object> definition,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        string? createdBy = null,
        string? organizationId = null);

    /// <summary>
    /// Get material template by ID with optional caching
    /// </summary>
    Task<MaterialTemplate?> GetMaterialTemplateAsync(string materialId, bool useCache = true);

    /// <summary>
    /// Get multiple material templates (batch operation)
    /// </summary>
    Task<List<MaterialTemplate>> GetMaterialTemplatesAsync(List<string> materialIds, bool useCache = true);

    /// <summary>
    /// List published material templates with filtering
    /// </summary>
    Task<List<MaterialTemplate>> ListMaterialTemplatesAsync(
        List<string>? tags = null,
        int skip = 0,
        int limit = 100);

    /// <summary>
    /// Update material template
    /// </summary>
    Task<bool> UpdateMaterialTemplateAsync(
        string materialId,
        Dictionary<string, object>? definition = null,
        string? name = null,
        string? description = null,
        List<string>? tags = null,
        bool? isPublished = null);

    /// <summary>
    /// Delete material template
    /// </summary>
    Task<bool> DeleteMaterialTemplateAsync(string materialId);

    // ============================================
    // Validation & Utilities
    // ============================================

    /// <summary>
    /// Validate that all template references exist
    /// </summary>
    Task<(bool isValid, List<string> missingTemplates)> ValidateTemplateReferencesAsync(
        List<string> componentTemplateIds,
        List<string> materialTemplateIds);

    /// <summary>
    /// Increment usage count when template is used in an emulation
    /// </summary>
    Task IncrementTemplateUsageAsync(List<string> componentTemplateIds, List<string> materialTemplateIds);

    /// <summary>
    /// Import templates from octahedron.json format
    /// </summary>
    Task<(int componentCount, int materialCount)> ImportTemplatesFromOctahedronAsync(
        Dictionary<string, object> octahedronData,
        string? createdBy = null);

    /// <summary>
    /// </summary>
    /// <param name="topCount">Number of top templates to preload (default: 20)</param>
    Task WarmupHotTemplatesAsync(int topCount = 20);
}
