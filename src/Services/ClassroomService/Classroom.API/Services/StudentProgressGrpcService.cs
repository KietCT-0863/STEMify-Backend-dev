using Classroom.Application.Features.StudentProgress.Commands.UpdateSectionProgress;
using Classroom.Application.Features.StudentProgress.Queries.GetLessonProgress;
using Classroom.Application.Features.StudentProgress.Queries.GetSectionProgress;
using Classroom.Application.Features.StudentProgress.Queries.GetStudentProgressByClassroomId;
using Grpc.Core;
using MediatR;
using ServiceStack;
using Shared.Helper;
using Shared.Protos.Classroom;

namespace Classroom.API.Services
{
    public class StudentProgressGrpcService : GrpcStudentProgress.GrpcStudentProgressBase
    {
        private readonly IMediator _mediator;

        public StudentProgressGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcPagedLessonProgressResponse> GetLessonProgress(
            GetLessonProgressRequest request,
            ServerCallContext context
        )
        {
            var result = await _mediator.Send(new GetLessonProgressQuery(request.EnrollmentId));

            // Map result domain → gRPC
            var response = new GrpcPagedLessonProgressResponse
            {
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
            };
            response.Items.AddRange(
                result.Items.Select(e => new LessonProgressModel
                {
                    Id = e.Id,
                    LessonId = e.LessonId,
                    CompletedAt = e.CompletedAt?.ToUtcTimestamp(),
                    Status = e.Status,
                })
            );

            return response;
        }

        public override async Task<GrpcPagedSectionProgressResponse> GetSectionProgress(
            GetSectionProgressRequest request,
            ServerCallContext context
        )
        {
            var result = await _mediator.Send(
                new GetSectionProgressQuery(request.EnrollmentId, request.LessonId)
            );

            // Map result domain → gRPC
            var response = new GrpcPagedSectionProgressResponse
            {
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
            };
            response.Items.AddRange(
                result.Items.Select(e => new SectionProgressModel
                {
                    Id = e.Id,
                    SectionId = e.SectionId,
                    CompletedAt = e.CompletedAt?.ToUtcTimestamp(),
                    Status = e.Status,
                    StudentQuizId = e.StudentQuizId,
                    StudentAssignmentId = e.StudentAssignmentId,
                })
            );

            return response;
        }

        //public override async Task<GrpcLessonProgressResponse> UpdateLessonProgress(
        //    UpdateLessonProgressRequest request,
        //    ServerCallContext context
        //)
        //{
        //    // Map request gRPC → domain model
        //    var command = new UpdateLessonProgressCommand
        //    {
        //        EnrollmentId = request.EnrollmentId,
        //        LessonId = request.LessonId,
        //    };
        //    var result = await _mediator.Send(command);
        //    // Map result domain → gRPC
        //    var response = new GrpcLessonProgressResponse
        //    {
        //        LessonProgress = new LessonProgressModel
        //        {
        //            Id = result.Id,
        //            LessonId = result.LessonId,
        //            CompletedAt = result.CompletedAt?.ToUtcTimestamp(),
        //            Status = result.Status,
        //        },
        //    };
        //    return response;
        //}

        public override async Task<GrpcSectionProgressResponse> UpdateSectionProgress(
            UpdateSectionProgressRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain model
            var command = new UpdateSectionProgressCommand
            {
                EnrollmentId = request.EnrollmentId,
                LessonId = request.LessonId,
                SectionId = request.SectionId,
                Status = request.Status.ToEnumOrDefault(Domain.Enums.ProgressStatus.InProgress)
            };

            var result = await _mediator.Send(command);

            // Map result domain → gRPC
            var response = new GrpcSectionProgressResponse
            {
                SectionProgress = new SectionProgressModel
                {
                    Id = result.Id,
                    SectionId = result.SectionId,
                    CompletedAt = result.CompletedAt?.ToUtcTimestamp(),
                    Status = result.Status,
                },
            };
            return response;
        }

        public async override Task<GrpcClassroomStudentProgressResponse> GetStudentProgressByClassroomId(GetStudentProgressByClassroomIdRequest request, ServerCallContext context)
        {
            var query = new GetStudentProgressByClassroomIdQuery
            {
                ClassroomId = request.ClassroomId,
                CourseId = request.CourseId
            };
            return await _mediator.Send(query);
        }
    }
}
