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
    /// Двухфазное решение института за <em>одну неделю</em>, ЭТАП 1 — максимум размещения. Размещение
    /// мягкое (<see cref="BestEffortPlacementSectionBuilder"/>, ≤ Hours/2 + награда за пару), мягкие
    /// ограничения качества опущены: цель этапа — быстро найти, сколько пар вообще можно поставить при
    /// всех жёстких ограничениях (перегруженная неделя физически не вмещает весь план). Это покрытие
    /// затем фиксируется этапом 2. Модель всегда разрешима.
    /// </summary>
    public static ScheduleModelDirector CreatePerWeekMaxPlacement(SolverPenaltyWeights? weights = null)
    {
        var w = weights ?? SolverPenaltyWeights.Default;
        return new(new IModelSectionBuilder[]
        {
            new VariablesSectionBuilder(),
            new BestEffortPlacementSectionBuilder(w), // ≤ Hours/2 + награда ⇒ максимум пар.
            new IntersectionSectionBuilder(),
            new OccupiedResourcesSectionBuilder(),
            new EquipmentSectionBuilder(),
            new CapacitySectionBuilder(),
            new SoftShiftSectionBuilder(w), // смена мягкая — даёт место упаковать день без окон.
            new BuildingTravelSectionBuilder(),
            new DoubleLessonSectionBuilder(),
            new ObjectiveSectionBuilder(),
        });
    }

    /// <summary>
    /// Двухфазное решение института за <em>одну неделю</em>, ЭТАП 2 — компактность при фиксированном
    /// покрытии. Число поставленных пар по каждой нагрузке закреплено снизу результатом этапа 1
    /// (<see cref="PlacementFloorSectionBuilder"/>), а в целевой функции — ТОЛЬКО штрафы качества (без
    /// доминирующей награды за размещение). Поэтому солвер реально минимизирует число учебных дней и
    /// окна, не выбрасывая занятия (нижняя граница хранит покрытие). Жёсткими остаются пересечения,
    /// занятые ресурсы, оборудование, вместимость, смена, переезды между корпусами и двойные пары;
    /// якорь — стабильность из недели в неделю.
    /// </summary>
    public static ScheduleModelDirector CreatePerWeekQuality(
        IReadOnlyList<int> placementFloors, SolverPenaltyWeights? weights = null)
    {
        var w = weights ?? SolverPenaltyWeights.Default;
        return new(new IModelSectionBuilder[]
        {
            new VariablesSectionBuilder(),

            // Покрытие этапа 1 как нижняя граница (без награды в целевой функции).
            new PlacementFloorSectionBuilder(placementFloors),

            // Жёсткие ограничения.
            new IntersectionSectionBuilder(),
            new OccupiedResourcesSectionBuilder(),
            new EquipmentSectionBuilder(),
            new CapacitySectionBuilder(),
            new BuildingTravelSectionBuilder(),
            new DoubleLessonSectionBuilder(),

            // Мягкие ограничения качества — слагаемые целевой функции на этом этапе.
            new SoftShiftSectionBuilder(w), // смена мягкая — даёт место упаковать день без окон.
            new DailyLessonsLimitSectionBuilder(w),
            new WindowSectionBuilder(),
            new DayCompactnessSectionBuilder(w),
            new AvailabilitySectionBuilder(),
            new FavoriteBuildingSectionBuilder(w),
            new ParallelismSectionBuilder(w),
            new PreviousScheduleAnchorSectionBuilder(w),

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
