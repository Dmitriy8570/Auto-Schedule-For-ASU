namespace Application.Common.Interfaces;

/// <summary>
/// Выполняет операцию записи в транзакции с уровнем изоляции SERIALIZABLE и автоматическим
/// повтором при ошибке сериализации. Это ключевой механизм защиты от коллизий при
/// одновременной работе нескольких сотрудников бюро расписаний: СУБД гарантирует, что
/// параллельные транзакции дают тот же результат, что и некоторая их последовательная
/// очередь, а конфликтующая транзакция откатывается и повторяется заново. Тем самым
/// исключаются «потерянные обновления» и наложения занятий, характерные для прежней работы
/// с общими Excel-файлами и для платформы «1С» при одновременном сохранении.
/// </summary>
public interface ITransactionRunner
{
    /// <summary>Выполнить операцию с результатом в SERIALIZABLE-транзакции (с повтором при сериализационном конфликте).</summary>
    Task<T> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);

    /// <summary>Выполнить операцию без результата в SERIALIZABLE-транзакции (с повтором при сериализационном конфликте).</summary>
    Task ExecuteSerializableAsync(
        Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}
