using Emulator.API.Protos;
using Emulator.API.Utilities;
using Emulator.Repository.Models;
using Emulator.Service.Interfaces;
using Grpc.Core;
using Microsoft.AspNetCore.Authentication;
using MongoDB.Bson;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using static Google.Rpc.Context.AttributeContext.Types;

namespace Emulator.API.Services
{
    public class EmulatorGrpcService : Protos.EmulatorService.EmulatorServiceBase
    {
        private readonly IEmulationService _emulationService;
        private readonly ILogger<EmulatorGrpcService> _logger;

        public EmulatorGrpcService(
            IEmulationService emulationService,
            ILogger<EmulatorGrpcService> logger)
        {
            _emulationService = emulationService;
            _logger = logger;
        }

        public override async Task<EmulationResponse> CreateEmulation(
            Protos.CreateEmulationRequest request,
            ServerCallContext context)
        {
            try
            {


                var userId = ExtractUserIdOrThrow(context);

                // Handle empty or null definition_json
                if (string.IsNullOrEmpty(request.DefinitionJson))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "definition_json is required"));
                }

                // Use shared JsonSerializerOptions from JsonElementConverter utility
                var definition = JsonSerializer.Deserialize<Emulator.Repository.Entities.EmulationDefinition>(
                    request.DefinitionJson,
                    JsonElementConverter.DefaultOptions);

                if (definition == null)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid definition_json format"));
                }

                var createRequest = new Emulator.Repository.Models.CreateEmulationRequest
                {
                    Name = request.Name,
                    Description = request.Description,
                    Visibility = request.Visibility,
                    Definition = definition,
                    ThumbnailImageBase64 = request.ThumbnailImageBase64,
                    ThumbnailFileName = request.ThumbnailFileName
                };

                var result = await _emulationService.CreateEmulationAsync(createRequest, userId);

                if (!result.IsSucceeded)
                {
                    throw new RpcException(new Status(StatusCode.Internal, result.Message ?? "Failed to create emulation"));
                }

                return new EmulationResponse
                {
                    Success = true,
                    EmulationId = result.Data!.EmulationId,
                    Name = result.Data.Name,
                    Slug = result.Data.Slug,
                    Version = result.Data.Version,
                    Status = result.Data.Status,
                    Message = result.Message ?? "Emulation created successfully",
                    ThumbnailUrl = result.Data.ThumbnailUrl ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating emulation");
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<EmulationDetailResponse> GetEmulation(
            GetEmulationRequest request,
            ServerCallContext context)
        {
            try
            {
                var result = await _emulationService.GetEmulationByIdAsync(
                    request.EmulationId,
                    request.IncludeTemplates,
                    request.IncludeStatistics,
                    request.Version
                );

                if (!result.IsSucceeded || result.Data == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, result.Message ?? "Emulation not found"));
                }
                // Phase 4: Serialize definition to JSON with frontend-compatible Id mapping
                // Convert MongoDB _id to id for frontend compatibility
                string definitionObject;
                if (result.Data.Definition != null)
                {
                    definitionObject = result.Data.Definition.ToFrontendJson();
                }
                else
                {
                    definitionObject = "{}";
                }

                return new EmulationDetailResponse
                {
                    Success = true,
                    EmulationId = result.Data.EmulationId,
                    Name = result.Data.Name,
                    Slug = result.Data.Slug,
                    Version = result.Data.Version,
                    Status = result.Data.Status,
                    DefinitionJson = definitionObject,
                    ThumbnailUrl = result.Data.ThumbnailUrl ?? string.Empty,
                    CreatedAt = result.Data.CreatedAt.ToString("O"),
                    UpdatedAt = result.Data.UpdatedAt.ToString("O"),
                    Description = result.Data.Description,
                    UserId = result.Data.CreatedBy.UserId,
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emulation {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<PagedEmulationsResponse> ListEmulations(
            ListEmulationsRequest request,
            ServerCallContext context)
        {
            try
            {
                var filters = BuildPublishedListFilters(request, context);

                var result = await _emulationService.ListEmulationsAsync(filters);

                if (!result.IsSucceeded)
                {
                    throw new RpcException(new Status(StatusCode.Internal, result.Message ?? "Failed to list emulations"));
                }

                var response = new PagedEmulationsResponse
                {
                    Success = true,
                    Pagination = new Protos.PaginationInfo
                    {
                        Page = result.Data?.PageNumber ?? 1,
                        Limit = result.Data?.PageSize ?? 20,
                        Total = result.Data?.TotalCount ?? 0,
                        Pages = result.Data?.TotalPages ?? 0
                    }
                };

                foreach (var item in result.Data?.Items ?? new List<Emulator.Repository.Models.EmulationListDto>())
                {
                    response.Items.Add(new EmulationListItem
                    {
                        EmulationId = item.EmulationId,
                        Name = item.Name,
                        Slug = item.Slug,
                        Description = item.Description ?? "",
                        Difficulty = item.Difficulty,
                        Status = item.Status,
                        CreatedAt = item.CreatedAt.ToString("O"),
                        ThumbnailUrl = item.ThumbnailUrl ?? string.Empty,
                        UserId = item.CreatedBy.UserId,
                    });
                }

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing emulations");
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        private EmulationFilterParams BuildPublishedListFilters(ListEmulationsRequest request, ServerCallContext context)
        {
            // Default pagination
            var page = request.Page > 0 ? request.Page : 1;
            var limit = request.Limit > 0 ? request.Limit : 20;

            // Default sort if empty/whitespace
            var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "createdAt" : request.SortBy;
            var sortDir = request.SortOrder?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true
                ? Shared.Enums.SortDirection.Asc
                : Shared.Enums.SortDirection.Desc;

            var filters = new EmulationFilterParams
            {
                PageNumber = page,
                PageSize = limit,
                Search = request.Search,
                Difficulty = request.Difficulty,
                Status = request.Status,
                Visibility = request.Visibility,
                OrderBy = sortBy,
                SortDirection = sortDir,
                Tags = request.Tags.ToList(),
                CreatedByUserId = request.UserId
            };


            // var userId = ExtractUserIdOrThrow(context);
       
            // if (!string.IsNullOrWhiteSpace(userId))
            // {
            //     filters.CreatedByUserId = userId;
            // }

            return filters;
        }

        public override async Task<EmulationResponse> UpdateEmulation(
            Protos.UpdateEmulationRequest request,
            ServerCallContext context)
        {
            try
            {
                Emulator.Repository.Entities.EmulationDefinition? definition = null;
                if (!string.IsNullOrEmpty(request.DefinitionJson))
                {
                     definition = JsonSerializer.Deserialize<Emulator.Repository.Entities.EmulationDefinition>(
                    request.DefinitionJson,
                    JsonElementConverter.DefaultOptions);
                }

                var updateRequest = new Emulator.Repository.Models.UpdateEmulationRequest
                {
                    Name = request.Name,
                    Description = request.Description,
                    Definition = definition,
                    Status = request.Status
                };

                var result = await _emulationService.UpdateEmulationAsync(request.EmulationId, updateRequest);

                if (!result.IsSucceeded)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, result.Message ?? "Emulation not found"));
                }

                return new EmulationResponse
                {
                    Success = true,
                    EmulationId = result.Data!.EmulationId,
                    Name = result.Data.Name,
                    Slug = result.Data.Slug,
                    Version = result.Data.Version,
                    Status = result.Data.Status,
                    Message = result.Message ?? "Emulation updated successfully"
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating emulation {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<DeleteResponse> DeleteEmulation(
            DeleteEmulationRequest request,
            ServerCallContext context)
        {
            try
            {
                var result = await _emulationService.DeleteEmulationAsync(request.EmulationId, request.Permanent);

                if (!result.IsSucceeded)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, result.Message ?? "Emulation not found"));
                }

                return new DeleteResponse
                {
                    Success = true,
                    Message = result.Message ?? (request.Permanent ? "Emulation permanently deleted" : "Emulation soft deleted")
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting emulation {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<PublishResponse> PublishEmulation(
            PublishEmulationRequest request,
            ServerCallContext context)
        {
            try
            {
                var result = await _emulationService.PublishEmulationAsync(request.EmulationId);

                if (!result.IsSucceeded)
                {
                    throw new RpcException(new Status(StatusCode.FailedPrecondition, result.Message ?? "Failed to publish emulation"));
                }

                return new PublishResponse
                {
                    Success = true,
                    EmulationId = request.EmulationId,
                    Status = "published",
                    PublishedAt = DateTime.UtcNow.ToString("O")
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing emulation {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<ValidationResultResponse> ValidateEmulation(
            ValidateEmulationRequest request,
            ServerCallContext context)
        {
            try
            {
                var result = await _emulationService.ValidateEmulationAsync(request.EmulationId);

                if (!result.IsSucceeded)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, result.Message ?? "Emulation not found"));
                }

                var response = new ValidationResultResponse
                {
                    Success = true,
                    Valid = result.Data!.Valid
                };

                foreach (var error in result.Data.Errors)
                {
                    response.Errors.Add(new Protos.ValidationError
                    {
                        Type = error.Type,
                        Message = error.Message,
                        Location = error.Location ?? "",
                        ComponentId = error.ComponentId ?? ""
                    });
                }

                foreach (var warning in result.Data.Warnings)
                {
                    response.Warnings.Add(new Protos.ValidationError
                    {
                        Type = warning.Type,
                        Message = warning.Message,
                        Location = warning.Location ?? "",
                        ComponentId = warning.ComponentId ?? ""
                    });
                }

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating emulation {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<EmulationResponse> DuplicateEmulation(
            DuplicateEmulationRequest request,
            ServerCallContext context)
        {
            try
            {
                var userId = ExtractUserIdOrThrow(context);

                var result = await _emulationService.DuplicateEmulationAsync(request.EmulationId, userId);

                if (!result.IsSucceeded)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, result.Message ?? "Emulation not found"));
                }

                return new EmulationResponse
                {
                    Success = true,
                    EmulationId = result.Data!.EmulationId,
                    Name = result.Data.Name,
                    Slug = result.Data.Slug,
                    Version = result.Data.Version,
                    Status = result.Data.Status,
                    Message = result.Message ?? "Emulation duplicated successfully"
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error duplicating emulation {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<EmulationResponse> CreateEmulationDraft(
            Protos.CreateEmulationDraftRequest request,
            ServerCallContext context)
        {
            try
            {
                var userId = ExtractUserIdOrThrow(context);
                // Build minimal definition placeholder
                var definition = new Emulator.Repository.Entities.EmulationDefinition
                {
                    Metadata = new Emulator.Repository.Entities.Metadata
                    {
                        Version = "1.0",
                        Author = userId
                    }
                };

                var createRequest = new Emulator.Repository.Models.CreateEmulationRequest
                {
                    Name = request.Name,
                    Description = request.Description,
                    Visibility = request.Visibility,
                    Definition = definition,
                    ThumbnailImageBase64 = request.ThumbnailImageBase64,
                    ThumbnailFileName = request.ThumbnailFileName
                };

                var result = await _emulationService.CreateEmulationAsync(createRequest, userId);

                if (!result.IsSucceeded)
                {
                    throw new RpcException(new Status(StatusCode.Internal, result.Message ?? "Failed to create emulation draft"));
                }

                return new EmulationResponse
                {
                    Success = true,
                    EmulationId = result.Data!.EmulationId,
                    Name = result.Data.Name,
                    Slug = result.Data.Slug,
                    Version = result.Data.Version,
                    Status = result.Data.Status,
                    Message = result.Message ?? "Emulation draft created successfully",
                    ThumbnailUrl = result.Data.ThumbnailUrl ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating emulation draft");
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        private string ExtractUserIdOrThrow(ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();

            // Try principal claims
            var principal = httpContext.User;
            var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? principal?.FindFirst("sub")?.Value
                         ?? httpContext.Request.Headers["X-User-Id"].FirstOrDefault();

            // Fallback: parse Authorization header (no validation)
            if (string.IsNullOrWhiteSpace(userId))
            {
                var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(authHeader))
                {
                    var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? authHeader.Substring("Bearer ".Length)
                        : authHeader;
                    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                    userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                          ?? jwt.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
                }
            }

            if (string.IsNullOrWhiteSpace(userId))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing user identity"));

            return userId;
        }
    }
}
