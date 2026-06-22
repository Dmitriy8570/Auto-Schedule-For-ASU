using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Builder.BuildSections;
using Application.Solver.Model;

namespace Application.Solver.Builder;

/// <summary>
/// Директор сборки модели расписания: последовательно применяет секционные строители.
/// Порядок важен — переменные создаются первыми, целевая функция фиксируется последней.
/// </summary>
public class ScheduleModelDirector
{
    private readonly IReadOnlyList<IModelSectionBuilder> _builders;

    public ScheduleModelDirector(IReadOnlyList<IModelSectionBuilder> builders)
    {
        _builders = builders;
    }

    /// <summary>Полный набор ограничений из текста диплома (жёсткие + мягкие + целевая функция).</summary>
    public static ScheduleModelDirector CreateDefault(SolverPenaltyWeights? weights = null)
    {
        var w = weights ?? SolverPenaltyWeights.Default;
        return new(new IModelSectionBuilder[]
        {
            // Переменные.
            new VariablesSectionBuilder(),

            // Жёсткие ограничения.
            new TotalHoursSectionBuilder(),
            new IntersectionSectionBuilder(),
            new EquipmentSectionBuilder(),
            new CapacitySectionBuilder(),
            new ShiftSectionBuilder(),
            new BuildingTravelSectionBuilder(),
            new DoubleLessonSectionBuilder(),

            // Мягкие ограничения (штрафы в целевой функции).
            new DailyLessonsLimitSectionBuilder(w),
            new WindowSectionBuilder(),
            new AvailabilitySectionBuilder(),
            new FavoriteBuildingSectionBuilder(w),
            new ParallelismSectionBuilder(w),

            // Целевая функция.
            new ObjectiveSectionBuilder(),
        });
    }

    /// <summary>
    /// Набор для генерации по отдельному институту (декомпозиция «B + C»):
    /// к полному набору добавляются жёсткая блокировка ресурсов, занятых другими
    /// институтами этого семестра, и мягкий якорь к расписанию прошлого семестра.
    /// </summary>
    public static ScheduleModelDirector CreatePerInstitute(SolverPenaltyWeights? weights = null)
    {
        var w = weights ?? SolverPenaltyWeights.Default;
        return new(new IModelSectionBuilder[]
        {
            // Переменные.
            new VariablesSectionBuilder(),

            // Жёсткие ограничения.
            new TotalHoursSectionBuilder(),
            new IntersectionSectionBuilder(),
            new OccupiedResourcesSectionBuilder(), // B: ресурсы других институтов уже заняты.
            new EquipmentSectionBuilder(),
            new CapacitySectionBuilder(),
            new ShiftSectionBuilder(),
            new BuildingTravelSectionBuilder(),
            new DoubleLessonSectionBuilder(),

            // Мягкие ограничения (штрафы в целевой функции).
            new DailyLessonsLimitSectionBuilder(w),
            new WindowSectionBuilder(),
            new AvailabilitySectionBuilder(),
            new FavoriteBuildingSectionBuilder(w),
            new ParallelismSectionBuilder(w),
            new PreviousScheduleAnchorSectionBuilder(w), // C: стабильность к прошлому семестру.

            // Целевая функция.
            new ObjectiveSectionBuilder(),
        });
    }

    /// <summary>
    /// Аварийный (best-effort) набор для одного института: применяется, когда обычная модель
    /// компонента неразрешима (Infeasible) или не решилась за отведённое время (Unknown). Размещение
    /// релаксировано (<see cref="BestEffortPlacementSectionBuilder"/>) — максимизируем число
    /// размещённых пар при сохранении всех жёстких ограничений (пересечения, занятые ресурсы,
    /// оборудование, вместимость, смена, переезды между корпусами, двойные пары). Мягкие
    /// предпочтения (доступность, окна, корпус, дневные лимиты, якорь прошлого семестра) опускаются:
    /// цель — не оставить преподавателей вовсе без занятий, а не выдать идеальное расписание
    /// (его дорабатывает планировщик вручную по диагностике дефицита).
    /// </summary>
    public static ScheduleModelDirector CreatePerInstituteBestEffort()
    {
        return new(new IModelSectionBuilder[]
        {
            // Переменные.
            new VariablesSectionBuilder(),

            // Размещение — мягкое (максимум возможного); прочие ограничения остаются жёсткими.
            new BestEffortPlacementSectionBuilder(),
            new IntersectionSectionBuilder(),
            new OccupiedResourcesSectionBuilder(),
            new EquipmentSectionBuilder(),
            new CapacitySectionBuilder(),
            new ShiftSectionBuilder(),
            new BuildingTravelSectionBuilder(),
            new DoubleLessonSectionBuilder(),

            // Целевая функция (минимизирует отрицательные слагаемые размещения ⇒ максимум пар).
            new ObjectiveSectionBuilder(),
        });
    }

    public ScheduleModel Build(ScheduleData data)
    {
        var model = new ScheduleModel(data);

        foreach (var builder in _builders)
            builder.Build(model);

        return model;
    }
}
