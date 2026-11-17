using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.UOW;

public class UnitOfWork: IUnitOfWork
{

    private readonly ApiContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(ApiContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw BuildUniqueConstraintConflict(ex);
        }
    }

    public async Task BeginTransaction(CancellationToken cancellationToken = default)
    {
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null) return;

        try
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                throw BuildUniqueConstraintConflict(ex);
            }

            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if(_currentTransaction is null) return;
        
        await DisposeTransactionAsync();
    }

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    #region Helper Methods

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }

    private ConflictException BuildUniqueConstraintConflict(DbUpdateException exception)
    {
        var postgresException = (PostgresException)exception.InnerException!;

        _logger.LogWarning(
            "Benzersizlik kısıtı ihlal edildi; istek 409 ile reddedildi. Table:{TableName}, Constraint:{ConstraintName}",
            postgresException.TableName, postgresException.ConstraintName);

        return new ConflictException("Bu kayıt zaten mevcut. Lütfen farklı bir değerle tekrar deneyiniz.");
    }

    #endregion
}
