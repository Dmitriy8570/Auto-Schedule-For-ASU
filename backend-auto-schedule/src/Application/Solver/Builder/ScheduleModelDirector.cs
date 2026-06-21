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

    public ScheduleModel Build(ScheduleData data)
    {
        var model = new ScheduleModel(data);

        foreach (var builder in _builders)
            builder.Build(model);

        return model;
    }
}
