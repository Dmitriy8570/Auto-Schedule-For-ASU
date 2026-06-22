using Application.Common.Interfaces;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Тестовая подделка <see cref="ITransactionRunner"/>: выполняет операцию напрямую, без реальной
/// транзакции и СУБД. Используется в быстрых модульных тестах хендлеров записи расписания, где
/// репозиторий тоже подменён in-memory подделкой.
/// </summary>
internal sealed class FakeTransactionRunner : ITransactionRunner
{
    public Task<T> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
        => operation(cancellationToken);

    public Task ExecuteSerializableAsync(
        Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
        => operation(cancellationToken);
}
