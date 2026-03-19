using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Grpc;
using Resource.Application.Queries.Curriculum;
using Resource.Application.Specifications.Curriculums;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Curriculum
{
    public class GetCurriculumRelationsQueryHandler : IRequestHandler<GetCurriculumRelationsQuery, CurriculumRelationsResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IGrpcEmulationClient _grpcEmulationClient;

        public GetCurriculumRelationsQueryHandler(
            IResourceUnitOfWork unitOfWork,
            IGrpcEmulationClient grpcEmulationClient)
        {
            _unitOfWork = unitOfWork;
            _grpcEmulationClient = grpcEmulationClient;
        }

        public async Task<CurriculumRelationsResponse> Handle(
            GetCurriculumRelationsQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new CurriculumBasicSnapshotSpecification(request.Id);
            var curriculum = await _unitOfWork.Curriculums.FirstOrDefaultAsync(spec, cancellationToken);

            if (curriculum == null)
            {
                throw new KeyNotFoundException($"Khung chương trình không tồn tại trong hệ thống");
            }

            var response = new CurriculumRelationsResponse { 
                Title = curriculum.Title,
                Description = curriculum.Description,
                ImageUrl = curriculum.ImageUrl,
                Code = curriculum.Code,
                Id = curriculum.Id
            };

            var courses = curriculum.CurriculumCourses
                .DistinctBy(c => c.CourseId)
                .Select(c => new CourseSnapshot
                        {
                            CourseId = c.CourseId,
                            Title = c.Course.Title,
                            ImageUrl = c.Course.ImageUrl,
                            Description = c.Course.Description,
                            Level = c.Course.Level.ToString(),
                            Code = c.Course.Code,
                            KitId = c.Course.KitId
                        })
                 .ToList(); 

            var emulationIds = curriculum.CurriculumEmulations.Select(c => c.EmulationId).ToList();
            foreach (var emulationId in emulationIds)
            {
                var emulation = await _grpcEmulationClient.GetEmulationByIdAsync(emulationId);
                if (emulation != null)
                {
                    response.Emulators.Add(new EmulatorSnapshot
                    {
                        EmulationId = emulation.EmulationId,
                        Name = emulation.Name,
                        Description = emulation.Description,
                        ThumbnailUrl = emulation.ThumbnailUrl
                    });
                }
            }

            response.Courses.AddRange(courses);
            
            return response;
        }
    }
}