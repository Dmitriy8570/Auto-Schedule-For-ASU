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
    public static ScheduleModelDirector CreateDefault() => new(new IModelSectionBuilder[]
    {
        // Переменные.
        new VariablesSectionBuilder(),

        // Жёсткие ограничения.
        new TotalHoursSectionBuilder(),
        new IntersectionSectionBuilder(),
        new EquipmentSectionBuilder(),
        new ShiftSectionBuilder(),
        new BuildingTravelSectionBuilder(),
        new DoubleLessonSectionBuilder(),

        // Мягкие ограничения (штрафы в целевой функции).
        new DailyLessonsLimitSectionBuilder(),
        new WindowSectionBuilder(),
        new AvailabilitySectionBuilder(),
        new FavoriteBuildingSectionBuilder(),
        new ParallelismSectionBuilder(),

        // Целевая функция.
        new ObjectiveSectionBuilder(),
    });

    public ScheduleModel Build(ScheduleData data)
    {
        var model = new ScheduleModel(data);

        foreach (var builder in _builders)
            builder.Build(model);

        return model;
    }
}
