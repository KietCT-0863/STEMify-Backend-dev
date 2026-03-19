using Emulator.Repository.Entities;
using Emulator.Repository.Models;

namespace Emulator.Repository.Interfaces;

/// <summary>
/// Repository interface for Emulation entity
/// </summary>
public interface IEmulationRepository
{
    Task<Emulation> CreateAsync(Emulation emulation);
    Task<Emulation?> GetByIdAsync(string emulationId);
    Task<Emulation?> GetBySlugAsync(string slug);
    Task<PagedResult<Emulation>> GetPagedAsync(EmulationFilterParams filters);
    Task<bool> UpdateAsync(Emulation emulation);
    Task<bool> DeleteAsync(string emulationId, bool permanent = false);
    Task<bool> ExistsAsync(string emulationId);
    Task<List<Emulation>> GetByCreatorAsync(string userId);
    Task<List<Emulation>> GetPublishedAsync(int limit = 10);
}
