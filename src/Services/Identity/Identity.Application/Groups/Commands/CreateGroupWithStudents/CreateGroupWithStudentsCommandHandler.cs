using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Grpc;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Groups.Dtos;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Groups.Commands.CreateGroupWithStudents;

public class CreateGroupWithStudentsCommandHandler : IRequestHandler<CreateGroupWithStudentsCommand, GroupDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IOrganizationUserLicenseReadRepository _licenseReadRepository;
    private readonly IOrderLicenseService _orderLicenseService;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<CreateGroupWithStudentsCommandHandler> _logger;

    public CreateGroupWithStudentsCommandHandler(
        IGroupRepository groupRepository,
        IOrganizationUserRepository organizationUserRepository,
        IOrganizationUserLicenseReadRepository licenseReadRepository,
        IOrderLicenseService orderLicenseService,
        IIdentityUnitOfWork unitOfWork,
        ILogger<CreateGroupWithStudentsCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _organizationUserRepository = organizationUserRepository;
        _licenseReadRepository = licenseReadRepository;
        _orderLicenseService = orderLicenseService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupDto> Handle(
        CreateGroupWithStudentsCommand request,
        CancellationToken cancellationToken)
    {

        string? organizationCode = null;
        string? fullGroupCode = null;
        
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var organizationDetails = await _orderLicenseService.GetOrganizationAsync(
                request.OrganizationId,
                cancellationToken);
            organizationCode = organizationDetails?.Code;
            
          
            fullGroupCode = GroupCodeBuilder.BuildFullGroupCode(organizationCode, request.Code, request.OrganizationId);
            
            var isCodeUnique = await _groupRepository.IsCodeUniqueInOrganizationAsync(
                request.OrganizationId,
                fullGroupCode,
                cancellationToken: cancellationToken);

            if (!isCodeUnique)
                throw new InvalidOperationException($"Group code '{fullGroupCode}' already exists in this organization");
        }

        int subscriptionOrderId;
        if (request.SubscriptionOrderId.HasValue && request.SubscriptionOrderId.Value > 0)
        {
            subscriptionOrderId = request.SubscriptionOrderId.Value;
            _logger.LogDebug(
                "Using provided subscriptionOrderId {SubscriptionOrderId} for organization {OrganizationId}",
                subscriptionOrderId,
                request.OrganizationId);
        }
        else
        {
            var organization = await _orderLicenseService.GetOrganizationForBulkProvisioningAsync(
                request.OrganizationId,
                cancellationToken);
            
            var activeSubscription = organization.Subscriptions
                .FirstOrDefault(s => s.IsActive);

            if (activeSubscription != null)
            {
                subscriptionOrderId = activeSubscription.SubscriptionOrderId;
                _logger.LogDebug(
                    "Using active subscriptionOrderId {SubscriptionOrderId} for organization {OrganizationId}",
                    subscriptionOrderId,
                    request.OrganizationId);
            }
            else
            {
                throw new InvalidOperationException(
                    $"No active subscription found for organization {request.OrganizationId}. Please provide a valid SubscriptionOrderId.");
            }
        }


        var licenseType = string.IsNullOrWhiteSpace(request.LicenseType) ? "Student" : request.LicenseType;

        List<OrganizationUser>? orgUsers = null;
        if (request.StudentIds != null && request.StudentIds.Count > 0)
        {
            orgUsers = await _organizationUserRepository.GetByIdsAsync(request.StudentIds, cancellationToken);
            if (orgUsers.Count != request.StudentIds.Count)
            {
                var foundIds = orgUsers.Select(ou => ou.Id).ToList();
                var missingIds = request.StudentIds.Except(foundIds).ToList();
                throw new InvalidOperationException($"Some student IDs are invalid: {string.Join(", ", missingIds)}");
            }

           
            var invalidUsers = orgUsers
                .Where(ou =>
                    ou.OrganizationId != request.OrganizationId
                    || ou.OrganizationRole != OrganizationRole.Student)
                .ToList();

            if (invalidUsers.Any())
            {
                var invalidIds = invalidUsers.Select(ou => ou.Id).ToList();
                throw new InvalidOperationException(
                    $"Some users are not valid students in this organization: {string.Join(", ", invalidIds)}");
            }
        }


        GroupGrade? grade = null;
        if (request.Grade.HasValue && Enum.IsDefined(typeof(GroupGrade), request.Grade.Value))
        {
            grade = (GroupGrade)request.Grade.Value;
        }

        return await _unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            Group group;
            
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                group = Group.CreateWithCode(
                    request.OrganizationId,
                    request.Name,
                    request.CreatedByUserId,
                    organizationCode,
                    request.Code, // This is the group segment
                    request.Description,
                    grade);
            }
            else
            {
                group = Group.Create(
                    request.OrganizationId,
                    request.Name,
                    request.CreatedByUserId,
                    request.Description,
                    code: null,
                    grade);
            }

            await _groupRepository.AddAsync(group, cancellationToken);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            if (orgUsers != null && orgUsers.Count > 0)
            {
 
                var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
                    organizationId: request.OrganizationId,
                    subscriptionOrderId: subscriptionOrderId,
                    cancellationToken);

                var studentsNeedingLicense = orgUsers
                    .Where(ou => !activeOrgUserIds.Contains(ou.Id))
                    .ToList();


                if (studentsNeedingLicense.Any())
                {
                    _logger.LogInformation(
                        "Assigning licenses to {Count} students without active license",
                        studentsNeedingLicense.Count);

                    foreach (var student in studentsNeedingLicense)
                    {
                        if (student.User?.Email == null)
                        {
                            _logger.LogWarning(
                                "Cannot assign license to student {StudentId}: email is null",
                                student.Id);
                            continue;
                        }

                        try
                        {
                            var licenseResult = await _orderLicenseService.AssignLicenseAsync(
                                request.OrganizationId,
                                student.User.Email,
                                licenseType,
                                subscriptionOrderId,
                                cancellationToken);

                            if (licenseResult.Success)
                            {
                                _logger.LogInformation(
                                    "Successfully assigned license {LicenseAssignmentId} to student {StudentId} ({Email})",
                                    licenseResult.LicenseAssignmentId,
                                    student.Id,
                                    student.User.Email);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Failed to assign license to student {StudentId} ({Email}): {ErrorMessage}. Student will still be added to group.",
                                    student.Id,
                                    student.User.Email,
                                    licenseResult.ErrorMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Error assigning license to student {StudentId} ({Email}). Student will still be added to group.",
                                student.Id,
                                student.User?.Email);
                        }
                    }
                }

                // Assign all students to group (regardless of license assignment result)
                foreach (var student in orgUsers)
                {
                    student.AssignToGroup(group.Id);
                }

                await _organizationUserRepository.UpdateRangeAsync(orgUsers, cancellationToken);
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Added {Count} students to group {GroupId}",
                    orgUsers.Count,
                    group.Id);
            }

            return new GroupDto
            {
                Id = group.Id,
                OrganizationId = group.OrganizationId,
                Name = group.Name,
                Description = group.Description,
                Code = group.Code,
                Status = group.Status,
                CreatedByUserId = group.CreatedByUserId,
                CreatedAt = group.CreatedAt,
                UpdatedAt = group.UpdatedAt
            };
        }, cancellationToken);
    }
}

