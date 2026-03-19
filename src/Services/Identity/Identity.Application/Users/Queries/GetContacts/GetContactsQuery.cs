using Identity.Domain.Enums;
using MediatR;
using Shared.Protos.User;
using Shared.SeedWork;

namespace Identity.Application.Users.Queries.GetContacts
{
    public class GetContactsQuery : PagingRequestParam, IRequest<PagedContactList>
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        //public int? PageNumber { get; set; }
        //public int? PageSize { get; set; }
        public string? OrderBy { get; set; }
        public ContactStatus? Status { get; set; }
    }
}
