using Microsoft.EntityFrameworkCore;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Enums;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _context;

        public PaymentRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<Domain.Entities.Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .Include(p => p.Transactions)
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Domain.Entities.Payment?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .Include(p => p.Transactions)
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
        }

        public async Task<Domain.Entities.Payment?> GetByProviderTransactionIdAsync(string providerTransactionId, CancellationToken cancellationToken = default)
        {
            var payment = await _context.Payments
                .Include(p => p.Transactions)
                .Include(p => p.Refunds)
                .Where(p => p.Transactions.Any(t => t.ProviderTransactionId == providerTransactionId))
                .FirstOrDefaultAsync(cancellationToken);
            return payment;
        }

        public async Task<List<Domain.Entities.Payment>> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .Include(p => p.Transactions)
                .Include(p => p.Refunds)
                .Where(p => p.BuyerId == buyerId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<Domain.Entities.Payment> AddAsync(Domain.Entities.Payment payment, CancellationToken cancellationToken = default)
        {
            await _context.Payments.AddAsync(payment, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return payment;
        }

        public async Task UpdateAsync(Domain.Entities.Payment payment, CancellationToken cancellationToken = default)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Domain.Entities.PaymentTransaction> AddTransactionAsync(Domain.Entities.PaymentTransaction transaction, CancellationToken cancellationToken = default)
        {
            await _context.PaymentTransactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return transaction;
        }

        public async Task<Domain.Entities.Refund> AddRefundAsync(Domain.Entities.Refund refund, CancellationToken cancellationToken = default)
        {
            await _context.Refunds.AddAsync(refund, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return refund;
        }
    }
}
