using Payment.Domain.Enums;

namespace Payment.Application.Common.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Domain.Entities.Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Payment?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Payment?> GetByProviderTransactionIdAsync(string providerTransactionId, CancellationToken cancellationToken = default);
        Task<List<Domain.Entities.Payment>> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Payment> AddAsync(Domain.Entities.Payment payment, CancellationToken cancellationToken = default);
        Task UpdateAsync(Domain.Entities.Payment payment, CancellationToken cancellationToken = default);
        Task<Domain.Entities.PaymentTransaction> AddTransactionAsync(Domain.Entities.PaymentTransaction transaction, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Refund> AddRefundAsync(Domain.Entities.Refund refund, CancellationToken cancellationToken = default);
    }
}
