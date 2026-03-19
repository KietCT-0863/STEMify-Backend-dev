using Contracts.Abstractions.Persistence;
using Notification.Domain.Entities;

namespace Notification.Application.Common.Interfaces.Repositories
{
    public interface IDeviceRepository : IRepositoryBaseAsync<UserDevice, int> { }
}
