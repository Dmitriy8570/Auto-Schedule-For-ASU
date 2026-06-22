using System.Data;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// EF Core-реализация <see cref="ITransactionRunner"/> для PostgreSQL. Оборачивает операцию в
/// транзакцию уровня <see cref="IsolationLevel.Serializable"/> поверх стратегии выполнения
/// (<see cref="DbContext.Database"/> + <c>EnableRetryOnFailure</c>): при ошибке сериализации
/// (PostgreSQL SQLSTATE 40001) транзакция автоматически повторяется. Перед каждой попыткой
/// очищается трекер изменений, чтобы повтор начинался с чистого состояния, а все чтения и
/// записи операции попадали в одну согласованную транзакцию.
/// </summary>
public sealed class TransactionRunner(ApplicationDbContext context) : ITransactionRunner
{
    public async Task<T> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Стратегия выполнения может повторить делегат — стартуем с чистого трекера.
            context.ChangeTracker.Clear();

            await using var transaction = await context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var result = await operation(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    public Task ExecuteSerializableAsync(
        Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
        => ExecuteSerializableAsync(async ct =>
        {
            await operation(ct);
            return true;
        }, cancellationToken);
}
