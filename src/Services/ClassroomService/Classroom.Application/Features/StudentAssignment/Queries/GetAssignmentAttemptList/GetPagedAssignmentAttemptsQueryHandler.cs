using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Models.AssignmentAttemptModel;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Classroom.Application.Features.StudentAssignment.Queries.GetAssignmentAttemptList
{
    public class GetPagedAssignmentAttemptsQueryHandler : IRequestHandler<GetPagedAssignmentAttemptsQuery, GrpcPagedAssignmentAttemptsResponse>
    {
        private readonly ILogger<GetPagedAssignmentAttemptsQueryHandler> _logger;
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcAssignmentClient _grpcAssignmentClient;

        public GetPagedAssignmentAttemptsQueryHandler(
            ILogger<GetPagedAssignmentAttemptsQueryHandler> logger,
            IGrpcAssignmentClient grpcAssignmentClient,
            IClassroomUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _grpcAssignmentClient = grpcAssignmentClient;
        }

        public async Task<GrpcPagedAssignmentAttemptsResponse> Handle(GetPagedAssignmentAttemptsQuery request, CancellationToken cancellationToken)
        {
            var from = request.FromDate;
            var to = request.ToDate;

            Expression<Func<Domain.Entities.AssignmentAttempt, bool>> predicate = qa =>
                (string.IsNullOrEmpty(request.StudentId) || qa.StudentAssignment.StudentId == request.StudentId) &&
                (!from.HasValue || qa.SubmittedAt >= from.Value) &&
                (!to.HasValue || qa.SubmittedAt <= to.Value);

            Expression<Func<Domain.Entities.AssignmentAttempt, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "submittedat" => qa => qa.SubmittedAt,
                    "totalscore" => qa => qa.TotalScore,
                    "attemptnumber" => qa => qa.AttemptNumber,
                    _ => qa => qa.Id,
                };

            // Include StudentAssignment so we can project StudentId and AssignmentId
            Func<IQueryable<Domain.Entities.AssignmentAttempt>, IQueryable<AssignmentAttemptDto>> projectionFunc = query =>
               query
                   .Include(q => q.StudentAssignment)
                   .Select(q => new AssignmentAttemptDto
                   {
                       Id = q.Id,
                       AssignmentId = q.StudentAssignment.AssignmentId,
                       StudentAssignmentId = q.StudentAssignmentId,
                       Status = q.Status,
                       TotalScore = q.TotalScore,
                       AttemptNumber = q.AttemptNumber
                   });

            var paged = await _unitOfWork.AssignmentAttempts.GetByPageFilter(
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
            var quizIds = paged.Items.Select(i => i.AssignmentId).Distinct().ToArray();

            var quizTasks = quizIds
                .Select(async id =>
                {
                    try
                    {
                        var res = await _grpcAssignmentClient.GetAssignmentByIdAsync(id);
                        return (id, res);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch quiz {AssignmentId} from resource service.", id);
                        return (id, (GrpcAssignmentModel?)null);
                    }
                })
                .ToArray();

            var quizResults = await Task.WhenAll(quizTasks);

            var quizMap = quizResults.ToDictionary(x => x.id, x => x.Item2);

            var items = paged.Items.Select(dto =>
            {
                quizMap.TryGetValue(dto.AssignmentId, out var quizRes);

                return new GrpcAssignmentAttemptModel
                {
                    Id = dto.Id,
                    StudentAssignmentId = dto.StudentAssignmentId,
                    SubmittedAt = dto.SubmittedAt.HasValue
                        ? Timestamp.FromDateTime(dto.SubmittedAt.Value.ToUniversalTime())
                        : null,
                    TotalScore = (double)dto.TotalScore,
                    Status = dto.Status.ToString(),
                    AttemptNumber = dto.AttemptNumber,
                    Assignment = quizRes != null
                        ? new GrpcAssignment
                        {
                            Id = quizRes.Id,
                            PassingScore = quizRes.PassingScore,
                            ContentId = quizRes.ContentId,
                            TotalScore = quizRes.TotalScore,
                            DurationDays = quizRes.DurationDays
                        }
                        : null
                };
            }).ToList();

            var response = new GrpcPagedAssignmentAttemptsResponse
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