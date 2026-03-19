using Shared.SeedWork;

namespace Notification.Application.Models
{
    public class NotificationParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public string? UserId { get; set; }
        public bool? IsRead { get; set; }
    }
}
