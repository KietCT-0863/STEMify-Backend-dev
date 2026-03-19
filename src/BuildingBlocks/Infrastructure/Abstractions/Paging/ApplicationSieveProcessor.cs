using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;
using System.Reflection;

namespace Infrastructure.Abstractions.Paging
{
    public class ApplicationSieveProcessor : SieveProcessor
    {
        public ApplicationSieveProcessor(IOptions<SieveOptions> options)
            : base(options) { }

        public ApplicationSieveProcessor(
            IOptions<SieveOptions> options,
            ISieveCustomSortMethods customSortMethods
        )
            : base(options, customSortMethods) { }

        public ApplicationSieveProcessor(
            IOptions<SieveOptions> options,
            ISieveCustomFilterMethods customFilterMethods
        )
            : base(options, customFilterMethods) { }

        public ApplicationSieveProcessor(
            IOptions<SieveOptions> options,
            ISieveCustomSortMethods customSortMethods,
            ISieveCustomFilterMethods customFilterMethods
        )
            : base(options, customSortMethods, customFilterMethods) { }

        protected override SievePropertyMapper MapProperties(SievePropertyMapper mapper)
        {
            return mapper.ApplyConfigurationsFromAssembly(Assembly.GetCallingAssembly());
        }
    }
}
