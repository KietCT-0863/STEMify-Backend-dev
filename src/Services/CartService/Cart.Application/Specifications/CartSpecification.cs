using Ardalis.Specification;

namespace Cart.Application.Specifications
{
    public class CartByUserIdSpecification : Specification<Domain.Entities.Cart>
    {
        public CartByUserIdSpecification(string userId)
        {
            Query
                .Include(c => c.CartItems)
                    .Where(c => c.UserId == userId && c.Status == Shared.Enums.CartStatus.Active)
                    .OrderByDescending(c => c.CreatedDate);
        }
    }
}
