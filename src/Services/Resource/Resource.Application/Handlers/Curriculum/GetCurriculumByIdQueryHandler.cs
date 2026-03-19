using MediatR;
using Microsoft.Extensions.Logging;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Cache;
using Resource.Application.Common.Interfaces.Grpc;
using Resource.Application.Queries.Curriculum;
using Resource.Application.Specifications.Curriculums;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Curriculum
{
    public class GetCurriculumByIdQueryHandler : IRequestHandler<GetCurriculumByIdQuery, CurriculumDetails>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IUserCacheService _userCache;
        private readonly IGrpcEmulationClient _grpcEmulationClient;
        private readonly ILogger<GetCurriculumByIdQueryHandler> _logger;

        public GetCurriculumByIdQueryHandler(
            IResourceUnitOfWork unitOfWork,
            IUserCacheService userCache,
            IGrpcEmulationClient grpcEmulationClient,
            ILogger<GetCurriculumByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _userCache = userCache;
            _grpcEmulationClient = grpcEmulationClient;
            _logger = logger;
        }

        public async Task<CurriculumDetails> Handle(
            GetCurriculumByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new CurriculumByIdSpecification(request.Id);
            var curriculum = await _unitOfWork.Curriculums.FirstOrDefaultAsync(spec, cancellationToken);

            if (curriculum == null)
                throw new KeyNotFoundException($"Curriculum with ID {request.Id} not found.");

            var user = await _userCache.GetByIdAsync(Guid.Parse(curriculum.CreatedByUserId), cancellationToken);

            var response = new CurriculumDetails
            {
                Id = curriculum.Id,
                Title = curriculum.Title ?? string.Empty,
                Code = curriculum.Code ?? string.Empty,
                ImageUrl = curriculum.ImageUrl ?? string.Empty,
                Description = curriculum.Description ?? string.Empty,
                Status = curriculum.Status.ToString(),
                CreatedByUserId = curriculum.CreatedByUserId ?? string.Empty,
                CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(curriculum.CreatedDate),
                LastModifiedDate = curriculum.LastModifiedDate != null
                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(curriculum.LastModifiedDate.Value)
                    : null,
                ApprovedByUserId = curriculum.ApprovedByUserId ?? string.Empty,
                ApprovedAt = curriculum.ApprovedAt != null
                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(curriculum.ApprovedAt.Value)
                    : null,
                CreatedByUserName = user?.Name ?? curriculum.CreatedByUserId,
                CourseCount = curriculum.CurriculumCourses.Count,
                Duration = curriculum.CurriculumCourses
                    .Where(cc => cc.Course != null)
                    .Sum(cc => cc.Course.Lessons?.Sum(l => l.Duration) ?? 0)
            };

            // Map related courses
            response.Courses.AddRange(
                curriculum.CurriculumCourses.Select(cc => new CourseDetails
                {
                    Id = cc.Course.Id,
                    Title = cc.Course.Title ?? string.Empty,
                    Code = cc.Course.Code ?? string.Empty,
                    ImageUrl = cc.Course.ImageUrl ?? string.Empty,
                    Description = cc.Course.Description ?? string.Empty,
                    Duration = cc.Course.Duration,
                    Status = cc.Course.Status.ToString(),
                    Level = cc.Course.Level.ToString(),
                    AgeRangeLabel = cc.Course.AgeRange?.AgeRangeLabel ?? string.Empty,
                    CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(cc.Course.CreatedDate),
                    CourseOrderIndex = cc.CourseOrderIndex,
                    Lessons = {
                        cc.Course.Lessons.Select(lesson => new GrpcLessonModel
                        {
                            Id = lesson.Id,
                            Title = lesson.Title ?? string.Empty,
                            Duration = lesson.Duration,
                        })
                    }
                })
            );

            response.KitIds.AddRange(
                curriculum.CurriculumCourses
                    .Where(cc => cc.Course != null && cc.Course.KitId.HasValue)
                    .Select(cc => cc.Course!.KitId!.Value)
                    .Distinct());

            response.Skills.AddRange(
                curriculum.CurriculumCourses
                    .SelectMany(cc => cc.Course.Lessons != null ? cc.Course.Lessons : Enumerable.Empty<Resource.Domain.Entities.Lesson>())
                    .SelectMany(lesson => lesson.LessonSkills != null ? lesson.LessonSkills : Enumerable.Empty<Resource.Domain.Entities.LessonSkill>())
                    .Where(ls => ls.Skill != null && !string.IsNullOrWhiteSpace(ls.Skill.SkillName))
                    .Select(ls => ls.Skill.SkillName)
                    .Distinct()
            );

            response.Topics.AddRange(
                curriculum.CurriculumCourses
                    .SelectMany(cc => cc.Course.Lessons != null ? cc.Course.Lessons : Enumerable.Empty<Resource.Domain.Entities.Lesson>())
                    .SelectMany(lesson => lesson.LessonTopics != null ? lesson.LessonTopics : Enumerable.Empty<Resource.Domain.Entities.LessonTopic>())
                    .Where(lt => lt.Topic != null && !string.IsNullOrWhiteSpace(lt.Topic.Name))
                    .Select(lt => lt.Topic.Name)
                    .Distinct()
            );

            var emulationIds = curriculum.CurriculumEmulations
                .Select(ce => ce.EmulationId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            if (emulationIds.Count > 0)
            {
                foreach (var emId in emulationIds)
                {
                    try
                    {
                        // IGrpcEmulationClient returns EmulationDetailResponse; map to EmulationResponse summary type
                        var emulationDetail = await _grpcEmulationClient.GetEmulationByIdAsync(emId);
                        if (emulationDetail != null)
                        {
                            var emulationSummary = new Emulator.API.Protos.EmulationListItem
                            {
                                EmulationId = emulationDetail.EmulationId ?? string.Empty,
                                Name = emulationDetail.Name ?? string.Empty,
                                Slug = emulationDetail.Slug ?? string.Empty,
                                Status = emulationDetail.Status ?? string.Empty,
                                ThumbnailUrl = emulationDetail.ThumbnailUrl ?? string.Empty,
                                CreatedAt = emulationDetail.CreatedAt,
                                Description = emulationDetail.Description ?? string.Empty,
                                Difficulty = string.Empty,
                                UserId = emulationDetail.UserId ?? string.Empty
                            };

                            response.Emulations.Add(emulationSummary);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch emulation {EmulationId} via gRPC", emId);
                    }
                }
            }

            return response;
        }
    }
}