using Contracts.Abstractions.Persistence.EfCore;
using Notification.Application.Common.Interfaces.Repositories;

namespace Notification.Application.Common.Interfaces
{
    public interface INotificationUnitOfWork : IEfUnitOfWork
    {
        INotificationRepository Notifications { get; }
        IDeviceRepository Devices { get; }
    }
}
