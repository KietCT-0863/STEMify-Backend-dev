using Notification.Domain.Entities;

namespace Notification.Application.Common.Interfaces.Services
{
    public interface IFCMNotificationService
    {
        Task<bool> SendNotificationAsync(FCMessage message);
    }
}
