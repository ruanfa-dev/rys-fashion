using System.Data;

using Ardalis.GuardClauses;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Serilog;

using UseCases.Common.Persistence.Context;

namespace Infrastructure.Persistence.Contexts;

public sealed class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly IApplicationDbContext _context;
    private readonly SemaphoreSlim _transactionSemaphore;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(IApplicationDbContext context)
    {
        _context = Guard.Against.Null(context, nameof(context));
        _transactionSemaphore = new SemaphoreSlim(1, 1);

        Log.Information("UnitOfWork initialized for context type: {ContextType}", context.GetType().Name);
    }

    public IApplicationDbContext Context => _context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            Log.Debug("Saving changes to database context");
            var result = await _context.SaveChangesAsync(cancellationToken);
            Log.Information("Successfully saved {ChangesCount} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save changes to database context");
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
    }

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _transactionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_transaction != null)
            {
                Log.Warning("Attempted to begin transaction while another is active. Current ID: {TransactionId}",
                    _transaction.TransactionId);
                throw new InvalidOperationException($"Transaction already in progress: {_transaction.TransactionId}");
            }

            // Check for ambient transaction
            if (System.Transactions.Transaction.Current != null)
            {
                Log.Information("Ambient transaction detected. Using existing transaction context.");
            }

            Log.Information("Beginning transaction with isolation level: {IsolationLevel}", isolationLevel);
            _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            Log.Information("Transaction started with ID: {TransactionId}", _transaction.TransactionId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to begin transaction with isolation level: {IsolationLevel}", isolationLevel);
            throw;
        }
        finally
        {
            _transactionSemaphore.Release();
        }
    }
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _transactionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_transaction == null)
            {
                Log.Warning("Attempted to commit transaction when no transaction is in progress");
                throw new InvalidOperationException("No transaction is in progress.");
            }

            var transactionId = _transaction.TransactionId;
            Log.Information("Committing database transaction with ID: {TransactionId}", transactionId);

            await _transaction.CommitAsync(cancellationToken);
            Log.Information("Successfully committed database transaction with ID: {TransactionId}", transactionId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to commit database transaction");
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            _transactionSemaphore.Release();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _transactionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_transaction == null)
            {
                Log.Warning("Attempted to rollback transaction when no transaction is in progress");
                throw new InvalidOperationException("No transaction is in progress.");
            }

            var transactionId = _transaction.TransactionId;
            Log.Information("Rolling back database transaction with ID: {TransactionId}", transactionId);

            await _transaction.RollbackAsync(cancellationToken);
            Log.Information("Successfully rolled back database transaction with ID: {TransactionId}", transactionId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to rollback database transaction");
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            _transactionSemaphore.Release();
        }
    }

    public async Task<int> ExecuteSqlAsync(string sql, params object[] parameters)
    {
        return await ExecuteSqlAsync(sql, CancellationToken.None, parameters);
    }

    public async Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken, params object[] parameters)
    {
        ThrowIfDisposed();
        Guard.Against.NullOrWhiteSpace(sql);

        try
        {
            Log.Debug("Executing SQL: {Sql}", sql);
            var result = await _context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
            Log.Information("SQL execution completed. Rows affected: {RowsAffected}", result);
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to execute SQL: {Sql}", sql);
            throw;
        }
    }

    // Add transaction state properties
    public bool HasActiveTransaction => _transaction != null;
    public Guid? CurrentTransactionId => _transaction?.TransactionId;

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            Log.Warning("Attempted to use disposed UnitOfWork instance");
            throw new ObjectDisposedException(nameof(UnitOfWork));
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            Log.Information("Disposing UnitOfWork resources");

            try
            {
                _transaction?.Dispose();
                _transactionSemaphore?.Dispose();

                Log.Debug("UnitOfWork resources disposed successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while disposing UnitOfWork resources");
            }

            _disposed = true;
        }
    }
}
