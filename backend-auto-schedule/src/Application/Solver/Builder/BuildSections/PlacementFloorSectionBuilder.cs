using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Второй этап двухфазного решения: закрепляет размещение, найденное на первом этапе (максимум
/// поставленных пар), как нижнюю границу, НЕ добавляя награду за размещение в целевую функцию.
/// Для каждой нагрузки число поставленных пар ограничено снизу значением из первого этапа и сверху
/// планом недели (Hours/2): <c>floor[w] ≤ Σ ≤ Hours/2</c>.
///
/// Зачем так: если размещение и качество (компактность, окна) оптимизировать одной функцией, большая
/// награда за размещение доминирует, и солвер не «дотягивается» до вторичного члена компактности —
/// расписание остаётся разбросанным. Зафиксировав покрытие как жёсткое ограничение, второй этап
/// оптимизирует ТОЛЬКО качество (без доминирующей награды), поэтому компактность реально минимизируется,
/// а ни один преподаватель не теряет занятия (нижняя граница не даёт выбросить пары ради экономии дня).
/// </summary>
public sealed class PlacementFloorSectionBuilder : IModelSectionBuilder
{
    private readonly IReadOnlyList<int> _floors;

    /// <param name="floors">Сколько пар поставлено по каждой нагрузке на первом этапе (по индексу нагрузки).</param>
    public PlacementFloorSectionBuilder(IReadOnlyList<int> floors) => _floors = floors;

    public void Build(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            int plannedPairs = model.Data.Workloads[w].Hours / 2;
            int floor = w < _floors.Count ? Math.Min(_floors[w], plannedPairs) : 0;

            var vars = new List<LinearExpr>();
            for (int r = 0; r < model.ClassroomCount; r++)
                for (int t = 0; t < model.TimeSlotCount; t++)
                    if (model.Lessons[w, r, t] is { } var) vars.Add(var); // пропуск запрещённых (прунинг)

            if (vars.Count == 0) continue;

            var sum = LinearExpr.Sum(vars);
            model.Model.Add(sum <= plannedPairs);  // перевыполнять план нельзя
            if (floor > 0)
                model.Model.Add(sum >= floor);     // сохранить покрытие первого этапа
        }
    }
}
