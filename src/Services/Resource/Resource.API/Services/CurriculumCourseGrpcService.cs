using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Course;
using Resource.Application.Commands.CurriculumCourse;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class CurriculumCourseGrpcService : CurriculumCourseService.CurriculumCourseServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CurriculumGrpcService> _logger;

        public CurriculumCourseGrpcService(IMediator mediator, ILogger<CurriculumGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        public override async Task<Empty> CreateCurriculumCourse(
            CreateCurriculumCourseRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateCurriculumCourseCommand
            {
                CourseIds = request.CourseIds.ToList(),
                CurriculumId = request.CurriculumId,
            };

            await _mediator.Send(command);
            return new Empty();
        }

        public override async Task<Empty> DeleteCurriculumCourse(
            DeleteCurriculumCourseRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteCurriculumCourseCommand
            {
                CourseIds = request.CourseIds.ToList(),
                CurriculumId = request.CurriculumId
            };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<Empty> UpdateCoursesOrder(
            UpdateCoursesOrderRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateCoursesOrderCommand
            {
                CurriculumId = request.CurriculumId,
                OrderedCourseIds = request.OrderedCourseIds.ToList(),
            };

            await _mediator.Send(command);
            return new Empty();
        }
    }
}
