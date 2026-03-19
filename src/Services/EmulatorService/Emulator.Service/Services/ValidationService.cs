using Emulator.Repository.Models;
using Emulator.Repository.Interfaces;
using Emulator.Repository.Entities;
using Emulator.Service.Interfaces;

namespace Emulator.Service.Services;

/// <summary>
/// Service for validating emulation structural integrity
/// </summary>
public class ValidationService : IValidationService
{
    public async Task<ValidationResult> ValidateEmulationIntegrityAsync(EmulationDefinition definition)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationError>();

        // 1. Validate template references exist (simplified - in real implementation, check database)
        foreach (var material in definition.Templates.Materials)
        {
            if (string.IsNullOrEmpty(material.Id) || string.IsNullOrEmpty(material.Source))
            {
                errors.Add(new ValidationError
                {
                    Type = "error",
                    Message = $"Invalid material template: missing id or source",
                    Location = "templates.materials"
                });
            }
        }

        foreach (var component in definition.Templates.Components)
        {
            if (string.IsNullOrEmpty(component.Id) || string.IsNullOrEmpty(component.Source))
            {
                errors.Add(new ValidationError
                {
                    Type = "error",
                    Message = $"Invalid component template: missing id or source",
                    Location = "templates.components"
                });
            }
        }

        // 2. Check for duplicate instance IDs
        var duplicateErrors = await ValidateDuplicateInstanceIdsAsync(definition.Instances);
        errors.AddRange(duplicateErrors);

        // 3. Validate connection references
        var connectionErrors = await ValidateConnectionReferencesAsync(definition.Connections, definition.Instances);
        errors.AddRange(connectionErrors);

        // 4. Validate action references
        var actionIds = new HashSet<string>(definition.Actions.Select(a => a.Id));
        foreach (var activity in definition.Activities)
        {
            foreach (var step in activity.Steps)
            {
                if (!actionIds.Contains(step.ActionId))
                {
                    errors.Add(new ValidationError
                    {
                        Type = "error",
                        Message = $"Activity step references non-existent action: {step.ActionId}",
                        Location = $"activities.{activity.Id}.steps"
                    });
                }
            }
        }

        return new ValidationResult
        {
            Valid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    public async Task<bool> ValidateTemplateReferencesAsync(List<TemplateReference> templates)
    {
        // Simplified - in production, check against template database
        return await Task.FromResult(templates.All(t => !string.IsNullOrEmpty(t.Id) && !string.IsNullOrEmpty(t.Source)));
    }

    public async Task<List<ValidationError>> ValidateDuplicateInstanceIdsAsync(Instances instances)
    {
        var errors = new List<ValidationError>();
        var allStrawIds = new HashSet<string>();
        var allConnectorIds = new HashSet<string>();

        // Check straw duplicates
        foreach (var group in instances.Straws)
        {
            foreach (var inst in group.Instances)
            {
                if (!allStrawIds.Add(inst.Id))
                {
                    errors.Add(new ValidationError
                    {
                        Type = "error",
                        Message = $"Duplicate straw ID: {inst.Id}",
                        ComponentId = inst.Id,
                        Location = "instances.straws"
                    });
                }
            }
        }

        // Check connector duplicates
        foreach (var group in instances.Connectors)
        {
            foreach (var inst in group.Instances)
            {
                if (!allConnectorIds.Add(inst.Id))
                {
                    errors.Add(new ValidationError
                    {
                        Type = "error",
                        Message = $"Duplicate connector ID: {inst.Id}",
                        ComponentId = inst.Id,
                        Location = "instances.connectors"
                    });
                }
            }
        }

        return await Task.FromResult(errors);
    }

    public async Task<List<ValidationError>> ValidateConnectionReferencesAsync(
        Dictionary<string, List<Connection>> connections,
        Instances instances)
    {
        var errors = new List<ValidationError>();

        // Build ID sets
        var allStrawIds = new HashSet<string>(
            instances.Straws.SelectMany(g => g.Instances.Select(i => i.Id))
        );
        var allConnectorIds = new HashSet<string>(
            instances.Connectors.SelectMany(g => g.Instances.Select(i => i.Id))
        );

        // Validate each connection
        foreach (var kvp in connections)
        {
            foreach (var conn in kvp.Value)
            {
                if (!allStrawIds.Contains(conn.StrawId))
                {
                    errors.Add(new ValidationError
                    {
                        Type = "error",
                        Message = $"Connection references non-existent straw: {conn.StrawId}",
                        Location = $"connections.{kvp.Key}",
                        ComponentId = conn.StrawId
                    });
                }

                if (!allConnectorIds.Contains(conn.ConnectorId))
                {
                    errors.Add(new ValidationError
                    {
                        Type = "error",
                        Message = $"Connection references non-existent connector: {conn.ConnectorId}",
                        Location = $"connections.{kvp.Key}",
                        ComponentId = conn.ConnectorId
                    });
                }

                // Validate endpoint values
                if (conn.Endpoint != "start" && conn.Endpoint != "end")
                {
                    errors.Add(new ValidationError
                    {
                        Type = "error",
                        Message = $"Invalid connection endpoint: {conn.Endpoint} (must be 'start' or 'end')",
                        Location = $"connections.{kvp.Key}"
                    });
                }
            }
        }

        return await Task.FromResult(errors);
    }
}
