using Ardalis.Specification;
using Product.Domain.Entities;

namespace Product.Application.Specifications
{
    public class KitByIdSpecification : Specification<KitProduct>
    {
        public KitByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id)
                .Include(c => c.KitImages)
                .Include(c => c.KitComponents)
                .ThenInclude(kc => kc.Component);
        }
    }

    public class KitWithIncludesSpecification : Specification<KitProduct>
    {
        public KitWithIncludesSpecification()
        {
            Query.Include(x => x.KitImages);
        }
    }
}
