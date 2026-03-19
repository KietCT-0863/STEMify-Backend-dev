using Notification.Application.Common.Interfaces;
using Notification.Application.Common.Interfaces.Services;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly INotificationUnitOfWork _unitOfWork;

        public DeviceService(INotificationUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> AddDeviceAsync(UserDevice device)
        {
            var result = false;

            // Check if the device has been added before
            bool isExisted = await CheckExistedDevice(device.FCMToken);

            // If the device toke was saved before then skip the adding
            if (!isExisted)
            {
                await _unitOfWork.Devices.AddAsync(device);
                result = (await _unitOfWork.SaveChangesAsync()) > 0;
            }
            else
                result = true;

            return result;
        }

        public async Task<List<string>> GetAllDeviceTokens()
        {
            var devices = await _unitOfWork.Devices.GetAllAsync();

            // Project to devices to a list of tokens
            var deviceTokens = devices.Select(d => d.FCMToken).ToList();

            return deviceTokens;
        }

        public async Task<List<string>> GetDeviceTokensForUser(string userId)
        {
            // Get all devices that the current user logged onto
            // var spec = new DeviceSpecification(userId);
            var devices = await _unitOfWork.Devices.FindAsync(d => d.UserId == userId);

            // Project to devices to a list of tokens
            var deviceTokens = devices.Select(d => d.FCMToken).ToList();

            return deviceTokens;
        }

        public async Task<List<string>> GetDeviceTokensForSelectedUsers(List<string> userIds)
        {
            var deviceTokens = new List<string>();
            foreach (var id in userIds)
            {
                var userDeviceTokens = await GetDeviceTokensForUser(id);
                deviceTokens.AddRange(userDeviceTokens);
            }
            return deviceTokens;
        }

        private async Task<bool> CheckExistedDevice(string token)
        {
            //var spec = new DeviceSpecification(token);
            var device = await _unitOfWork.Devices.FindOneAsync(d => d.FCMToken == token);
            return (device != null);
        }
    }
}
