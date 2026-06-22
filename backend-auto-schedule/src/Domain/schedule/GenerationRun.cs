using Domain.common;

namespace Domain.schedule;

/// <summary>
/// Запись истории одного запуска автогенерации расписания института: что и как сгенерировалось.
/// Названия института и семестра денормализованы (сохраняются на момент запуска), чтобы история
/// оставалась читаемой даже при последующем переименовании справочников. Список недоразмещённых
/// нагрузок хранится сериализованным в <see cref="UnplacedJson"/> (структуру задаёт слой
/// приложения — домен про неё не знает).
/// </summary>
public class GenerationRun
{
    private GenerationRun() { }

    public Guid Id { get; private set; }

    public Guid SemesterId { get; private set; }
    public string SemesterName { get; private set; } = null!;

    public Guid InstituteId { get; private set; }
    public string InstituteName { get; private set; } = null!;

    /// <summary>Завершилась ли задача успешно (иначе — ошибка, см. <see cref="Error"/>).</summary>
    public bool Succeeded { get; private set; }

    /// <summary>Текстовый статус солвера: Optimal / Feasible / Partial (…​) / Infeasible / Empty.</summary>
    public string Status { get; private set; } = null!;

    /// <summary>Сколько занятий-черновиков создано.</summary>
    public int LessonsCreated { get; private set; }

    /// <summary>Значение целевой функции (сумма штрафов мягких ограничений).</summary>
    public double ObjectiveValue { get; private set; }

    /// <summary>Суммарное время поиска солвера, секунды.</summary>
    public double WallTimeSeconds { get; private set; }

    /// <summary>Число недоразмещённых нагрузок (для быстрого отображения без разбора JSON).</summary>
    public int UnplacedCount { get; private set; }

    /// <summary>Сериализованный список недоразмещённых нагрузок (кто/что/сколько/почему).</summary>
    public string UnplacedJson { get; private set; } = "[]";

    /// <summary>Сообщение об ошибке, если задача упала; иначе null.</summary>
    public string? Error { get; private set; }

    /// <summary>Когда задача поставлена в очередь.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Когда задача завершилась (успехом или ошибкой).</summary>
    public DateTime CompletedAt { get; private set; }

    /// <summary>Создать запись истории завершённого запуска генерации.</summary>
    public static GenerationRun Create(
        Guid id,
        Guid semesterId, string semesterName,
        Guid instituteId, string instituteName,
        bool succeeded, string status,
        int lessonsCreated, double objectiveValue, double wallTimeSeconds,
        int unplacedCount, string unplacedJson, string? error,
        DateTime createdAt, DateTime completedAt) => new()
    {
        Id = Guard.NotEmpty(id, nameof(id)),
        SemesterId = Guard.NotEmpty(semesterId, nameof(semesterId)),
        SemesterName = semesterName ?? string.Empty,
        InstituteId = Guard.NotEmpty(instituteId, nameof(instituteId)),
        InstituteName = instituteName ?? string.Empty,
        Succeeded = succeeded,
        Status = Guard.NotBlank(status, nameof(status)),
        LessonsCreated = Guard.NotNegative(lessonsCreated, nameof(lessonsCreated)),
        ObjectiveValue = objectiveValue,
        WallTimeSeconds = wallTimeSeconds,
        UnplacedCount = Guard.NotNegative(unplacedCount, nameof(unplacedCount)),
        UnplacedJson = unplacedJson ?? "[]",
        Error = error,
        CreatedAt = createdAt,
        CompletedAt = completedAt
    };
}
