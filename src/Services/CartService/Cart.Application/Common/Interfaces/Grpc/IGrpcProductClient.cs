using Shared.Protos.Product;

namespace Cart.Application.Common.Interfaces.Grpc
{
    public interface IGrpcProductClient
    {
        Task<ProductResponse> GetProductByIdAsync(int productId);
    }
}
