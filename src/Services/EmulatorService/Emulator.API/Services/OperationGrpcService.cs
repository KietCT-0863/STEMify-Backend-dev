using Emulator.Repository.Entities;
using Emulator.Service.Interfaces;
using Grpc.Core;
using System.Text.Json;

namespace Emulator.API.Services
{
    public class OperationGrpcService : Protos.OperationService.OperationServiceBase
    {
        private readonly IOperationService _operationService;
        private readonly ILogger<OperationGrpcService> _logger;

        public OperationGrpcService(
            IOperationService operationService,
            ILogger<OperationGrpcService> logger)
        {
            _operationService = operationService;
            _logger = logger;
        }

        /// <summary>
        /// Append single operation to emulation event log
        /// Implements atomic sequence generation and conflict detection
        /// </summary>
        public override async Task<Protos.AppendOperationResponse> AppendOperation(
            Protos.AppendOperationRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("AppendOperation called for emulation: {EmulationId}, op: {Op}, path: {Path}",
                    request.EmulationId, request.Op, request.Path);

                var serviceRequest = new AppendOperationRequest
                {
                    EmulationId = request.EmulationId,
                    Op = request.Op,
                    Path = request.Path,
                    Value = ConvertProtobufValueToObject(request.Value),
                    From = request.From,
                    UserId = request.UserId,
                    Metadata = MapMetadata(request.Metadata),
                    ClientLastSeq = request.ClientLastSeq
                };

                var result = await _operationService.AppendOperationAsync(serviceRequest);

                if (!result.IsSucceeded)
                {
                    _logger.LogWarning("Failed to append operation: {Message}", result.Message);

                    return new Emulator.API.Protos.AppendOperationResponse
                    {
                        Success = false,
                        Message = result.Message ?? "Failed to append operation",
                        Validation = new Protos.ValidationResult
                        {
                            IsValid = false,
                            Errors = { result.Message ?? "Unknown error" }
                        }
                    };
                }

                var response = new Emulator.API.Protos.AppendOperationResponse
                {
                    Success = true,
                    Message = result.Message ?? "Operation appended successfully",
                    Operation = MapOperationToProto(result.Data!)
                };

                // Note: CustomFields not available in current OperationMetadata structure
                // This would need to be implemented if validation results need to be passed through metadata

                _logger.LogInformation("Operation appended successfully: {EmulationId}, seq: {Seq}",
                    result.Data!.EmulationId, result.Data.Seq);

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error appending operation for emulation: {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Execute batch of operations atomically
        /// All operations succeed or all fail together
        /// </summary>
        public override async Task<Protos.BatchOperationResponse> ExecuteBatchOperation(
            Protos.ExecuteBatchRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("ExecuteBatchOperation called for emulation: {EmulationId}, count: {Count}",
                    request.EmulationId, request.Operations.Count);

                var operations = request.Operations.Select(op => new OperationDto
                {
                    Op = op.Op,
                    Path = op.Path,
                    Value = ConvertProtobufValueToObject(op.Value),
                    From = op.From,
                    Metadata = MapMetadata(op.Metadata)
                }).ToList();

                var serviceRequest = new Emulator.Service.Interfaces.AppendBatchRequest
                {
                    EmulationId = request.EmulationId,
                    Operations = operations,
                    UserId = request.UserId,
                    BatchId = request.BatchId,
                    ClientLastSeq = request.ClientLastSeq
                };

                var result = await _operationService.AppendBatchOperationsAsync(serviceRequest);

                if (!result.IsSucceeded)
                {
                    _logger.LogWarning("Failed to execute batch operation: {Message}", result.Message);

                    return new Protos.BatchOperationResponse
                    {
                        Success = false,
                        Message = result.Message ?? "Failed to execute batch operation"
                    };
                }

                var response = new Protos.BatchOperationResponse
                {
                    Success = true,
                    Message = result.Message ?? "Batch executed successfully",
                    TotalOperations = result.Data!.Count,
                    Operations = { result.Data.Select(MapOperationToProto) }
                };

                if (result.Data.Any())
                {
                    response.StartSeq = result.Data.Min(op => op.Seq);
                    response.EndSeq = result.Data.Max(op => op.Seq);
                }

                _logger.LogInformation("Batch operation executed successfully: {EmulationId}, operations: {Count}, seqs: {StartSeq}-{EndSeq}",
                    request.EmulationId, result.Data.Count, response.StartSeq, response.EndSeq);

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing batch operation for emulation: {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get operation history for delta sync
        /// Returns operations since client's last known sequence number
        /// </summary>
        public override async Task<Protos.OperationHistoryResponse> GetOperationHistory(
            Protos.GetOperationHistoryRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("GetOperationHistory called for emulation: {EmulationId}, sinceSeq: {SinceSeq}",
                    request.EmulationId, request.SinceSeq);

                var operations = await _operationService.GetOperationsSinceAsync(
                    request.EmulationId,
                    request.SinceSeq
                );

                // Apply limit if specified
                var limitedOperations = operations.Data ?? new List<EmulationOperation>();
                if (request.Limit > 0 && limitedOperations.Count > request.Limit)
                {
                    limitedOperations = limitedOperations.Take(request.Limit).ToList();
                }

                // Get latest seq
                var latestSeq = await _operationService.GetCurrentSequenceAsync(request.EmulationId);

                var response = new Protos.OperationHistoryResponse
                {
                    Success = true,
                    EmulationId = request.EmulationId,
                    Operations = { limitedOperations.Select(MapOperationToProto) },
                    LatestSeq = latestSeq.Data,
                    TotalCount = limitedOperations.Count,
                    HasMore = operations.Data != null && operations.Data.Count > limitedOperations.Count
                };

                _logger.LogDebug("Returning {Count} operations for emulation: {EmulationId}",
                    limitedOperations.Count, request.EmulationId);

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operation history for emulation: {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get operation statistics for monitoring
        /// Used by dashboard and metrics endpoints
        /// </summary>
        public override async Task<Protos.StatisticsResponse> GetOperationStatistics(
            Protos.GetStatisticsRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("GetOperationStatistics called for emulation: {EmulationId}", request.EmulationId);

                var result = await _operationService.GetOperationStatisticsAsync(request.EmulationId);

                if (!result.IsSucceeded || result.Data == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, result.Message ?? "Statistics not found"));
                }

                var stats = result.Data;

                var response = new Protos.StatisticsResponse
                {
                    Success = true,
                    Statistics = new Protos.OperationStatistics
                    {
                        TotalOperations = stats.TotalOperations,
                        PendingOperations = stats.PendingOperations,
                        AppliedOperations = stats.AppliedOperations,
                        DeletedOperations = stats.DeletedOperations,
                        LastSeq = stats.LastSeq,
                        LastSnapshotSeq = stats.LastSnapshotSeq,
                        OperationsSinceSnapshot = stats.OperationsSinceSnapshot,
                        LastOperationAt = stats.LastOperationAt?.ToString("O") ?? "",
                        LastSnapshotAt = stats.LastSnapshotAt?.ToString("O") ?? "",
                        AverageOperationsPerDay = stats.AverageOperationsPerDay
                    }
                };

                // Map dictionaries
                foreach (var kvp in stats.OperationsByType)
                {
                    response.Statistics.OperationsByType.Add(kvp.Key, kvp.Value);
                }

                foreach (var kvp in stats.OperationsByUser)
                {
                    response.Statistics.OperationsByUser.Add(kvp.Key, kvp.Value);
                }

                _logger.LogDebug("Statistics retrieved for emulation: {EmulationId}, {Summary}",
                    request.EmulationId, stats.GetSummary());

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for emulation: {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Detect conflicts before appending operation
        /// Implements Last-Write-Wins strategy
        /// </summary>
        public override async Task<Protos.ConflictDetectionResponse> DetectConflicts(
            Protos.DetectConflictsRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("DetectConflicts called for emulation: {EmulationId}, path: {Path}",
                    request.EmulationId, request.Path);

                var operation = new EmulationOperation
                {
                    EmulationId = request.EmulationId,
                    Op = request.Op,
                    Path = request.Path,
                    Value = ConvertProtobufValueToObject(request.Value)
                };

                var conflictResult = await _operationService.DetectConflictsAsync(
                    request.EmulationId,
                    operation,
                    request.ClientLastSeq
                );

                // Get current server sequence
                var currentSeqResult = await _operationService.GetCurrentSequenceAsync(request.EmulationId);
                var serverSeq = currentSeqResult.Data;

                var response = new Protos.ConflictDetectionResponse
                {
                    HasConflict = conflictResult.HasConflict,
                    ConflictType = conflictResult.ConflictType.ToString(),
                    ResolutionStrategy = conflictResult.SuggestedResolution.ToString(),
                    ServerSeq = serverSeq,
                    ClientSeq = request.ClientLastSeq
                };

                if (conflictResult.ConflictingOperations != null && conflictResult.ConflictingOperations.Any())
                {
                    response.ConflictingOperations.AddRange(
                        conflictResult.ConflictingOperations.Select(MapOperationToProto)
                    );
                }

                _logger.LogDebug("Conflict detection result: {HasConflict}, type: {ConflictType}",
                    conflictResult.HasConflict, conflictResult.ConflictType);

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting conflicts for emulation: {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get operations since a specific sequence number
        /// Phase 4 CQRS: Optimized query for client sync
        /// </summary>
        public override async Task<Protos.OperationHistoryResponse> GetOperationsSince(
            Protos.GetOperationsSinceRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("GetOperationsSince called for emulation: {EmulationId}, sinceSeq: {SinceSeq}, limit: {Limit}",
                    request.EmulationId, request.SinceSeq, request.Limit);

                // Use existing service method
                var result = await _operationService.GetOperationsSinceAsync(
                    request.EmulationId,
                    request.SinceSeq
                );

                if (!result.IsSucceeded)
                {
                    _logger.LogWarning("Failed to get operations: {Message}", result.Message);
                    return new Protos.OperationHistoryResponse
                    {
                        Success = false,
                        EmulationId = request.EmulationId,
                        LatestSeq = request.SinceSeq
                    };
                }

                // Apply limit if specified
                var operations = result.Data ?? new List<EmulationOperation>();
                var limit = request.Limit > 0 ? request.Limit : 100;  // Default 100
                var limitedOperations = operations.Take(limit).ToList();

                // Get current sequence
                var currentSeqResult = await _operationService.GetCurrentSequenceAsync(request.EmulationId);

                var response = new Protos.OperationHistoryResponse
                {
                    Success = true,
                    EmulationId = request.EmulationId,
                    Operations = { limitedOperations.Select(MapOperationToProto) },
                    LatestSeq = currentSeqResult.Data,
                    TotalCount = limitedOperations.Count,
                    HasMore = operations.Count > limitedOperations.Count
                };

                _logger.LogDebug("Returning {Count} operations (hasMore: {HasMore}) for emulation: {EmulationId}",
                    limitedOperations.Count, response.HasMore, request.EmulationId);

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operations since seq for emulation: {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get latest sequence number for emulation
        /// Used for sync status checking
        /// </summary>
        public override async Task<Protos.LatestSeqResponse> GetLatestSeq(
            Protos.GetLatestSeqRequest request,
            ServerCallContext context)
        {
            try
            {
                _logger.LogDebug("GetLatestSeq called for emulation: {EmulationId}", request.EmulationId);

                var result = await _operationService.GetCurrentSequenceAsync(request.EmulationId);

                return new Protos.LatestSeqResponse
                {
                    Success = true,
                    LatestSeq = result.Data,
                    EmulationId = request.EmulationId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest seq for emulation: {EmulationId}", request.EmulationId);
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        // ============================================================================
        // Helper Methods - Mapping between Proto and Domain Models
        // ============================================================================

        /// <summary>
        /// Map EmulationOperation entity to gRPC Protos.OperationData message
        /// </summary>
        private static Protos.OperationData MapOperationToProto(EmulationOperation operation)
        {
            var protoOp = new Protos.OperationData
            {
                OperationId = operation.Id,
                EmulationId = operation.EmulationId,
                Seq = operation.Seq,
                Op = operation.Op,
                Path = operation.Path,
                From = operation.From ?? "",
                Timestamp = operation.Timestamp.ToString("O"),
                UserId = operation.UserId,
                AppliedToSnapshot = operation.AppliedToSnapshot,
                SnapshotSeq = operation.SnapshotSeq ?? 0,
                IsDeleted = operation.IsDeleted
            };

            // Convert Value to protobuf Value
            if (operation.Value != null)
            {
                protoOp.Value = ConvertObjectToProtobufValue(operation.Value);
            }

            // Convert OldValue to protobuf Value
            if (operation.OldValue != null)
            {
                protoOp.OldValue = ConvertObjectToProtobufValue(operation.OldValue);
            }

            // Map metadata
            if (operation.Metadata != null)
            {
                protoOp.Metadata = new Protos.OperationMetadata
                {
                    Source = operation.Metadata.ActionType ?? "",
                    SessionId = operation.Metadata.SessionId ?? "",
                    BatchId = operation.Metadata.BatchId ?? "",
                    ComponentId = operation.Metadata.Tool ?? "",
                    ComponentType = operation.Metadata.DeviceInfo ?? ""
                };

                // Add additional fields as custom fields
                if (operation.Metadata.Tool != null)
                    protoOp.Metadata.CustomFields.Add("tool", operation.Metadata.Tool);
                if (operation.Metadata.DeviceInfo != null)
                    protoOp.Metadata.CustomFields.Add("deviceInfo", operation.Metadata.DeviceInfo);
                if (operation.Metadata.ClientTimestamp != null)
                    protoOp.Metadata.CustomFields.Add("clientTimestamp", operation.Metadata.ClientTimestamp.Value.ToString("O"));
            }

            return protoOp;
        }

        /// <summary>
        /// Map gRPC OperationMetadata to domain OperationMetadata
        /// </summary>
        private static Repository.Entities.OperationMetadata? MapMetadata(Protos.OperationMetadata? protoMetadata)
        {
            if (protoMetadata == null)
                return null;

            return new Repository.Entities.OperationMetadata
            {
                ActionType = protoMetadata.Source,
                SessionId = protoMetadata.SessionId,
                BatchId = protoMetadata.BatchId,
                Tool = protoMetadata.ComponentId,
                DeviceInfo = protoMetadata.ComponentType,
                ClientTimestamp = protoMetadata.CustomFields?.ContainsKey("clientTimestamp") == true &&
                                 DateTime.TryParse(protoMetadata.CustomFields["clientTimestamp"], out var clientTs) ?
                                 clientTs : null
            };
        }

        /// <summary>
        /// Convert protobuf Value to C# object
        /// Handles all JSON types: null, number, string, bool, array, object
        /// </summary>
        private static object? ConvertProtobufValueToObject(Google.Protobuf.WellKnownTypes.Value? value)
        {
            if (value == null)
                return null;

            switch (value.KindCase)
            {
                case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.NullValue:
                    return null;

                case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.NumberValue:
                    return value.NumberValue;

                case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.StringValue:
                    return value.StringValue;

                case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.BoolValue:
                    return value.BoolValue;

                case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.StructValue:
                    var dict = new Dictionary<string, object?>();
                    foreach (var field in value.StructValue.Fields)
                    {
                        dict[field.Key] = ConvertProtobufValueToObject(field.Value);
                    }
                    return dict;

                case Google.Protobuf.WellKnownTypes.Value.KindOneofCase.ListValue:
                    return value.ListValue.Values.Select(ConvertProtobufValueToObject).ToList();

                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert C# object to protobuf Value
        /// Handles all JSON types: null, number, string, bool, array, object
        /// </summary>
        private static Google.Protobuf.WellKnownTypes.Value ConvertObjectToProtobufValue(object? obj)
        {
            if (obj == null)
            {
                return Google.Protobuf.WellKnownTypes.Value.ForNull();
            }

            switch (obj)
            {
                case string s:
                    return Google.Protobuf.WellKnownTypes.Value.ForString(s);

                case bool b:
                    return Google.Protobuf.WellKnownTypes.Value.ForBool(b);

                case int i:
                    return Google.Protobuf.WellKnownTypes.Value.ForNumber(i);

                case long l:
                    return Google.Protobuf.WellKnownTypes.Value.ForNumber(l);

                case double d:
                    return Google.Protobuf.WellKnownTypes.Value.ForNumber(d);

                case float f:
                    return Google.Protobuf.WellKnownTypes.Value.ForNumber(f);

                case decimal dec:
                    return Google.Protobuf.WellKnownTypes.Value.ForNumber((double)dec);

                case Dictionary<string, object?> dict:
                    var structValue = new Google.Protobuf.WellKnownTypes.Struct();
                    foreach (var kvp in dict)
                    {
                        structValue.Fields[kvp.Key] = ConvertObjectToProtobufValue(kvp.Value);
                    }
                    return Google.Protobuf.WellKnownTypes.Value.ForStruct(structValue);

                case System.Collections.IEnumerable enumerable when obj is not string:
                    var listValue = new Google.Protobuf.WellKnownTypes.ListValue();
                    foreach (var item in enumerable)
                    {
                        listValue.Values.Add(ConvertObjectToProtobufValue(item));
                    }
                    return new Google.Protobuf.WellKnownTypes.Value { ListValue = listValue };

                default:
                    // Try to serialize to JSON and convert
                    var json = JsonSerializer.Serialize(obj);
                    var element = JsonSerializer.Deserialize<JsonElement>(json);
                    return ConvertJsonElementToProtobufValue(element);
            }
        }

        /// <summary>
        /// Convert JsonElement to protobuf Value
        /// Helper for complex object serialization
        /// </summary>
        private static Google.Protobuf.WellKnownTypes.Value ConvertJsonElementToProtobufValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    return Google.Protobuf.WellKnownTypes.Value.ForNull();

                case JsonValueKind.String:
                    return Google.Protobuf.WellKnownTypes.Value.ForString(element.GetString() ?? "");

                case JsonValueKind.Number:
                    return Google.Protobuf.WellKnownTypes.Value.ForNumber(element.GetDouble());

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return Google.Protobuf.WellKnownTypes.Value.ForBool(element.GetBoolean());

                case JsonValueKind.Object:
                    var structValue = new Google.Protobuf.WellKnownTypes.Struct();
                    foreach (var property in element.EnumerateObject())
                    {
                        structValue.Fields[property.Name] = ConvertJsonElementToProtobufValue(property.Value);
                    }
                    return Google.Protobuf.WellKnownTypes.Value.ForStruct(structValue);

                case JsonValueKind.Array:
                    var listValue = new Google.Protobuf.WellKnownTypes.ListValue();
                    foreach (var item in element.EnumerateArray())
                    {
                        listValue.Values.Add(ConvertJsonElementToProtobufValue(item));
                    }
                    return new Google.Protobuf.WellKnownTypes.Value { ListValue = listValue };

                default:
                    return Google.Protobuf.WellKnownTypes.Value.ForNull();
            }
        }
    }
}
