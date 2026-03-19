using Emulator.Repository.Entities;

namespace Emulator.Repository.Interfaces;

/// <summary>
/// Repository interface for managing component and material templates
/// </summary>
public interface ITemplateRepository
{
    // ============================================
    // Component Template Operations
    // ============================================

    /// <summary>
    /// Create a new component template
    /// </summary>
    Task<ComponentTemplate> CreateComponentTemplateAsync(ComponentTemplate template);

    /// <summary>
    /// Upsert component template (insert if not exists, replace if exists)
    /// </summary>
    Task<(ComponentTemplate template, bool wasCreated)> UpsertComponentTemplateAsync(ComponentTemplate template);

    /// <summary>
    /// Get component template by ID
    /// </summary>
    Task<ComponentTemplate?> GetComponentTemplateByIdAsync(string templateId);

    /// <summary>
    /// Get multiple component templates by IDs
    /// </summary>
    Task<List<ComponentTemplate>> GetComponentTemplatesByIdsAsync(List<string> templateIds);

    /// <summary>
    /// Get all published component templates with optional filtering
    /// </summary>
    Task<List<ComponentTemplate>> GetPublishedComponentTemplatesAsync(
        string? type = null,
        List<string>? tags = null,
        int skip = 0,
        int limit = 100);

    /// <summary>
    /// Update component template
    /// </summary>
    Task<bool> UpdateComponentTemplateAsync(ComponentTemplate template);

    /// <summary>
    /// Delete component template
    /// </summary>
    Task<bool> DeleteComponentTemplateAsync(string templateId);

    /// <summary>
    /// Check if component template exists
    /// </summary>
    Task<bool> ComponentTemplateExistsAsync(string templateId);

    /// <summary>
    /// Increment usage count for component template
    /// </summary>
    Task IncrementComponentTemplateUsageAsync(string templateId);

    // ============================================
    // Material Template Operations
    // ============================================

    /// <summary>
    /// Create a new material template
    /// </summary>
    Task<MaterialTemplate> CreateMaterialTemplateAsync(MaterialTemplate template);

    /// <summary>
    /// Upsert material template (insert if not exists, replace if exists)
    /// </summary>
    Task<(MaterialTemplate template, bool wasCreated)> UpsertMaterialTemplateAsync(MaterialTemplate template);

    /// <summary>
    /// Get material template by ID
    /// </summary>
    Task<MaterialTemplate?> GetMaterialTemplateByIdAsync(string materialId);

    /// <summary>
    /// Get multiple material templates by IDs
    /// </summary>
    Task<List<MaterialTemplate>> GetMaterialTemplatesByIdsAsync(List<string> materialIds);

    /// <summary>
    /// Get all published material templates with optional filtering
    /// </summary>
    Task<List<MaterialTemplate>> GetPublishedMaterialTemplatesAsync(
        List<string>? tags = null,
        int skip = 0,
        int limit = 100);

    /// <summary>
    /// Update material template
    /// </summary>
    Task<bool> UpdateMaterialTemplateAsync(MaterialTemplate template);

    /// <summary>
    /// Delete material template
    /// </summary>
    Task<bool> DeleteMaterialTemplateAsync(string materialId);

    /// <summary>
    /// Check if material template exists
    /// </summary>
    Task<bool> MaterialTemplateExistsAsync(string materialId);

    /// <summary>
    /// Increment usage count for material template
    /// </summary>
    Task IncrementMaterialTemplateUsageAsync(string materialId);

    // ============================================
    // Batch Operations
    // ============================================

    /// <summary>
    /// Validate all template references exist
    /// </summary>
    Task<(bool allExist, List<string> missingTemplateIds)> ValidateTemplateReferencesAsync(
        List<string> componentTemplateIds,
        List<string> materialTemplateIds);
}
