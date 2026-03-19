using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Models.ClassroomModels;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Protos.Resource;

namespace Classroom.Infrastructure.Services.Grpc
{
    public class GrpcLessonClient : IGrpcLessonClient
    {
        private readonly ILogger<GrpcLessonClient> _logger;
        private readonly LessonService.LessonServiceClient _client;

        public GrpcLessonClient(
            ILogger<GrpcLessonClient> logger,
            LessonService.LessonServiceClient client
        )
        {
            _logger = logger;
            _client = client;
        }

        public async Task<LessonModel> GetLessonByIdAsync(int lessonId)
        {
            _logger.LogInformation("Calling GRPC Service to get lesson by id: {id}", lessonId);

            var request = new GetLessonRequest { Id = lessonId };
            var response = await _client.GetLessonAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No lesson found for id: {id}", lessonId);
                throw new NotFoundException("No lesson found");
            }
            var lessonModel = new LessonModel
            {
                Id = response.Id,
                Title = response.Title,
                SectionIds = response.SectionIds,
                OrderIndex = response.OrderIndex,
            };
            return lessonModel;
        }

        public async Task<PagedLessonList> GetLessonsAsync(QueryLessonsRequest request)
        {
            _logger.LogInformation("Calling GRPC Service to get lessons with request: {@request}", request);

            var response = await _client.QueryLessonsAsync(request);

            _logger.LogInformation("Received {count} lessons from GRPC Service", response.Items.Count);

            return response;
        }
    }
}
