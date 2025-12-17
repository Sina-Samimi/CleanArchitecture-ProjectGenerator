using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Billing;
using Attar.Application.Interfaces;
using Attar.Domain.Base;
using Attar.Domain.Entities.Billing;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Attar.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<InvoiceRepository> _logger;
    private const int ApplicationLockTimeoutMilliseconds = 15_000;

    public InvoiceRepository(AppDbContext dbContext, ILogger<InvoiceRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken, bool includeDetails = false)
    {
        IQueryable<Invoice> query = _dbContext.Invoices
            .Where(invoice => invoice.Id == id);

        query = includeDetails
            ? query
                .Include(invoice => invoice.ItemsCollection)
                    .ThenInclude(item => item.Attributes)
                .Include(invoice => invoice.TransactionsCollection)
            : query
                .Include(invoice => invoice.ItemsCollection)
                .Include(invoice => invoice.TransactionsCollection);

        return await query.AsTracking().FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Invoice?> GetByIdForUserAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken,
        bool includeDetails = false)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var normalizedUserId = userId.Trim();

        IQueryable<Invoice> query = _dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.Id == id && invoice.UserId == normalizedUserId);

        query = includeDetails
            ? query
                .Include(invoice => invoice.ItemsCollection)
                    .ThenInclude(item => item.Attributes)
                .Include(invoice => invoice.TransactionsCollection)
            : query
                .Include(invoice => invoice.ItemsCollection)
                .Include(invoice => invoice.TransactionsCollection);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Invoice?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return null;
        }

        var normalized = invoiceNumber.Trim();

        return await _dbContext.Invoices
            .Include(invoice => invoice.ItemsCollection)
            .Include(invoice => invoice.TransactionsCollection)
            .AsTracking()
            .FirstOrDefaultAsync(invoice => invoice.InvoiceNumber == normalized, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Invoice>> GetListAsync(InvoiceListFilterDto? filter, CancellationToken cancellationToken)
    {
        IQueryable<Invoice> query = _dbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.ItemsCollection)
            .Include(invoice => invoice.TransactionsCollection);

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(invoice =>
                    invoice.InvoiceNumber.Contains(term) ||
                    invoice.Title.Contains(term) ||
                    (invoice.ExternalReference != null && invoice.ExternalReference.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(filter.UserId))
            {
                var normalizedUser = filter.UserId.Trim();
                query = query.Where(invoice => invoice.UserId == normalizedUser);
            }

            if (filter.Status.HasValue)
            {
                var status = filter.Status.Value;
                query = query.Where(invoice => invoice.Status == status);
            }

            if (filter.IssueDateFrom.HasValue)
            {
                var from = filter.IssueDateFrom.Value;
                query = query.Where(invoice => invoice.IssueDate >= from);
            }

            if (filter.IssueDateTo.HasValue)
            {
                var to = filter.IssueDateTo.Value;
                query = query.Where(invoice => invoice.IssueDate < to);
            }
        }

        query = query
            .OrderByDescending(invoice => invoice.IssueDate)
            .ThenBy(invoice => invoice.InvoiceNumber);

        // Apply pagination if specified
        if (filter?.PageNumber.HasValue == true && filter.PageSize.HasValue)
        {
            var pageNumber = Math.Max(1, filter.PageNumber.Value);
            var pageSize = Math.Max(1, filter.PageSize.Value);
            var skip = (pageNumber - 1) * pageSize;
            query = query.Skip(skip).Take(pageSize);
        }

        var items = await query.ToListAsync(cancellationToken);

        return items;
    }

    public async Task<int> GetListCountAsync(InvoiceListFilterDto? filter, CancellationToken cancellationToken)
    {
        IQueryable<Invoice> query = _dbContext.Invoices.AsNoTracking();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(invoice =>
                    invoice.InvoiceNumber.Contains(term) ||
                    invoice.Title.Contains(term) ||
                    (invoice.ExternalReference != null && invoice.ExternalReference.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(filter.UserId))
            {
                var normalizedUser = filter.UserId.Trim();
                query = query.Where(invoice => invoice.UserId == normalizedUser);
            }

            if (filter.Status.HasValue)
            {
                var status = filter.Status.Value;
                query = query.Where(invoice => invoice.Status == status);
            }

            if (filter.IssueDateFrom.HasValue)
            {
                var from = filter.IssueDateFrom.Value;
                query = query.Where(invoice => invoice.IssueDate >= from);
            }

            if (filter.IssueDateTo.HasValue)
            {
                var to = filter.IssueDateTo.Value;
                query = query.Where(invoice => invoice.IssueDate < to);
            }
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Invoice>> GetListByUserAsync(string userId, int? take, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<Invoice>();
        }

        var normalized = userId.Trim();

        IQueryable<Invoice> query = _dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.UserId == normalized)
            .Include(invoice => invoice.ItemsCollection)
            .Include(invoice => invoice.TransactionsCollection)
            .OrderByDescending(invoice => invoice.IssueDate)
            .ThenBy(invoice => invoice.InvoiceNumber);

        if (take.HasValue && take.Value > 0)
        {
            query = query.Take(take.Value);
        }

        var items = await query.ToListAsync(cancellationToken);
        return items;
    }
    
    public async Task<Invoice?> GetByTrackingHashAsync(int trackingHash, CancellationToken cancellationToken)
    {
        if (trackingHash == 0)
        {
            return null;
        }

        // As trackingHash is produced from invoice.Id.GetHashCode(), search for matching invoice
        // Note: GetHashCode() cannot be translated to SQL, so we need to load invoices and check in memory
        // To improve performance, we limit to recent invoices (last 30 days) and pending invoices
        var recentDate = DateTimeOffset.UtcNow.AddDays(-30);
        var invoices = await _dbContext.Invoices
            .Include(invoice => invoice.TransactionsCollection)
            .AsNoTracking()
            .Where(i => i.CreateDate >= recentDate || 
                       i.Status == InvoiceStatus.Pending)
            .ToListAsync(cancellationToken);

        // Find invoice with matching hash in memory
        var matchingInvoice = invoices.FirstOrDefault(i => Math.Abs(i.Id.GetHashCode()) == trackingHash);
        
        // If not found in recent invoices, search all invoices (fallback for older invoices)
        if (matchingInvoice == null)
        {
            var allInvoices = await _dbContext.Invoices
                .Include(invoice => invoice.TransactionsCollection)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            
            matchingInvoice = allInvoices.FirstOrDefault(i => Math.Abs(i.Id.GetHashCode()) == trackingHash);
        }

        return matchingInvoice;
    }

    public async Task<Invoice?> GetByTrackingNumberAsync(long trackingNumber, CancellationToken cancellationToken)
    {
        if (trackingNumber <= 0)
        {
            return null;
        }

        // Search for invoice by tracking number stored in PaymentTransaction.Reference or Metadata
        // Tracking number is stored as string in Reference field
        var trackingNumberStr = trackingNumber.ToString();
        
        var paymentTransaction = await _dbContext.PaymentTransactions
            .Include(pt => pt.Invoice)
                .ThenInclude(inv => inv.TransactionsCollection)
            .Where(pt => 
                (pt.Reference == trackingNumberStr || 
                 (pt.Metadata != null && pt.Metadata.Contains(trackingNumberStr))) &&
                !pt.Invoice.IsDeleted)
            .OrderByDescending(pt => pt.CreateDate)
            .FirstOrDefaultAsync(cancellationToken);

        // Return invoice with tracking enabled for mutations
        if (paymentTransaction?.Invoice is null)
        {
            return null;
        }

        // Reload invoice with tracking to enable mutations
        return await GetByIdAsync(paymentTransaction.Invoice.Id, cancellationToken, includeDetails: true);
    }

    public async Task<IReadOnlyCollection<InvoiceItem>> GetProductInvoiceItemsAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Array.Empty<InvoiceItem>();
        }

        var items = await _dbContext.InvoiceItems
            .AsNoTracking()
            .Include(item => item.Invoice)
            .Where(item =>
                item.ItemType == InvoiceItemType.Product &&
                item.ReferenceId.HasValue &&
                item.ReferenceId.Value == productId &&
                !item.Invoice.IsDeleted)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        await _dbContext.Invoices.AddAsync(invoice, cancellationToken);
        await EnsureNewBillingEntitiesTrackedAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var entry = _dbContext.Entry(invoice);
        if (entry.State == EntityState.Detached)
        {
            _dbContext.Invoices.Attach(invoice);
            entry = _dbContext.Entry(invoice);
        }

        await EnsureNewBillingEntitiesTrackedAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<TResult>> MutateAsync<TResult>(
        Guid invoiceId,
        bool includeDetails,
        Func<Invoice, CancellationToken, Task<Result<TResult>>> mutation,
        CancellationToken cancellationToken,
        string? notFoundMessage = null)
    {
        if (invoiceId == Guid.Empty)
        {
            return Result<TResult>.Failure("شناسه فاکتور معتبر نیست.");
        }

        ArgumentNullException.ThrowIfNull(mutation);

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            const int maxAttempts = 3;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

                try
                {
                    await AcquireInvoiceLockAsync(invoiceId, transaction.GetDbTransaction(), cancellationToken);

                    // Clear all tracked entities to start fresh
                    DetachTrackedEntities();
                    
                    var invoice = await GetByIdAsync(invoiceId, cancellationToken, includeDetails);
                    if (invoice is null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result<TResult>.Failure(notFoundMessage ?? "فاکتور مورد نظر یافت نشد.");
                    }

                    var mutationResult = await mutation(invoice, cancellationToken);

                    if (!mutationResult.IsSuccess)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        DetachTrackedEntities();
                        return mutationResult;
                    }

                    await EnsureNewBillingEntitiesTrackedAsync(cancellationToken);
                    await RemoveMissingInvoiceChildrenEntriesAsync(cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    DetachTrackedEntities();
                    return mutationResult;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var resolved = await TryResolveMissingInvoiceChildrenAsync(ex, cancellationToken);

                    await transaction.RollbackAsync(cancellationToken);
                    DetachTrackedEntities();

                    _logger.LogWarning(
                        ex,
                        "Concurrency conflict while updating invoice {InvoiceId} (attempt {Attempt}/{MaxAttempts}). ResolvedMissingChildren={Resolved}",
                        invoiceId,
                        attempt + 1,
                        maxAttempts,
                        resolved);

                    if (resolved)
                    {
                        // Items already removed in database; retry mutation/save with clean state.
                        await Task.Delay(50 * (attempt + 1), cancellationToken);
                        continue;
                    }

                    var conflictDetails = await BuildConcurrencyConflictDetailsAsync(ex, cancellationToken);

                    if (!string.IsNullOrWhiteSpace(conflictDetails))
                    {
                        _logger.LogWarning(
                            "Concurrency conflict details for invoice {InvoiceId}: {Details}",
                            invoiceId,
                            conflictDetails);
                    }

                    if (attempt == maxAttempts - 1)
                    {
                        var message = "خطای همزمانی در بروزرسانی فاکتور.";
                        if (!string.IsNullOrWhiteSpace(conflictDetails))
                        {
                            message += $" جزئیات: {conflictDetails}";
                        }

                        _logger.LogError(
                            ex,
                            "Failed to update invoice {InvoiceId} due to concurrency conflict after {MaxAttempts} attempts. Details: {Details}",
                            invoiceId,
                            maxAttempts,
                            conflictDetails);

                        return Result<TResult>.Failure(message);
                    }
                    
                    // Wait a bit before retry
                    await Task.Delay(100 * (attempt + 1), cancellationToken);
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    DetachTrackedEntities();
                    
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;

                    _logger.LogError(
                        ex,
                        "Database error while updating invoice {InvoiceId}: {Error}",
                        invoiceId,
                        innerMessage);

                    return Result<TResult>.Failure($"خطای دیتابیس در بروزرسانی فاکتور: {innerMessage}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    DetachTrackedEntities();
                    
                    _logger.LogError(
                        ex,
                        "Unexpected error while updating invoice {InvoiceId}",
                        invoiceId);

                    return Result<TResult>.Failure($"خطای غیرمنتظره در بروزرسانی فاکتور: {ex.Message}");
                }
            }

            _logger.LogError(
                "Failed to update invoice {InvoiceId}: exceeded maximum retry attempts ({MaxAttempts}) without success.",
                invoiceId,
                maxAttempts);

            return Result<TResult>.Failure("در بروزرسانی فاکتور خطایی رخ داد. لطفاً مجدداً تلاش کنید.");
        });
    }

    private async Task<string?> BuildConcurrencyConflictDetailsAsync(DbUpdateConcurrencyException exception, CancellationToken cancellationToken)
    {
        if (exception.Entries.Count == 0)
        {
            return null;
        }

        var segments = new List<string>(exception.Entries.Count);

        foreach (var entry in exception.Entries)
        {
            var entityName = entry.Metadata.DisplayName();
            var keyValues = entry.Properties
                .Where(property => property.Metadata.IsPrimaryKey())
                .Select(property => $"{property.Metadata.Name}={property.CurrentValue ?? property.OriginalValue}")
                .DefaultIfEmpty("<بدون کلید>");

            string baseSegment = $"{entityName} [{string.Join(", ", keyValues)}]";

            try
            {
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues is null)
                {
                    segments.Add($"{baseSegment}: رکورد در پایگاه داده یافت نشد (احتمالاً حذف شده است).");
                    continue;
                }

                var differingProperties = entry.Properties
                    .Where(property => property.Metadata.IsPrimaryKey() is false && property.IsModified)
                    .Select(property =>
                    {
                        var propertyName = property.Metadata.Name;
                        var currentValue = entry.CurrentValues[propertyName];
                        var databaseValue = databaseValues[propertyName];
                        return $"{propertyName} (پایگاه داده={databaseValue ?? "null"}, ارسالی={currentValue ?? "null"})";
                    })
                    .Take(5)
                    .ToArray();

                if (differingProperties.Length == 0)
                {
                    segments.Add($"{baseSegment}: رکورد موجود است ولی توسط فرآیند دیگری تغییر یافته است.");
                }
                else
                {
                    segments.Add($"{baseSegment}: تغییرات متناقض -> {string.Join(" | ", differingProperties)}");
                }
            }
            catch (Exception lookupException)
            {
                segments.Add($"{baseSegment}: خطا در بازیابی اطلاعات پایگاه داده ({lookupException.Message}).");
            }
        }

        return segments.Count == 0 ? null : string.Join(" || ", segments);
    }

    private void DetachTrackedEntities()
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private async Task<bool> TryResolveMissingInvoiceChildrenAsync(
        DbUpdateConcurrencyException exception,
        CancellationToken cancellationToken)
    {
        var handledAny = false;

        foreach (var entry in exception.Entries)
        {
            _logger.LogWarning(
                "Resolving concurrency for entry type {EntityType} with state {EntityState}. Keys: {Keys}",
                entry.Entity.GetType().Name,
                entry.State,
                string.Join(", ", entry.Properties
                    .Where(property => property.Metadata.IsPrimaryKey())
                    .Select(property => $"{property.Metadata.Name}={property.CurrentValue ?? property.OriginalValue}")));

            switch (entry.Entity)
            {
                case Invoice:
                    // Allow the parent invoice entry to be retried once child entities are refreshed.
                    continue;
                case InvoiceItem or InvoiceItemAttribute:
                    {
                        var missing = false;

                        try
                        {
                            var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                            missing = databaseValues is null;
                        }
                        catch (InvalidOperationException)
                        {
                            missing = true;
                        }

                        if (!missing)
                        {
                            _logger.LogWarning(
                                "Concurrency entry {EntityType} with Id={EntityId} still exists in database; retry will not proceed.",
                                entry.Entity.GetType().Name,
                                entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue);
                            return false;
                        }

                        _logger.LogWarning(
                            "Detaching missing {EntityType} with Id={EntityId} to retry invoice update.",
                            entry.Entity.GetType().Name,
                            entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue);

                        entry.State = EntityState.Detached;
                        handledAny = true;
                        continue;
                    }
                default:
                    _logger.LogWarning(
                        "Encountered concurrency entry of unsupported type {EntityType}; cannot resolve automatically.",
                        entry.Entity.GetType().Name);
                    return false;
            }
        }

        return handledAny;
    }

    public async Task<bool> ExistsByNumberAsync(string invoiceNumber, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return false;
        }

        var normalized = invoiceNumber.Trim();

        var query = _dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.InvoiceNumber == normalized);

        if (excludeId.HasValue && excludeId.Value != Guid.Empty)
        {
            var excluded = excludeId.Value;
            query = query.Where(invoice => invoice.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private async Task EnsureNewBillingEntitiesTrackedAsync(CancellationToken cancellationToken)
    {
        await MarkNewEntitiesAsAddedAsync<WalletTransaction>(cancellationToken);
        await MarkNewEntitiesAsAddedAsync<PaymentTransaction>(cancellationToken);
    }

    private async Task MarkNewEntitiesAsAddedAsync<TEntity>(CancellationToken cancellationToken)
        where TEntity : class
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries<TEntity>())
        {
            if (entry.State is EntityState.Added)
            {
                continue;
            }

            if (entry.State is EntityState.Detached)
            {
                entry.State = EntityState.Added;
                continue;
            }

            var existsInDatabase = true;

            try
            {
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                existsInDatabase = databaseValues is not null;
            }
            catch (InvalidOperationException)
            {
                existsInDatabase = false;
            }

            if (!existsInDatabase)
            {
                entry.State = EntityState.Added;
            }
        }
    }

    private async Task RemoveMissingInvoiceChildrenEntriesAsync(CancellationToken cancellationToken)
    {
        await DetachIfMissingAsync(_dbContext.InvoiceItems, cancellationToken);
        await DetachIfMissingAsync(_dbContext.InvoiceItemAttributes, cancellationToken);
    }

    private async Task DetachIfMissingAsync<TEntity>(
        DbSet<TEntity> dbSet,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var deletedEntries = _dbContext.ChangeTracker
            .Entries<TEntity>()
            .Where(entry => entry.State == EntityState.Deleted)
            .ToList();

        if (deletedEntries.Count == 0)
        {
            _logger.LogDebug(
                "No deleted entries of type {EntityType} detected before invoice save.",
                typeof(TEntity).Name);
            return;
        }

        var keyProperty = _dbContext.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey();
        if (keyProperty is null || keyProperty.Properties.Count != 1)
        {
            return;
        }

        var primaryKeyName = keyProperty.Properties[0].Name;

        var ids = deletedEntries
            .Select(entry => entry.Property(primaryKeyName).CurrentValue)
            .OfType<Guid>()
            .ToArray();

        if (ids.Length == 0)
        {
            _logger.LogDebug(
                "Deleted entries of type {EntityType} do not expose Guid primary keys; skipping existence check.",
                typeof(TEntity).Name);
            return;
        }

        var existingIds = await dbSet
            .AsNoTracking()
            .Where(entity => ids.Contains(EF.Property<Guid>(entity, primaryKeyName)))
            .Select(entity => EF.Property<Guid>(entity, primaryKeyName))
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<Guid>(existingIds);

        foreach (var entry in deletedEntries)
        {
            if (entry.Property(primaryKeyName).CurrentValue is not Guid id)
            {
                continue;
            }

            if (existingSet.Contains(id))
            {
                continue;
            }

            _logger.LogWarning(
                "Skipping delete for {EntityType} with Id={EntityId} because it no longer exists in the database.",
                typeof(TEntity).Name,
                id);

            entry.State = EntityState.Detached;
        }
    }

    private async Task AcquireInvoiceLockAsync(Guid invoiceId, DbTransaction transaction, CancellationToken cancellationToken)
    {
        var connection = transaction?.Connection;
        if (connection is null)
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @DbPrincipal = 'public', @LockTimeout = @timeout;";
        command.CommandType = CommandType.Text;

        var resultParameter = command.CreateParameter();
        resultParameter.ParameterName = "@result";
        resultParameter.DbType = DbType.Int32;
        resultParameter.Direction = ParameterDirection.Output;
        command.Parameters.Add(resultParameter);

        var resourceParameter = command.CreateParameter();
        resourceParameter.ParameterName = "@resource";
        resourceParameter.DbType = DbType.String;
        resourceParameter.Value = $"invoice:{invoiceId:N}";
        command.Parameters.Add(resourceParameter);

        var timeoutParameter = command.CreateParameter();
        timeoutParameter.ParameterName = "@timeout";
        timeoutParameter.DbType = DbType.Int32;
        timeoutParameter.Value = ApplicationLockTimeoutMilliseconds;
        command.Parameters.Add(timeoutParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var status = resultParameter.Value is int value ? value : -1;

        if (status < 0)
        {
            if (status is -1 or -2 or -3)
            {
                throw new TimeoutException("Timed out acquiring invoice lock.");
            }

            throw new InvalidOperationException("Failed to acquire invoice lock.");
        }
    }
}
