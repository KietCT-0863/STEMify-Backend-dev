using MediatR;
using Resource.Application.Commands.Category;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Category
{
    public class CreateTopicCommandHandler : IRequestHandler<CreateTopicCommand, CategoryResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateTopicCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CategoryResponse> Handle(
            CreateTopicCommand request,
            CancellationToken cancellationToken
        )
        {
            var category = new Domain.Entities.Topic { Name = request.Name };

            await _unitOfWork.Topics.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CategoryResponse() { Id = category.Id, Name = category.Name };
        }
    }
}
