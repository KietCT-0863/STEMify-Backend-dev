using Contracts.Domains;

namespace Identity.Domain.Entities
{
    public class JobRole : EntityBase<int>
    {
        public string Name { get; set; } = string.Empty;
    }
}
