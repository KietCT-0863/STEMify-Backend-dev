using Infrastructure.Abstractions.Persistence.EfCore;
using Notification.Application.Common.Interfaces.Repositories;
using Notification.Domain.Entities;
using Sieve.Services;

namespace Notification.Infrastructure.Persistence.Repositories
{
    public class DeviceRepository
        : EfRepositoryBase<NotificationDbContext, UserDevice, int>,
            IDeviceRepository
    {
        public DeviceRepository(NotificationDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
