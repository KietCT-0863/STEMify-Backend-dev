using Emulator.Repository.Entities;
using Emulator.Repository.Models;
using Shared.SeedWork;

namespace Emulator.Service.Interfaces;

/// <summary>
/// Service interface for Emulation business logic
/// </summary>
public interface IEmulationService
{
    Task<ApiResult<EmulationDto>> CreateEmulationAsync(CreateEmulationRequest request, string userId);
    Task<ApiResult<EmulationDetailDto?>> GetEmulationByIdAsync(string emulationId, bool includeTemplates = false, bool includeStatistics = false, string? version = null);
    Task<ApiResult<PagedResult<EmulationListDto>>> ListEmulationsAsync(EmulationFilterParams filters);
    Task<ApiResult<EmulationDto>> UpdateEmulationAsync(string emulationId, UpdateEmulationRequest request);
    Task<ApiResult> DeleteEmulationAsync(string emulationId, bool permanent = false);
    Task<ApiResult<object>> PublishEmulationAsync(string emulationId);
    Task<ApiResult<ValidationResult>> ValidateEmulationAsync(string emulationId);
    Task<ApiResult<List<VersionHistory>>> GetVersionHistoryAsync(string emulationId);
    Task<ApiResult<EmulationDto>> DuplicateEmulationAsync(string emulationId, string userId);
}
