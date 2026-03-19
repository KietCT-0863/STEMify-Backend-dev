using Ardalis.Specification;
using Identity.Domain.Entities;

namespace Identity.Application.Specifications.Contacts
{
    public class GetContactByIdSpecification : Specification<Contact>
    {
        public GetContactByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id)
                .Include(c => c.JobRole);
        }
    }
}
