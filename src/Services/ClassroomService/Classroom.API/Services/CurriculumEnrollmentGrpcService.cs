using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Features.CurriculumEnrollments.Commands.DeleteCurriculumEnrollment;
using Classroom.Application.Features.CurriculumEnrollments.Commands.UpdateCurriculumEnrollment;
using Classroom.Application.Features.CurriculumEnrollments.Queries.GetCurriculumEnrollmentList;
using Classroom.Domain.Enums;
using Grpc.Core;
using MediatR;
using Shared.Extensions;
using Shared.Protos.Classroom;

namespace Classroom.API.Services
{
    public class CurriculumEnrollmentGrpcService : GrpcCurriculumEnrollment.GrpcCurriculumEnrollmentBase
    {
        private readonly IMediator _mediator;

        public CurriculumEnrollmentGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcPagedCurriculumEnrollmentsResponse> GetPagedCurriculumEnrollments(
            GetCurriculumEnrollmentsRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain params
            var queryParams = request.ToEnrollmentParams();

            var result = await _mediator.Send(new GetCurriculumEnrollmentListQuery(queryParams));

            // Map result domain → gRPC
            var response = new GrpcPagedCurriculumEnrollmentsResponse
            {
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
            };
            response.Items.AddRange(result.Items.Select(e => e.ToGrpcEnrollmentDetail()));

            return response;
        }

        public override async Task<GrpcCurriculumEnrollmentResponse> CreateCurriculumEnrollment(
            CreateCurriculumEnrollmentRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain model
            var command = request.ToCreatEnrollmentCommand();
            var result = await _mediator.Send(command);
            // Map result domain → gRPC
            var response = new GrpcCurriculumEnrollmentResponse
            {
                CurriculumEnrollment = result.ToGrpcEnrollmentModel(),
            };
            return response;
        }

        public override async Task<DeleteCurriculumEnrollmentResponse> DeleteCurriculumEnrollment(
            DeleteCurriculumEnrollmentRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain model
            var command = new DeleteCurriculumEnrollmentCommand(request.Id);
            var result = await _mediator.Send(command);

            var response = new DeleteCurriculumEnrollmentResponse { Success = result };
            return response;
        }

        public override async Task<GrpcCurriculumEnrollmentResponse> UpdateCurriculumEnrollment(
            UpdateCurriculumEnrollmentRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain model
            var command = new UpdateCurriculumEnrollmentCommand
            {
                Id = request.Id,
                CurriculumId = request.CurriculumId,
                Status = request.Status.ToEnumOrNull<EnrollmentStatus>() ?? EnrollmentStatus.InProgress,
            };
            var result = await _mediator.Send(command);
            // Map result domain → gRPC
            var response = new GrpcCurriculumEnrollmentResponse
            {
                CurriculumEnrollment = result.ToGrpcEnrollmentModel(),
            };
            return response;
        }
    }
}
