using Grpc.Core;
using Identity.Application.Common.Interfaces.Grpc;
using Identity.Application.Dtos.Grpc;
using Identity.Domain.Exceptions;
using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Shared.Protos.Order;

namespace Identity.Infrastructure.Grpc;

public class OrderLicenseService : IOrderLicenseService
{
    private readonly GrpcLicenseAssignmentService.GrpcLicenseAssignmentServiceClient _licenseClient;
    private readonly GrpcOrganizationService.GrpcOrganizationServiceClient _organizationClient;
    private readonly IPollyResilienceService _resilienceService;
    private readonly ILogger<OrderLicenseService> _logger;

    private const string LicensePolicyName = "OrderService.License";
    private const string OrganizationPolicyName = "OrderService.Organization";

    public OrderLicenseService(
        GrpcLicenseAssignmentService.GrpcLicenseAssignmentServiceClient licenseClient,
        GrpcOrganizationService.GrpcOrganizationServiceClient organizationClient,
        IPollyResilienceService resilienceService,
        ILogger<OrderLicenseService> logger)
    {
        _licenseClient = licenseClient;
        _organizationClient = organizationClient;
        _resilienceService = resilienceService;
        _logger = logger;
    }

    public async Task<LicenseAvailabilityDto> CheckLicenseAvailabilityAsync(
        int organizationId,
        string licenseType,
        int requestedCount,
        CancellationToken cancellationToken = default)
    {
        return await _resilienceService.ExecuteAsync(async ct =>
        {
            try
            {
                _logger.LogInformation(
                    "Checking license availability for organization {OrganizationId}, type {LicenseType}, count {RequestedCount}",
                    organizationId, licenseType, requestedCount);

                var request = new CheckLicenseAvailabilityRequest
                {
                    OrganizationId = organizationId,
                    LicenseType = licenseType,
                    RequestedCount = requestedCount
                };

                var response = await _licenseClient.CheckLicenseAvailabilityAsync(
                    request,
                    cancellationToken: ct);

                return new LicenseAvailabilityDto
                {
                    Available = response.Available,
                    AvailableCount = response.AvailableCount,
                    TotalLicenses = response.TotalLicenses,
                    UsedLicenses = response.UsedLicenses,
                    Message = response.Message
                };
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Organization {OrganizationId} not found", organizationId);
                throw new OrganizationNotFoundException(organizationId);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error checking license availability for organization {OrganizationId}", organizationId);
                throw new InvalidOperationException($"Failed to check license availability: {ex.Status.Detail}", ex);
            }
        }, LicensePolicyName, cancellationToken);
    }

    public async Task<BulkLicenseCheckDto> BulkCheckLicensesAsync(
        int organizationId,
        Dictionary<string, int> licenseRequests,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Bulk checking licenses for organization {OrganizationId}, {Count} types",
                organizationId, licenseRequests.Count);

            var request = new BulkCheckLicensesRequest
            {
                OrganizationId = organizationId
            };

            foreach (var (licenseType, count) in licenseRequests)
            {
                request.LicenseRequests.Add(new LicenseCheckRequest
                {
                    LicenseType = licenseType,
                    Count = count
                });
            }

            var response = await _licenseClient.BulkCheckLicensesAsync(
                request,
                cancellationToken: cancellationToken);

            var dto = new BulkLicenseCheckDto
            {
                AllAvailable = response.AllAvailable,
                Message = response.Message
            };

            foreach (var result in response.Results)
            {
                dto.Results[result.LicenseType] = new LicenseCheckResultDto
                {
                    LicenseType = result.LicenseType,
                    Available = result.Available,
                    AvailableCount = result.AvailableCount,
                    RequestedCount = result.RequestedCount
                };
            }

            return dto;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Organization {OrganizationId} not found", organizationId);
            throw new OrganizationNotFoundException(organizationId);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error bulk checking licenses for organization {OrganizationId}", organizationId);
            throw new InvalidOperationException($"Failed to bulk check licenses: {ex.Status.Detail}", ex);
        }
    }

    public async Task<LicenseAssignmentResultDto> AssignLicenseAsync(
        int organizationId,
        string userEmail,
        string licenseType,
        int subscriptionOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Assigning {LicenseType} license to {UserEmail} in organization {OrganizationId}",
                licenseType, userEmail, organizationId);

            var request = new AssignLicenseByEmailRequest
            {
                OrganizationId = organizationId,
                UserEmail = userEmail,
                LicenseType = licenseType,
                SubscriptionOrderId = subscriptionOrderId
            };

            var response = await _licenseClient.AssignLicenseByEmailAsync(
                request,
                cancellationToken: cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation(
                    "Successfully assigned license {LicenseAssignmentId} to {UserEmail}",
                    response.LicenseAssignmentId, userEmail);

                return LicenseAssignmentResultDto.CreateSuccess(response.LicenseAssignmentId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to assign license to {UserEmail}: {ErrorMessage}",
                    userEmail, response.ErrorMessage);

                return LicenseAssignmentResultDto.CreateFailure(response.ErrorMessage);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Organization {OrganizationId} or subscription not found", organizationId);
            return LicenseAssignmentResultDto.CreateFailure($"Organization or subscription not found");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.ResourceExhausted)
        {
            _logger.LogWarning(ex, "No licenses available for {LicenseType} in organization {OrganizationId}",
                licenseType, organizationId);
            return LicenseAssignmentResultDto.CreateFailure($"No {licenseType} licenses available");
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error assigning license to {UserEmail}", userEmail);
            return LicenseAssignmentResultDto.CreateFailure($"Failed to assign license: {ex.Status.Detail}");
        }
    }

    public async Task<OrganizationDto> GetOrganizationAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting organization {OrganizationId}", organizationId);

            var request = new GetOrganizationRequest { Id = organizationId };

            var response = await _organizationClient.GetOrganizationByIdAsync(
                request,
                cancellationToken: cancellationToken);

            return new OrganizationDto
            {
                Id = response.Id,
                Name = response.Name,
                Status = response.Status,
                Description = response.Description,
                ImageUrl = response.ImageUrl,
                CreatedDate = response.CreatedDate?.ToDateTime() ?? DateTime.MinValue,
                Code = response.Code
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Organization {OrganizationId} not found", organizationId);
            throw new OrganizationNotFoundException(organizationId);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error getting organization {OrganizationId}", organizationId);
            throw new InvalidOperationException($"Failed to get organization: {ex.Status.Detail}", ex);
        }
    }

    public async Task<OrganizationBulkProvisioningDto> GetOrganizationForBulkProvisioningAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting organization {OrganizationId} for bulk provisioning", organizationId);

            var request = new GetOrganizationRequest { Id = organizationId };

            var response = await _organizationClient.GetOrganizationForBulkProvisioningAsync(
                request,
                cancellationToken: cancellationToken);

            var dto = new OrganizationBulkProvisioningDto
            {
                Id = response.Id,
                Name = response.Name,
                EmailDomain = response.EmailDomain,
                Status = response.Status,
                IsActive = response.IsActive
            };

            foreach (var sub in response.Subscriptions)
            {
                dto.Subscriptions.Add(new SubscriptionLicenseInfoDto
                {
                    SubscriptionOrderId = sub.SubscriptionOrderId,
                    PlanName = sub.PlanName,
                    Status = sub.Status,
                    StartDate = sub.StartDate?.ToDateTime(),
                    MaxStudentSeats = sub.MaxStudentSeats,
                    MaxTeacherSeats = sub.MaxTeacherSeats,
                    MaxOrganizationAdminSeats = sub.MaxOrganizationAdminSeats,
                    CurrentStudentSeats = sub.CurrentStudentSeats,
                    CurrentTeacherSeats = sub.CurrentTeacherSeats,
                    CurrentOrganizationAdminSeats = sub.CurrentOrganizationAdminSeats,
                    AvailableStudentSeats = sub.AvailableStudentSeats,
                    AvailableTeacherSeats = sub.AvailableTeacherSeats,
                    AvailableOrganizationAdminSeats = sub.AvailableOrganizationAdminSeats
                });
            }

            return dto;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Organization {OrganizationId} not found", organizationId);
            throw new OrganizationNotFoundException(organizationId);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error getting organization for bulk provisioning {OrganizationId}", organizationId);
            throw new InvalidOperationException($"Failed to get organization for bulk provisioning: {ex.Status.Detail}", ex);
        }
    }

    public async Task<LicenseAssignmentResultDto> ReserveLicenseAsync(
        int organizationId,
        string userId,
        string licenseType,
        int subscriptionOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Reserving {LicenseType} license for UserId {UserId} in organization {OrganizationId}, subscription {SubscriptionOrderId}",
                licenseType, userId, organizationId, subscriptionOrderId);

            var request = new ReserveLicenseByEmailRequest
            {
                OrganizationId = organizationId,
                OrganizationUserId = userId,
                LicenseType = licenseType,
                SubscriptionOrderId = subscriptionOrderId
            };

            var response = await _licenseClient.ReserveLicenseByEmailAsync(
                request,
                cancellationToken: cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation(
                    "Successfully reserved license {LicenseAssignmentId} (Pending) for UserId {UserId}",
                    response.LicenseAssignmentId, userId);

                return LicenseAssignmentResultDto.CreateSuccess(response.LicenseAssignmentId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to reserve license for UserId {UserId}: {ErrorMessage}",
                    userId, response.ErrorMessage);

                return LicenseAssignmentResultDto.CreateFailure(response.ErrorMessage);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.ResourceExhausted)
        {
            _logger.LogWarning(ex, "No licenses available for {LicenseType} in organization {OrganizationId}",
                licenseType, organizationId);
            return LicenseAssignmentResultDto.CreateFailure($"No {licenseType} licenses available");
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error reserving license for UserId {UserId}", userId);
            return LicenseAssignmentResultDto.CreateFailure($"Failed to reserve license: {ex.Status.Detail}");
        }
    }

    public async Task<LicenseAssignmentResultDto> ActivateReservedLicenseAsync(
        int organizationId,
        string organizationUserId,
        string licenseType,
        int subscriptionOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Activating reserved {LicenseType} license for {UserEmail} in organization {OrganizationId}, subscription {SubscriptionOrderId}",
                licenseType, organizationUserId, organizationId, subscriptionOrderId);

            var request = new ActivateReservedLicenseRequest
            {
                OrganizationId = organizationId,
                OrganizationUserId = organizationUserId,
                LicenseType = licenseType,
                SubscriptionOrderId = subscriptionOrderId
            };

            var response = await _licenseClient.ActivateReservedLicenseAsync(
                request,
                cancellationToken: cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation(
                    "Successfully activated reserved license {LicenseAssignmentId} for {organizationUserId}",
                    response.LicenseAssignmentId, organizationUserId);

                return LicenseAssignmentResultDto.CreateSuccess(response.LicenseAssignmentId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to activate reserved license for {organizationUserId}: {ErrorMessage}",
                    organizationUserId, response.ErrorMessage);

                return LicenseAssignmentResultDto.CreateFailure(response.ErrorMessage);
            }
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error activating reserved license for {organizationUserId}", organizationUserId);
            return LicenseAssignmentResultDto.CreateFailure($"Failed to activate reserved license: {ex.Status.Detail}");
        }
    }
}
