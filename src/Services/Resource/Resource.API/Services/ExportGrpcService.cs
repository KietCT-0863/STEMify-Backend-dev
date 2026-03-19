using Grpc.Core;
using MediatR;
using Resource.Application.Queries.Exporter;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class ExportGrpcService : ExportService.ExportServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ExportGrpcService> _logger;

        public ExportGrpcService(IMediator mediator, ILogger<ExportGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task<ExportLessonResponse> ExportLesson(
            ExportLessonRequest request,
            ServerCallContext context)
        {
            try
            {
                var query = new GetExportedLesson(request.LessonId);
                var result = await _mediator.Send(query, context.CancellationToken);

                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Lesson not found: {Message}", ex.Message);
                throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting lesson {LessonId}", request.LessonId);
                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while exporting the lesson"));
            }
        }

        public override async Task<ExportCourseResponse> ExportCourse(
            ExportCourseRequest request,
            ServerCallContext context)
        {
            var query = new GetExportedCourse(request.CourseId);
            var result = await _mediator.Send(query, context.CancellationToken);

            return result;
        }
    }
}
