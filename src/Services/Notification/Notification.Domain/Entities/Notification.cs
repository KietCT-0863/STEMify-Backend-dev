using System.ComponentModel.DataAnnotations;
using Contracts.Domains;

namespace Notification.Domain.Entities
{
    public class Notification : EntityAuditBase<int>
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public bool IsRead { get; set; } = false;
        public string? ClickUrl { get; set; }
    }
}
