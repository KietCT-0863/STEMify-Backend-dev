using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Answer;
using Resource.Application.Queries.Answer;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class AnswerGrpcService : AnswerService.AnswerServiceBase
    {
        private readonly IMediator _mediator;

        public AnswerGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<AnswerResponse> CreateAnswer(
            CreateAnswerRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var command = new CreateAnswerCommand
                {
                    Content = request.Content,
                    IsCorrect = request.IsCorrect,
                    QuestionId = request.QuestionId,
                };

                var result = await _mediator.Send(command);
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"CreateAnswer failed: {ex.Message}")
                );
            }
        }

        public override async Task<AnswerResponse> GetAnswer(
            GetAnswerRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var query = new GetAnswerByIdQuery(request.Id);
                var result = await _mediator.Send(query);

                if (result == null)
                    throw new RpcException(
                        new Status(StatusCode.NotFound, $"Answer with ID {request.Id} not found.")
                    );

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"GetAnswer failed: {ex.Message}")
                );
            }
        }

        public override async Task<AnswerResponse> UpdateAnswer(
            UpdateAnswerRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var command = new UpdateAnswerCommand
                {
                    Id = request.Id,
                    Content = request.Content,
                    IsCorrect = request.IsCorrect,
                };

                var result = await _mediator.Send(command);
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"UpdateAnswer failed: {ex.Message}")
                );
            }
        }

        public override async Task<Empty> DeleteAnswer(
            DeleteAnswerRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var command = new DeleteAnswerCommand { Id = request.Id };
                await _mediator.Send(command);

                return new Empty();
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"DeleteAnswer failed: {ex.Message}")
                );
            }
        }

        public override async Task<AnswerList> ListAnswers(Empty request, ServerCallContext context)
        {
            try
            {
                var result = await _mediator.Send(new GetAnswerListQuery());

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListAnswers failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedAnswerList> QueryAnswers(
            QueryAnswersRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var query = new QueryAnswersQuery
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    OrderBy = request.OrderBy,
                    QuestionId = request.QuestionId,
                };
                var result = await _mediator.Send(query);
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"QueryAnswers failed: {ex.Message}")
                );
            }
        }
    }
}
