using Infrastructure.Abstractions.Persistence.EfCore;
using Notification.Application.Common.Interfaces.Repositories;
using Sieve.Services;

namespace Notification.Infrastructure.Persistence.Repositories;

public class NotificationRepository
    : EfRepositoryBase<NotificationDbContext, Domain.Entities.Notification, int>,
        INotificationRepository
{
    public NotificationRepository(NotificationDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
}
