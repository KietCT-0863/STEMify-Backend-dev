using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Lesson;
using Resource.Application.Queries.Lesson;
using ServiceStack;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class LessonGrpcService : LessonService.LessonServiceBase
    {
        private readonly IMediator _mediator;

        public LessonGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<LessonResponse> CreateLesson(
            CreateLessonRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateLessonCommand
            {
                Title = request.Title,
                ImageBytes = request.Image?.ToByteArray(),
                Description = request.Description,
                //OrderIndex = request.OrderIndex,
                CreatedByUserId = request.CreatedByUserId,
                CourseId = request.CourseId,
                LearningOutcome = request.LearningOutcome,
                Requirement = request.Requirement,
                SkillIds = request.SkillIds.ToList(),
                TopicIds = request.TopicIds.ToList(),
                StandardIds = request.StandardIds.ToList(),
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<LessonResponse> GetLesson(
            GetLessonRequest request,
            ServerCallContext context
        )
        {
            var query = new GetLessonByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"Lesson with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<LessonResponse> UpdateLesson(
            UpdateLessonRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateLessonCommand
            {
                Id = request.Id,
                Title = request.Title,
                LearningOutcome = request.LearningOutcome,
                Requirement = request.Requirement,
                StandardIds = request.StandardIds.ToList(),
                SkillIds = request.SkillIds.ToList(),
                TopicIds = request.TopicIds.ToList(),
                ImageBytes = request.Image?.ToByteArray(),
                Description = request.Description,
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.LessonStatus.Draft),
                OrderIndex = request.OrderIndex,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteLesson(
            DeleteLessonRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteLessonCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<LessonList> ListLessons(Empty request, ServerCallContext context)
        {
            try
            {
                var result = await _mediator.Send(new GetLessonListQuery());

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListLessons failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedLessonList> QueryLessons(
            QueryLessonsRequest request,
            ServerCallContext context
        )
        {
            Domain.Enums.LessonStatus? status = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (
                    System.Enum.TryParse<Domain.Enums.LessonStatus>(
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

            var query = new QueryLessonsQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                CreatedByUserId = request.CreatedByUserId,
                Status = status,
                CourseId = request.CourseId,
                Duration = request.Duration,
                AgeRangeId = request.AgeRangeId,
                TopicId = request.TopicId,
                SkillId = request.SkillId,
                StandardId = request.StandardId,
                SortDirection = sortDirection,
            };
            var result = await _mediator.Send(query);
            return result;
        }

        public override async Task<Empty> UpdateLessonsOrder(
            UpdateLessonsOrderRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateLessonsOrderCommand
            {
                CourseId = request.CourseId,
                OrderedLessonIds = request.OrderedLessonIds.ToList(),
            };

            await _mediator.Send(command);
            return new Empty();
        }
    }
}
