using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Models.ClassroomModels;
using Classroom.Application.Specifications.Classrooms;
using MediatR;
using Shared.Exceptions;

namespace Classroom.Application.Features.Classrooms.Commands.UpdateClassroom
{
    public class UpdateClassroomCommandHandler
        : IRequestHandler<UpdateClassroomCommand, ClassroomModel>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcUserClient _userClient;
        private readonly ICurriculumCacheService _curriculumCache;

        public UpdateClassroomCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcUserClient userClient,
            ICurriculumCacheService curriculumCache)
        {
            _unitOfWork = unitOfWork;
            _userClient = userClient;
            _curriculumCache = curriculumCache;
        }

        public async Task<ClassroomModel> Handle(
            UpdateClassroomCommand request,
            CancellationToken cancellationToken
        )
        {
            var classroomEntity = await _unitOfWork.Classrooms.FirstOrDefaultAsync(
                new ClassroomByIdSpecification(request.Id),
                cancellationToken
            );
            if (classroomEntity == null)
            {
                throw new NotFoundException($"Classroom with ID {request.Id} not found.");
            }
            // OrganizationUser validation
            if (request.TeacherId != null)
            {
                // Validate teacher exists
                await _userClient.GetOrganizationUserByIdAsync(request.TeacherId.Value, cancellationToken);
            }
            if (request.CourseId != null)
            {
                // Validate curriculum exists
                await _curriculumCache.GetByIdAsync(request.CourseId.Value, cancellationToken);
            }

            classroomEntity.PatchFromCommand(request);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return classroomEntity.ToClassroomModel();
        }
    }
}
