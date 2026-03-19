using Emulator.Repository.Models;
using Emulator.Repository.Entities;

namespace Emulator.Service.Interfaces;

/// <summary>
/// Service interface for structural validation
/// </summary>
public interface IValidationService
{
    Task<ValidationResult> ValidateEmulationIntegrityAsync(EmulationDefinition definition);
    Task<bool> ValidateTemplateReferencesAsync(List<TemplateReference> templates);
    Task<List<ValidationError>> ValidateDuplicateInstanceIdsAsync(Instances instances);
    Task<List<ValidationError>> ValidateConnectionReferencesAsync(Dictionary<string, List<Connection>> connections, Instances instances);
}
