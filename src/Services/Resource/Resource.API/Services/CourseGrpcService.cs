using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Course;
using Resource.Application.Queries.Course;
using ServiceStack;
using Shared.Extensions;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class CourseGrpcService : CourseService.CourseServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CourseGrpcService> _logger;

        public CourseGrpcService(IMediator mediator, ILogger<CourseGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task<CourseResponse> CreateCourse(
            CreateCourseRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateCourseCommand
            {
                Title = request.Title,
                ImageBytes = request.Image?.ToByteArray(),
                Code = request.Code,
                StudentTasks = request.StudentTasks,
                Prerequisites = request.Prerequisites,
                Level = request.Level.ToEnumOrDefault(Domain.Enums.CourseLevel.Beginner),
                Slug = request.Slug,
                Description = request.Description,
                CreatedByUserId = request.CreatedByUserId,
                AgeRangeId = request.AgeRangeId,
                KitId = request.KitId,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<CourseDetail> GetCourse(
            GetCourseRequest request,
            ServerCallContext context
        )
        {
            var query = new GetCourseByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"Course with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<CourseResponse> UpdateCourse(
            UpdateCourseRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateCourseCommand
            {
                Id = request.Id,
                Title = request.Title,
                Code = request.Code,
                StudentTasks = request.StudentTasks,
                Prerequisites = request.Prerequisites,
                ImageBytes = request.Image?.ToByteArray(),
                Slug = request.Slug,
                Description = request.Description,
                Status = request.Status.ToEnumOrNull<Domain.Enums.CourseStatus>(),
                Level = request.Level.ToEnumOrNull<Domain.Enums.CourseLevel>(),
                AgeRangeId = request.AgeRangeId,
                KitId = request.KitId,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteCourse(
            DeleteCourseRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteCourseCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<CourseList> ListCourses(Empty request, ServerCallContext context)
        {
            var result = await _mediator.Send(new GetCourseListQuery());

            return result;
        }

        public override async Task<PagedCourseList> QueryCourses(
            QueryCoursesRequest request,
            ServerCallContext context
        )
        {
            Domain.Enums.CourseStatus? status = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (
                    System.Enum.TryParse<Domain.Enums.CourseStatus>(
                        request.Status,
                        true,
                        out var parsedStatus
                    )
                )
                {
                    status = parsedStatus;
                }
            }

            Shared.Enums.SortDirection? sortDirection = null;
            if (!string.IsNullOrWhiteSpace(request.SortDirection))
            {
                if (
                    System.Enum.TryParse<Shared.Enums.SortDirection>(
                        request.SortDirection,
                        true,
                        out var parsedSortDirection
                    )
                )
                {
                    sortDirection = parsedSortDirection;
                }
            }
            var query = new QueryCoursesQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                CreatedByUserId = request.CreatedByUserId,
                Status = status,
                AgeRangeId = request.AgeRangeId,
                CategoryId = request.TopicId,
                SkillId = request.SkillId,
                StandardId = request.StandardId,
                SortDirection = sortDirection,
                KitId = request.KitId
            };
            var result = await _mediator.Send(query);

            return result;
        }
    }
}
