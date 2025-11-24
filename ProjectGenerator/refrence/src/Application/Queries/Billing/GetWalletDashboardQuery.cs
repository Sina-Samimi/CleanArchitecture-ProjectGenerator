using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Billing;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Billing;

public sealed record GetWalletDashboardQuery(string UserId, int TransactionsLimit = 20, int InvoiceLimit = 6) : IQuery<WalletDashboardDto>;

public sealed class GetWalletDashboardQueryHandler : IQueryHandler<GetWalletDashboardQuery, WalletDashboardDto>
{
    private const string DefaultCurrency = "IRT";

    private readonly IWalletRepository _walletRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IShoppingCartRepository _shoppingCartRepository;

    public GetWalletDashboardQueryHandler(
        IWalletRepository walletRepository,
        IInvoiceRepository invoiceRepository,
        IShoppingCartRepository shoppingCartRepository)
    {
        _walletRepository = walletRepository;
        _invoiceRepository = invoiceRepository;
        _shoppingCartRepository = shoppingCartRepository;
    }

    public async Task<Result<WalletDashboardDto>> Handle(GetWalletDashboardQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<WalletDashboardDto>.Failure("شناسه کاربر معتبر نیست.");
        }

        var transactionsLimit = request.TransactionsLimit <= 0 ? 20 : Math.Min(request.TransactionsLimit, 100);
        var invoiceLimit = request.InvoiceLimit <= 0 ? 6 : Math.Min(request.InvoiceLimit, 20);

        var account = await _walletRepository.GetByUserIdWithTransactionsAsync(request.UserId.Trim(), transactionsLimit, cancellationToken);

        var summary = account is not null
            ? account.ToSummaryDto()
            : new WalletSummaryDto(0m, DefaultCurrency, false, DateTimeOffset.UtcNow);

        var transactions = account is not null
            ? account.Transactions
                .OrderByDescending(transaction => transaction.OccurredAt)
                .Take(transactionsLimit)
                .Select(transaction => transaction.ToListItemDto())
                .ToArray()
            : Array.Empty<WalletTransactionListItemDto>();

        var invoices = await _invoiceRepository.GetListByUserAsync(request.UserId.Trim(), invoiceLimit, cancellationToken);
        var invoiceDtos = invoices
            .Select(invoice => invoice.ToWalletSnapshotDto())
            .ToArray();

        var cart = await _shoppingCartRepository.GetByUserIdAsync(request.UserId.Trim(), cancellationToken);
        var cartDto = cart is not null && !cart.IsEmpty
            ? cart.ToWalletDto()
            : null;

        var dashboard = new WalletDashboardDto(
            summary,
            transactions,
            invoiceDtos,
            cartDto,
            DateTimeOffset.UtcNow);

        return Result<WalletDashboardDto>.Success(dashboard);
    }
}
