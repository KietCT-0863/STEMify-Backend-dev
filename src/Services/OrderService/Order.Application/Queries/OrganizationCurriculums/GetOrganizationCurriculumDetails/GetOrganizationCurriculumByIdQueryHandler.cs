using Emulator.API.Protos;
using MediatR;
using Order.Application.Common.Interfaces;
using Shared.Protos.Order;
using Shared.Protos.Resource;

namespace Order.Application.Queries.OrganizationCurriculums.GetOrganizationCurriculumDetails
{
    public class GetOrganizationCurriculumByIdQueryHandler : IRequestHandler<GetOrganizationCurriculumByIdQuery, OrganizationCurriculumModel>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        public GetOrganizationCurriculumByIdQueryHandler(
            IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<OrganizationCurriculumModel> Handle(GetOrganizationCurriculumByIdQuery request, CancellationToken cancellationToken)
        {
            var orgCurr = await _unitOfWork.SubscriptionOrderCurriculums.FindOneAsync(sc =>
                sc.CurriculumId == request.CurriculumId &&
                sc.OrganizationSubscriptionOrder.OrganizationId == request.OrgId,
                cancellationToken);

            if (orgCurr == null)
            {
                throw new KeyNotFoundException("Khung chương trình này không tồn tại trong tổ chức.");
            }

            var grpcCurriculum = new OrganizationCurriculumModel
            {
                Id = orgCurr.CurriculumId,
                Title = orgCurr.CurriculumTitle,
                ImageUrl = orgCurr.CurriculumImageUrl,
                Code = orgCurr.CurriculumCode,
                Description = orgCurr.CurriculumDescription,
                CourseCount = orgCurr.CoursesSnapshot.Count,
            };
            grpcCurriculum.KitIds.AddRange(
                    orgCurr.CoursesSnapshot
                        .Where(c => c.KitId.HasValue)
                        .Select(c => c.KitId.Value)
                        .Distinct()
                );

            // Courses
            var courses = orgCurr.CoursesSnapshot.Select(c => new CourseDetails
            {
                Id = c.Id,
                Title = c.Title,
                ImageUrl = c.ImageUrl,
                Description = c.Description,
                Level = c.Level,
                Code = c.Code,
            })
            .DistinctBy(c => c.Id) 
            .ToList(); ;

            // Emulators
            var emulators = orgCurr.EmulatorsSnapshot.Select(e => new EmulationListItem
            {
                EmulationId = e.EmulationId,
                Name = e.Name,
                Description = e.Description,
                ThumbnailUrl = e.ThumbnailUrl
            })
            .DistinctBy(e => e.EmulationId)
            .ToList();

            grpcCurriculum.Courses.AddRange(courses);
            grpcCurriculum.Emulations.AddRange(emulators);
            return grpcCurriculum;
        }
    }
}
