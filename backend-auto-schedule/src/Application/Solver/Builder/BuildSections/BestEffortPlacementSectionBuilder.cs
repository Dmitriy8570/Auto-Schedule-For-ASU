using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Мягкое размещение: вместо жёсткого равенства «ровно Hours/2 пар» (<see cref="TotalHoursSectionBuilder"/>)
/// накладывает границу «не более Hours/2 пар» и поощряет каждую размещённую пару отрицательным
/// слагаемым целевой функции (<see cref="SolverPenaltyWeights.PlacementReward"/>). Награда заведомо
/// больше любых штрафов качества, поэтому солвер сначала ставит максимум возможного, а среди решений
/// с максимумом пар — выбирает самое компактное (минимум окон/дней).
///
/// Это делает модель ВСЕГДА разрешимой (можно не поставить ничего), поэтому план, который физически
/// не влезает (пересечения/смена/занятые ресурсы), просто недоразмещается, не делая неразрешимой всю
/// модель и не отключая оптимизацию качества для остальных нагрузок. Все жёсткие ограничения
/// сохраняются, поэтому частичное расписание валидно; дефицит до плана попадает в диагностику.
/// </summary>
public sealed class BestEffortPlacementSectionBuilder : IModelSectionBuilder
{
    private readonly int _reward;

    public BestEffortPlacementSectionBuilder(SolverPenaltyWeights? weights = null)
        => _reward = (weights ?? SolverPenaltyWeights.Default).PlacementReward;

    public void Build(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            int plannedPairs = model.Data.Workloads[w].Hours / 2;

            var vars = new List<LinearExpr>();
            for (int r = 0; r < model.ClassroomCount; r++)
                for (int t = 0; t < model.TimeSlotCount; t++)
                    if (model.Lessons[w, r, t] is { } var) vars.Add(var); // пропуск запрещённых (прунинг)

            if (vars.Count == 0) continue; // ни одной допустимой тройки — поставить нечего.

            // Перевыполнять план нельзя, недовыполнять — допустимо (в этом и смысл релаксации).
            model.Model.Add(LinearExpr.Sum(vars) <= plannedPairs);

            // Поощрение за каждую размещённую пару: минимизация суммы (−reward·var) ⇒ максимум пар.
            foreach (var var in vars)
                model.Objective.Add(LinearExpr.Term(var, -_reward));
        }
    }
}
