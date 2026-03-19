using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Features.CourseEnrollments.Commands.DeleteCourseEnrollment;
using Classroom.Application.Features.CourseEnrollments.Commands.UpdateCourseEnrollment;
using Classroom.Application.Features.CourseEnrollments.Queries.GetCourseEnrollmentList;
using Classroom.Domain.Enums;
using Grpc.Core;
using MediatR;
using Shared.Extensions;
using Shared.Protos.Classroom;

namespace Classroom.API.Services
{
    public class CourseEnrollmentGrpcService : GrpcCourseEnrollment.GrpcCourseEnrollmentBase
    {
        private readonly IMediator _mediator;

        public CourseEnrollmentGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Get paged enrollments with optional filters: studentId, courseId, status, search (by student name or email)
        public override async Task<GrpcPagedCourseEnrollmentsResponse> GetPagedCourseEnrollments(
            GetCourseEnrollmentsRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain params
            var queryParams = request.ToEnrollmentParams();

            var result = await _mediator.Send(new GetCourseEnrollmentListQuery(queryParams));

            // Map result domain → gRPC
            var response = new GrpcPagedCourseEnrollmentsResponse
            {
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
            };
            response.Items.AddRange(result.Items.Select(e => e.ToGrpcEnrollmentModel()));

            return response;
        }

        public override async Task<GrpcCourseEnrollmentResponse> CreateCourseEnrollment(
            CreateCourseEnrollmentRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain model
            var command = request.ToCreatEnrollmentCommand();
            var result = await _mediator.Send(command);
            // Map result domain → gRPC
            var response = new GrpcCourseEnrollmentResponse
            {
                CourseEnrollment = result.ToGrpcEnrollmentModel(),
            };
            return response;
        }
        // Delete is a soft delete, sets status to Dropped
        public override async Task<DeleteCourseEnrollmentResponse> DeleteCourseEnrollment(
            DeleteCourseEnrollmentRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain model
            var command = new DeleteCourseEnrollmentCommand(request.Id);
            var result = await _mediator.Send(command);

            var response = new DeleteCourseEnrollmentResponse { Success = result };
            return response;
        }

        public override async Task<GrpcCourseEnrollmentResponse> UpdateCourseEnrollment(
            UpdateCourseEnrollmentRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateCourseEnrollmentCommand
            {
                Id = request.Id,
                Status = request.Status.ToEnumOrNull<EnrollmentStatus>(),
            };
            var result = await _mediator.Send(command);
            var response = new GrpcCourseEnrollmentResponse
            {
                CourseEnrollment = result.ToGrpcEnrollmentModel(),
            };
            return response;
        }
    }
}
