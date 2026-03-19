using Contracts.Abstractions.Persistence;

namespace Notification.Application.Common.Interfaces.Repositories;

public interface INotificationRepository
    : IRepositoryBaseAsync<Domain.Entities.Notification, int> { }
