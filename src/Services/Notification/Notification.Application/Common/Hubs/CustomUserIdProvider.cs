using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Common.Hubs
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        private readonly ILogger<CustomUserIdProvider> _logger;

        public CustomUserIdProvider(ILogger<CustomUserIdProvider> logger)
        {
            _logger = logger;
        }

        public string GetUserId(HubConnectionContext connection)
        {
            var userId = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("CustomUserIdProvider called. Extracted sub: {UserId}", userId);
            return userId;
        }
    }
}
