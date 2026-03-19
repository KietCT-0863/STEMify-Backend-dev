using Contracts.Domains;

namespace Resource.Domain.Entities
{
    public class CurriculumEmulation : EntityBase<int>
    {
        public string EmulationId { get; set; }
        public int CurriculumId { get; set; }

        public virtual Curriculum Curriculum { get; set; }
    }
}
