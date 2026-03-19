using Contracts.Domains;

namespace Notification.Domain.Entities
{
    public class UserDevice : EntityBase<int>
    {
        public string UserId { get; set; }
        public string FCMToken { get; set; }
    }
}
