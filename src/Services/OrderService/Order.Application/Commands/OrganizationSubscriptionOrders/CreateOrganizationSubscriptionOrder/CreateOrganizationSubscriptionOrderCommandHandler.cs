using Contracts.Abstractions.Services;
using DnsClient.Internal;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Cache;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Queries.OrganizationSubscriptionOrders.GetOrganizationSubscriptionOrderById;
using Order.Domain.Entities;
using Shared.DTOs.Cloudinary;
using Shared.Helper;
using Shared.Protos.Order;

namespace Order.Application.Commands.OrganizationSubscriptionOrders.CreateOrganizationSubscriptionOrder
{
    public class CreateOrganizationSubscriptionOrderCommandHandler : IRequestHandler<CreateOrganizationSubscriptionOrderCommand, GrpcOrganizationSubscriptionOrderDetail>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IGrpcCurriculumClient _curriculumClient;
        private readonly IPlanBillingCycleCacheService _planBillingCycleCacheService;
        private readonly IMediator _mediator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<CreateOrganizationSubscriptionOrderCommandHandler> _logger;

        public CreateOrganizationSubscriptionOrderCommandHandler(
            IOrderUnitOfWork unitOfWork,
            IGrpcCurriculumClient curriculumClient,
            IPlanBillingCycleCacheService planBillingCycleCacheService,
            IMediator mediator,
            ICloudinaryService cloudinaryService,
            ILogger<CreateOrganizationSubscriptionOrderCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _curriculumClient = curriculumClient;
            _planBillingCycleCacheService = planBillingCycleCacheService;
            _mediator = mediator;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        public async Task<GrpcOrganizationSubscriptionOrderDetail> Handle(CreateOrganizationSubscriptionOrderCommand request, CancellationToken cancellationToken)
        {
            var organization = await _unitOfWork.Organizations.FindByIdAsync(request.OrganizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException($"Organization with ID {request.OrganizationId} not found.");

            var planBillingCycle = await _planBillingCycleCacheService.GetPlanBillingCycleByIdAsync(request.PlanBillingCycleId, cancellationToken);
            if (planBillingCycle == null)
            {
                throw new KeyNotFoundException($"Plan Billing Cycle with ID {request.PlanBillingCycleId} not found.");
            }

            if (request.CurriculumIds != null && request.CurriculumIds.Count > planBillingCycle.CurriculumCount)
                throw new ArgumentException($"The number of curriculums exceeds the limit of {planBillingCycle.CurriculumCount} for this plan billing cycle.");

            var subscriptionCurriculums = new List<SubscriptionOrderCurriculum>();
            if (request.CurriculumIds != null && request.CurriculumIds.Count > 0)
            {
                foreach (var curriculumId in request.CurriculumIds)
                {
                    var curriculumRelations = await _curriculumClient.GetCurriculumRelations(curriculumId);
                    if (curriculumRelations == null)
                    {
                        _logger.LogInformation("Curriculum with ID {CurriculumId} not found.", curriculumId);
                        throw new KeyNotFoundException($"Khung chương trình không tồn tại");
                    }

                    var courses = curriculumRelations.Courses.Select(c => new CourseSnapshot
                    {
                        Id = c.CourseId,
                        Title = c.Title,
                        ImageUrl = c.ImageUrl,
                        Description = c.Description,
                        Level = c.Level,
                        Code = c.Code,
                        KitId = c.KitId
                    });

                    var emulators = curriculumRelations.Emulators.Select(e => new EmulatorSnapshot
                    {
                        EmulationId = e.EmulationId,
                        Name = e.Name,
                        Description = e.Description,
                        ThumbnailUrl = e.ThumbnailUrl
                    });

                    subscriptionCurriculums.Add(new SubscriptionOrderCurriculum
                    {
                        CurriculumId = curriculumId,
                        CurriculumTitle = curriculumRelations.Title,
                        CurriculumCode = curriculumRelations.Code,
                        CurriculumDescription = curriculumRelations.Description,
                        CurriculumImageUrl = curriculumRelations.ImageUrl,
                        CoursesSnapshot = courses.ToList(),
                        EmulatorsSnapshot = emulators.ToList(),
                    });
                }
            }

            // Create new contract if ContractId is not provided and Contract details are given
            int contractId = await GetOrCreateContractIdAsync(request, cancellationToken);

            var grossAmount = (decimal)planBillingCycle.Price;
            var netAmount = grossAmount - (grossAmount * request.DiscountPercent / 100);

            var months = (int)planBillingCycle.BillingCycle;
            if (months <= 0)
                months = 12;

            var code = CodeGeneratorHelper.GenerateSubscriptionCode(organization.Code, request.StartDate);

            var oso = new OrganizationSubscriptionOrder
            {
                OrganizationId = request.OrganizationId,
                PlanBillingCycleId = request.PlanBillingCycleId,
                ContractId = contractId,
                ParentSubscriptionId = request.ParentSubscriptionId,
                Code = code,
                PlanName = planBillingCycle.Name,
                GrossAmount = grossAmount,
                NetAmount = netAmount,
                DiscountPercent = request.DiscountPercent,
                StartDate = request.StartDate,
                EndDate = request.StartDate.AddMonths(months),
                MaxStudentSeats = request.MaxStudentSeats,
                MaxTeacherSeats = request.MaxTeacherSeats,
                CurriculumCount = request.CurriculumIds?.Count ?? 0,
                Status = DateOnly.FromDateTime(request.StartDate) == DateOnly.FromDateTime(DateTime.Today)
                        ? Domain.Enums.OrganizationSubscriptionOrderStatus.Active
                        : Domain.Enums.OrganizationSubscriptionOrderStatus.Pending,
                SubscriptionOrderCurriculums = subscriptionCurriculums
            };

            await _unitOfWork.OrganizationSubscriptionOrders.AddAsync(oso, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var query = new GetOrganizationSubscriptionOrderByIdQuery
            {
                Id = oso.Id
            };
            var result = await _mediator.Send(query, cancellationToken);

            return result;
        }

        private async Task<int> GetOrCreateContractIdAsync(CreateOrganizationSubscriptionOrderCommand request, CancellationToken cancellationToken)
        {
            // Nếu đã có ContractId hợp lệ → dùng luôn
            if (request.ContractId is not null && request.ContractId > 0)
                return request.ContractId.Value;

            // Nếu chưa có contractId mà cũng không có dữ liệu contract → lỗi
            if (request.Contract == null)
                throw new ArgumentException("Contract data is required when ContractId is not provided.");

            // Tạo mới contract
            var newContract = new Domain.Entities.Contract
            {
                OrganizationId = request.OrganizationId,
                Name = request.Contract.Name,
                Description = request.Contract.Description
            };

            // Upload file nếu có
            if (request.Contract.FileBytes != null && request.Contract.FileBytes.Length > 0)
            {
                var uploadRequest = new UploadDocumentBytesRequest
                {
                    FileBytes = request.Contract.FileBytes,
                    FileName = $"{request.Contract.Name}-{Guid.NewGuid()}",
                };

                var uploadResult = await _cloudinaryService.UploadDocumentAsync(uploadRequest);
                if (uploadResult != null)
                {
                    newContract.FileUrl = uploadResult.AssetUrl;
                }
            }

            // Lưu contract mới
            await _unitOfWork.Contracts.AddAsync(newContract, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return newContract.Id;
        }

    }
}