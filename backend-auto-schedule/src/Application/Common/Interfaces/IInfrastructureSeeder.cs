namespace Application.Common.Interfaces;

/// <summary>
/// Сидер инфраструктуры расписания: наполняет данные, которых нет в ММИС, но без которых
/// у солвера нет осей модели — учебные корпуса с аудиториями (ось «аудитории») и для каждой
/// недели семестра рабочие дни с парами (ось «время»). Без них генерация возвращает Infeasible
/// за 0.00 с. Все операции идемпотентны: повторный прогон не создаёт дубликатов.
/// </summary>
public interface IInfrastructureSeeder
{
    /// <summary>Наполнить аудиторный фонд (корпуса + аудитории), если он пуст.</summary>
    Task<int> SeedFacilitiesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Наполнить каталог оборудования (если пуст) и оснастить им часть аудиторий, чтобы
    /// ограничение оборудования в солвере имело данные. Идемпотентно. Возвращает число типов оборудования.
    /// </summary>
    Task<int> SeedEquipmentAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Наполнить веса мягких ограничений (по одной записи на тип) значениями по умолчанию, если их
    /// ещё нет, — чтобы вкладка «Веса» была настраиваемой, а солвер имел осмысленные штрафы. Идемпотентно.
    /// </summary>
    Task<int> SeedConstraintWeightsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Достроить календарную сетку: для каждой недели без рабочих дней создать дни (Пн–Сб)
    /// и пары (1..N) согласно конфигурации. Возвращает число обработанных недель.
    /// </summary>
    Task<int> SeedCalendarGridAsync(CancellationToken cancellationToken);
}
