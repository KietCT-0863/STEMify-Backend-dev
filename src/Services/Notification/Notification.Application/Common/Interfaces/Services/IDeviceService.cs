using Notification.Domain.Entities;

namespace Notification.Application.Common.Interfaces.Services
{
    public interface IDeviceService
    {
        Task<bool> AddDeviceAsync(UserDevice device);
        Task<List<string>> GetDeviceTokensForUser(string userId);
        Task<List<string>> GetDeviceTokensForSelectedUsers(List<string> userIds);
        Task<List<string>> GetAllDeviceTokens();
    }
}
