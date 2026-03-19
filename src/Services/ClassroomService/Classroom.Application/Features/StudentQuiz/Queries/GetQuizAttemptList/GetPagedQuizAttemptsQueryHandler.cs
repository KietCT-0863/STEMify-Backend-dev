using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Models.QuizAttemptModel;
using Classroom.Domain.Entities;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Classroom.Application.Features.StudentQuiz.Queries.GetQuizAttemptList
{
    public class GetPagedQuizAttemptsQueryHandler : IRequestHandler<GetPagedQuizAttemptsQuery, GrpcPagedQuizAttemptsResponse>
    {
        private readonly ILogger<GetPagedQuizAttemptsQueryHandler> _logger;
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcQuizClient _grpcQuizClient;

        public GetPagedQuizAttemptsQueryHandler(
            ILogger<GetPagedQuizAttemptsQueryHandler> logger,
            IGrpcQuizClient grpcQuizClient,
            IClassroomUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _grpcQuizClient = grpcQuizClient;
        }

        public async Task<GrpcPagedQuizAttemptsResponse> Handle(GetPagedQuizAttemptsQuery request, CancellationToken cancellationToken)
        {
            var from = request.FromDate;
            var to = request.ToDate;

            Expression<Func<QuizAttempt, bool>> predicate = qa =>
                (string.IsNullOrEmpty(request.StudentId) || qa.StudentQuiz.StudentId == request.StudentId) &&
                (!from.HasValue || qa.StartedAt >= from.Value) &&
                (!to.HasValue || qa.StartedAt <= to.Value);

            Expression<Func<QuizAttempt, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "startedat" => qa => qa.StartedAt,
                    "totalscore" => qa => qa.TotalScore,
                    "attemptnumber" => qa => qa.AttemptNumber,
                    _ => qa => qa.Id,
                };

            // Include StudentQuiz so we can project StudentId and QuizId
            Func<IQueryable<QuizAttempt>, IQueryable<QuizAttemptDto>> projectionFunc = query =>
               query
                   .Include(q => q.StudentQuiz)
                   .Select(q => new QuizAttemptDto
                   {
                       Id = q.Id,
                       QuizId = q.StudentQuiz.QuizId,
                       StudentQuizId = q.StudentQuizId,
                       Status = q.Status,
                       TotalScore = q.TotalScore,
                       StartedAt = q.StartedAt,
                       CompletedAt = q.CompletedAt,
                       AttemptNumber = q.AttemptNumber
                   });

            var paged = await _unitOfWork.QuizAttempts.GetByPageFilter(
                pageRequest: new PageRequest
                {
                    PageNumber = request.PageNumber ?? 1,
                    PageSize = request.PageSize ?? 10,
                },
                projectionFunc: projectionFunc,
                sortExpression: sortExpression,
                predicate: predicate,
                descending: request.IsDescending,
                cancellationToken: cancellationToken
            );

            // Fetch quiz metadata in parallel for distinct quiz ids on the page
            var quizIds = paged.Items.Select(i => i.QuizId).Distinct().ToArray();

            var quizTasks = quizIds
                .Select(async id =>
                {
                    try
                    {
                        var res = await _grpcQuizClient.GetQuizByIdAsync(id);
                        return (id, res);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch quiz {QuizId} from resource service.", id);
                        return (id, (QuizResponse?)null);
                    }
                })
                .ToArray();

            var quizResults = await Task.WhenAll(quizTasks);

            var quizMap = quizResults.ToDictionary(x => x.id, x => x.Item2);

            var items = paged.Items.Select(dto =>
            {
                quizMap.TryGetValue(dto.QuizId, out var quizRes);

                return new GrpcQuizAttemptModel
                {
                    Id = dto.Id,
                    StudentQuizId = dto.StudentQuizId,
                    StartedAt = Timestamp.FromDateTime(dto.StartedAt.ToUniversalTime()),
                    CompletedAt = dto.CompletedAt.HasValue
                        ? Timestamp.FromDateTime(dto.CompletedAt.Value.ToUniversalTime())
                        : null,
                    TotalScore = (double)dto.TotalScore,
                    Status = dto.Status.ToString(),
                    AttemptNumber = dto.AttemptNumber,
                    Quiz = quizRes != null
                        ? new GrpcQuizModel
                        {
                            Id = quizRes.Id,
                            Title = quizRes.Title,
                            TotalMarks = quizRes.TotalMarks,
                            PassingMarks = quizRes.PassingMarks
                        }
                        : null
                };
            }).ToList();

            var response = new GrpcPagedQuizAttemptsResponse
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            response.Items.AddRange(items);

            return response;
        }
    }
}