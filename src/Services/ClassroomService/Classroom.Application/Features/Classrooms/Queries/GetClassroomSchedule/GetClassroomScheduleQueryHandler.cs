using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using DnsClient.Internal;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.Classrooms.Queries.GetClassroomSchedule
{
    public class GetClassroomScheduleQueryHandler :
        IRequestHandler<GetClassroomScheduleQuery, GrpcClassroomScheduleResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcCourseClient _grpcCourseClient;
        private readonly ILogger<GetClassroomScheduleQueryHandler> _logger;
        public GetClassroomScheduleQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcCourseClient grpcCourseClient,
            ILogger<GetClassroomScheduleQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _grpcCourseClient = grpcCourseClient;
            _logger = logger;
        }
        public async Task<GrpcClassroomScheduleResponse> Handle(GetClassroomScheduleQuery request, CancellationToken cancellationToken)
        {
            var classroom = await _unitOfWork.Classrooms.FindByIdAsync(request.ClassroomId);
            if (classroom == null)
                throw new NotFoundException("Classroom not found");

            var course = await _grpcCourseClient.GetCourseByIdAsync(classroom.CourseId);

            // 1. Tính số tuần
            var totalWeeks = (int)Math.Ceiling(
                (classroom.EndDate.ToDateTime(TimeOnly.MinValue) - classroom.StartDate.ToDateTime(TimeOnly.MinValue)).TotalDays / 7
            );

            // 2. Tổng thời lượng course (gốc, chưa scale)
            var originalTotalMinutes = course.Duration;
            _logger.LogInformation("Original total minutes for CourseId {CourseId} is {Minutes}", course.Id, originalTotalMinutes);

            if (originalTotalMinutes <= 0)
                _logger.LogWarning("CourseId {CourseId} has non-positive total duration: {Minutes}", course.Id, originalTotalMinutes);

            // 3. Tính minutesPerWeek
            var minutesPerWeek = (int)Math.Ceiling((double)originalTotalMinutes / totalWeeks);

            // 5. Build courseSchedules
            int currentWeek = 1;
            var lessons = course.Lessons
                .Select(l => new GrpcLessonSchedule
                {
                    LessonId = l.Id,
                    LessonTitle = l.Title,
                    Duration = l.Duration
                })
                .ToList();

            var (distributed, lastUsedWeek) = DistributeLessonsByWeek(lessons, minutesPerWeek, currentWeek);
            currentWeek = lastUsedWeek + 1;

            return new GrpcClassroomScheduleResponse
            {
                MinutesPerWeek = minutesPerWeek,
                TotalWeeks = totalWeeks,
                CourseId = course.Id,
                CourseTitle = course.Title,
                ScheduleItems = { distributed }
            };
        }

        private (List<GrpcCourseScheduleItem> Items, int LastWeek) DistributeLessonsByWeek(
            List<GrpcLessonSchedule> lessons,
            int maxMinutesPerWeek,
            int startWeek)
        {
            var result = new List<GrpcCourseScheduleItem>();

            int week = startWeek;
            int weekTotal = 0;
            var buffer = new List<GrpcLessonSchedule>();

            foreach (var lesson in lessons)
            {
                int remaining = lesson.Duration;

                while (remaining > 0)
                {
                    int available = maxMinutesPerWeek - weekTotal;

                    // Nếu còn slot trong tuần này → nhét phần vừa đủ
                    if (available > 0)
                    {
                        int chunk = Math.Min(available, remaining);

                        buffer.Add(new GrpcLessonSchedule
                        {
                            LessonId = lesson.LessonId,
                            LessonTitle = lesson.LessonTitle,
                            Duration = chunk
                        });

                        weekTotal += chunk;
                        remaining -= chunk;
                    }

                    // Nếu tuần đã đầy → đóng tuần và mở tuần mới
                    if (weekTotal == maxMinutesPerWeek)
                    {
                        result.Add(new GrpcCourseScheduleItem
                        {
                            WeekNumber = week++,
                            LessonSchedule = { buffer }
                        });

                        buffer = new List<GrpcLessonSchedule>();
                        weekTotal = 0;
                    }
                }
            }

            // Đóng tuần cuối cùng
            if (buffer.Any())
            {
                result.Add(new GrpcCourseScheduleItem
                {
                    WeekNumber = week,
                    LessonSchedule = { buffer }
                });
            }

            return (result, week);
        }

    }
}
