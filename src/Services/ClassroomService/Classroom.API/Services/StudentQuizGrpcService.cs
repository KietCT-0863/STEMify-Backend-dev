using Classroom.Application.Features.StudentQuiz.Queries.GetStudentQuizById;
using Classroom.Application.Features.StudentQuiz.Queries.GetStudentQuizList;
using Classroom.Domain.Enums;
using Grpc.Core;
using Infrastructure.Common.Paging;
using MediatR;
using Shared.Extensions;
using Shared.Protos.Classroom;

namespace Classroom.API.Services
{
    public class StudentQuizGrpcService : GrpcStudentQuiz.GrpcStudentQuizBase
    {
        private readonly IMediator _mediator;
        public StudentQuizGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }
        public override async Task<GrpcPagedStudentQuizzesResponse> GetPagedStudentQuizByClassroom(GetStudentQuizParams request, ServerCallContext context)
        {
            var query = new GetStudentQuizByClassroomQuery
            {
                ClassroomId = request.ClassroomId,
                Status = request.Status.ToEnumOrNull<StudentQuizStatus>(),
                PageRequest = new PageRequest
                {
                    PageNumber = request.PageNumber ?? 1,
                    PageSize = request.PageSize ?? 10
                }
            };
            return await _mediator.Send(query);
        }

        public override async Task<GrpcStudentQuizResponse> GetStudentQuizById(GetStudentQuizByIdRequest request, ServerCallContext context)
        {
            var query = new GetStudentQuizByIdQuery
            {
                Id = request.Id
            };
            return await _mediator.Send(query);
        }
    }
}
