using Emulator.API.Protos;
using Emulator.API.Utilities;
using Emulator.Service.Interfaces;
using Grpc.Core;
using System.Text.Json;

namespace Emulator.API.Services
{
    public class TemplateGrpcService : Protos.TemplateService.TemplateServiceBase
    {
        private readonly ITemplateService _templateService;
        private readonly ILogger<TemplateGrpcService> _logger;

        public TemplateGrpcService(
            ITemplateService templateService,
            ILogger<TemplateGrpcService> logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

        // ============================================
        // Component Template Operations
        // ============================================

        public override async Task<ComponentTemplateResponse> CreateComponentTemplate(
            CreateComponentTemplateRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Creating/Upserting component template: {TemplateId}", request.TemplateId);

                // Handle empty or null definition JSON
                if (string.IsNullOrEmpty(request.DefinitionJson))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "definition_json is required"));
                }

                // Parse definition JSON
                var definitionRaw = JsonSerializer.Deserialize<Dictionary<string, object>>(request.DefinitionJson);

                if (definitionRaw == null)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid definition JSON format"));
                }

                // Convert JsonElement objects to primitives for MongoDB compatibility
                var definition = JsonElementConverter.ConvertJsonElementsToPrimitives(definitionRaw);

                // Use Upsert instead of Create to allow frontend to send same template multiple times
                var (template, wasCreated) = await _templateService.UpsertComponentTemplateAsync(
                    templateId: request.TemplateId,
                    type: request.Type,
                    version: request.Version,
                    definition: definition,
                    name: request.Name,
                    description: request.Description,
                    tags: request.Tags.ToList(),
                    createdBy: request.CreatedBy,
                    organizationId: request.OrganizationId);

                _logger.LogInformation("Component template {Action}: {TemplateId}",
                    wasCreated ? "created" : "updated", request.TemplateId);

                return new ComponentTemplateResponse
                {
                    Success = true,
                    Template = MapToProtoComponentTemplate(template)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting component template");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        public override async Task<ComponentTemplateResponse> GetComponentTemplate(
            GetTemplateRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("Getting component template: {TemplateId}", request.Id);

                var template = await _templateService.GetComponentTemplateAsync(request.Id, useCache: true);

                if (template == null)
                {
                    return new ComponentTemplateResponse
                    {
                        Success = false,
                        ErrorMessage = $"Component template '{request.Id}' not found"
                    };
                }

                return new ComponentTemplateResponse
                {
                    Success = true,
                    Template = MapToProtoComponentTemplate(template)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting component template");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        public override async Task<ComponentTemplateListResponse> ListComponentTemplates(
            ListComponentTemplatesRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("Listing component templates");

                var templates = await _templateService.ListComponentTemplatesAsync(
                    type: request.Type,
                    tags: request.Tags.ToList(),
                    skip: request.Skip,
                    limit: request.Limit);

                var response = new ComponentTemplateListResponse
                {
                    Success = true,
                    Total = templates.Count
                };

                response.Templates.AddRange(templates.Select(MapToProtoComponentTemplate));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing component templates");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        public override async Task<OperationResponse> UpdateComponentTemplate(
            UpdateComponentTemplateRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Updating component template: {TemplateId}", request.TemplateId);

                Dictionary<string, object>? definition = null;
                if (!string.IsNullOrEmpty(request.DefinitionJson))
                {
                    var definitionRaw = JsonSerializer.Deserialize<Dictionary<string, object>>(request.DefinitionJson);
                    if (definitionRaw != null)
                    {
                        definition = JsonElementConverter.ConvertJsonElementsToPrimitives(definitionRaw);
                    }
                }

                var success = await _templateService.UpdateComponentTemplateAsync(
                    templateId: request.TemplateId,
                    definition: definition,
                    name: string.IsNullOrEmpty(request.Name) ? null : request.Name,
                    description: string.IsNullOrEmpty(request.Description) ? null : request.Description,
                    tags: request.Tags.Count > 0 ? request.Tags.ToList() : null,
                    isPublished: request.HasIsPublished ? request.IsPublished : null);

                return new OperationResponse
                {
                    Success = success,
                    ErrorMessage = success ? "" : "Failed to update component template"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating component template");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        public override async Task<OperationResponse> DeleteComponentTemplate(
            DeleteTemplateRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Deleting component template: {TemplateId}", request.Id);

                var success = await _templateService.DeleteComponentTemplateAsync(request.Id);

                return new OperationResponse
                {
                    Success = success,
                    ErrorMessage = success ? "" : "Failed to delete component template"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting component template");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        // ============================================
        // Material Template Operations
        // ============================================

        public override async Task<MaterialTemplateResponse> CreateMaterialTemplate(
            CreateMaterialTemplateRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Creating/Upserting material template: {MaterialId}", request.MaterialId);

                // Handle empty or null definition JSON
                if (string.IsNullOrEmpty(request.DefinitionJson))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "definition_json is required"));
                }

                var definitionRaw = JsonSerializer.Deserialize<Dictionary<string, object>>(request.DefinitionJson);

                if (definitionRaw == null)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid definition JSON format"));
                }

                // Convert JsonElement objects to primitives for MongoDB compatibility
                var definition = JsonElementConverter.ConvertJsonElementsToPrimitives(definitionRaw);

                // Use Upsert instead of Create to allow frontend to send same template multiple times
                var (template, wasCreated) = await _templateService.UpsertMaterialTemplateAsync(
                    materialId: request.MaterialId,
                    version: request.Version,
                    definition: definition,
                    name: request.Name,
                    description: request.Description,
                    tags: request.Tags.ToList(),
                    createdBy: request.CreatedBy,
                    organizationId: request.OrganizationId);

                _logger.LogInformation("Material template {Action}: {MaterialId}",
                    wasCreated ? "created" : "updated", request.MaterialId);

                return new MaterialTemplateResponse
                {
                    Success = true,
                    Template = MapToProtoMaterialTemplate(template)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting material template");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        public override async Task<MaterialTemplateResponse> GetMaterialTemplate(
            GetTemplateRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("Getting material template: {MaterialId}", request.Id);

                var template = await _templateService.GetMaterialTemplateAsync(request.Id, useCache: true);

                if (template == null)
                {
                    return new MaterialTemplateResponse
                    {
                        Success = false,
                        ErrorMessage = $"Material template '{request.Id}' not found"
                    };
                }

                return new MaterialTemplateResponse
                {
                    Success = true,
                    Template = MapToProtoMaterialTemplate(template)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting material template");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        public override async Task<MaterialTemplateListResponse> ListMaterialTemplates(
            ListMaterialTemplatesRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("Listing material templates");

                var templates = await _templateService.ListMaterialTemplatesAsync(
                    tags: request.Tags.ToList(),
                    skip: request.Skip,
                    limit: request.Limit);

                var response = new MaterialTemplateListResponse
                {
                    Success = true,
                    Total = templates.Count
                };

                response.Templates.AddRange(templates.Select(MapToProtoMaterialTemplate));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing material templates");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        public override async Task<OperationResponse> UpdateMaterialTemplate(
            UpdateMaterialTemplateRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Updating material template: {MaterialId}", request.MaterialId);

                Dictionary<string, object>? definition = null;
                if (!string.IsNullOrEmpty(request.DefinitionJson))
                {
                    var definitionRaw = JsonSerializer.Deserialize<Dictionary<string, object>>(request.DefinitionJson);
                    if (definitionRaw != null)
                    {
                        definition = JsonElementConverter.ConvertJsonElementsToPrimitives(definitionRaw);
                    }
                }

                var success = await _templateService.UpdateMaterialTemplateAsync(
                    materialId: request.MaterialId,
                    definition: definition,
                    name: string.IsNullOrEmpty(request.Name) ? null : request.Name,
                    description: string.IsNullOrEmpty(request.Description) ? null : request.Description,
                    tags: request.Tags.Count > 0 ? request.Tags.ToList() : null,
                    isPublished: request.HasIsPublished ? request.IsPublished : null);

                return new OperationResponse
                {
                    Success = success,
                    ErrorMessage = success ? "" : "Failed to update material template"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating material template");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        public override async Task<OperationResponse> DeleteMaterialTemplate(
            DeleteTemplateRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Deleting material template: {MaterialId}", request.Id);

                var success = await _templateService.DeleteMaterialTemplateAsync(request.Id);

                return new OperationResponse
                {
                    Success = success,
                    ErrorMessage = success ? "" : "Failed to delete material template"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting material template");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        // ============================================
        // Batch Operations
        // ============================================

        public override async Task<TemplatesBatchResponse> GetTemplatesByIds(
            GetTemplatesByIdsRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("Getting templates by IDs");

                var componentTemplates = await _templateService.GetComponentTemplatesAsync(
                    request.ComponentTemplateIds.ToList(), useCache: true);

                var materialTemplates = await _templateService.GetMaterialTemplatesAsync(
                    request.MaterialTemplateIds.ToList(), useCache: true);

                var response = new TemplatesBatchResponse
                {
                    Success = true
                };

                response.ComponentTemplates.AddRange(componentTemplates.Select(MapToProtoComponentTemplate));
                response.MaterialTemplates.AddRange(materialTemplates.Select(MapToProtoMaterialTemplate));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates by IDs");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        public override async Task<ValidationResponse> ValidateTemplateReferences(
            ValidateTemplateReferencesRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("Validating template references");

                var (isValid, missingTemplates) = await _templateService.ValidateTemplateReferencesAsync(
                    request.ComponentTemplateIds.ToList(),
                    request.MaterialTemplateIds.ToList());

                var response = new ValidationResponse
                {
                    IsValid = isValid
                };

                response.MissingTemplateIds.AddRange(missingTemplates);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template references");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        // ============================================
        // Utility Operations
        // ============================================

        public override async Task<ImportTemplatesResponse> ImportTemplatesFromOctahedron(
            ImportTemplatesRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Importing templates from octahedron data");

                var octahedronDataRaw = JsonSerializer.Deserialize<Dictionary<string, object>>(request.OctahedronJson)
                    ?? throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid octahedron JSON"));

                // Convert JsonElement objects to primitives for MongoDB compatibility
                var octahedronData = JsonElementConverter.ConvertJsonElementsToPrimitives(octahedronDataRaw);

                var (componentCount, materialCount) = await _templateService.ImportTemplatesFromOctahedronAsync(
                    octahedronData,
                    request.CreatedBy);

                return new ImportTemplatesResponse
                {
                    Success = true,
                    ComponentCount = componentCount,
                    MaterialCount = materialCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing templates from octahedron");
                return new ImportTemplatesResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ============================================
        // Helper Methods
        // ============================================

        private static ComponentTemplate MapToProtoComponentTemplate(Repository.Entities.ComponentTemplate template)
        {
            var proto = new ComponentTemplate
            {
                Id = template.Id ?? string.Empty,
                TemplateId = template.TemplateId,
                Type = template.Type,
                Version = template.Version,
                Name = template.Name ?? string.Empty,
                Description = template.Description ?? string.Empty,
                DefinitionJson = JsonSerializer.Serialize(template.Definition),
                IsPublished = template.IsPublished,
                CreatedAt = template.CreatedAt.ToString("O"),
                UpdatedAt = template.UpdatedAt.ToString("O"),
                UsageCount = template.UsageCount
            };

            if (template.Tags != null)
                proto.Tags.AddRange(template.Tags);

            if (!string.IsNullOrEmpty(template.ThumbnailUrl))
                proto.ThumbnailUrl = template.ThumbnailUrl;

            if (!string.IsNullOrEmpty(template.CreatedBy))
                proto.CreatedBy = template.CreatedBy;

            if (!string.IsNullOrEmpty(template.OrganizationId))
                proto.OrganizationId = template.OrganizationId;

            if (!string.IsNullOrEmpty(template.CdnUrl))
                proto.CdnUrl = template.CdnUrl;

            return proto;
        }

        private static MaterialTemplate MapToProtoMaterialTemplate(Repository.Entities.MaterialTemplate template)
        {
            var proto = new MaterialTemplate
            {
                Id = template.Id ?? string.Empty,
                MaterialId = template.MaterialId,
                Version = template.Version,
                Name = template.Name ?? string.Empty,
                Description = template.Description ?? string.Empty,
                DefinitionJson = JsonSerializer.Serialize(template.Definition),
                IsPublished = template.IsPublished,
                CreatedAt = template.CreatedAt.ToString("O"),
                UpdatedAt = template.UpdatedAt.ToString("O"),
                UsageCount = template.UsageCount
            };

            if (template.Tags != null)
                proto.Tags.AddRange(template.Tags);

            if (!string.IsNullOrEmpty(template.ThumbnailUrl))
                proto.ThumbnailUrl = template.ThumbnailUrl;

            if (!string.IsNullOrEmpty(template.CreatedBy))
                proto.CreatedBy = template.CreatedBy;

            if (!string.IsNullOrEmpty(template.OrganizationId))
                proto.OrganizationId = template.OrganizationId;

            if (!string.IsNullOrEmpty(template.CdnUrl))
                proto.CdnUrl = template.CdnUrl;

            return proto;
        }
    }
}
