using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentQuiz.Queries.GetQuizAttemptList
{
    public class GetPagedQuizAttemptsQuery : IRequest<GrpcPagedQuizAttemptsResponse>
    {
        public string? Search { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public string? OrderBy { get; set; }
        public bool IsDescending { get; set; }
        public string? StudentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
