using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Релаксация размещения для «аварийного» (best-effort) прогона: вместо жёсткого равенства
/// «ровно Hours/2 пар» (<see cref="TotalHoursSectionBuilder"/>) накладывает мягкую границу
/// «не более Hours/2 пар» и поощряет максимальное число размещённых пар отрицательными
/// слагаемыми целевой функции (солвер её минимизирует ⇒ ставит как можно больше пар).
///
/// Применяется только когда обычная (жёсткая) модель компонента оказалась неразрешимой
/// (Infeasible) либо не решилась за отведённое время (Unknown): иначе все нагрузки компонента
/// теряются целиком и преподаватели остаются вовсе без расписания. Все жёсткие ограничения
/// (пересечения, вместимость, оборудование, смена, занятые ресурсы, двойные пары) сохраняются,
/// поэтому частичное расписание остаётся валидным. Недостающие до плана пары фиксируются как
/// дефицит и попадают в диагностику результата генерации.
/// </summary>
public sealed class BestEffortPlacementSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            int plannedPairs = model.Data.SemesterWorkloads[w].Hours / 2;

            var vars = new List<LinearExpr>();
            for (int r = 0; r < model.ClassroomCount; r++)
                for (int t = 0; t < model.TimeSlotCount; t++)
                    if (model.Lessons[w, r, t] is { } var) vars.Add(var); // пропуск запрещённых (прунинг)

            if (vars.Count == 0) continue; // ни одной допустимой тройки — поставить нечего.

            // Перевыполнять план нельзя, недовыполнять — допустимо (в этом и смысл релаксации).
            model.Model.Add(LinearExpr.Sum(vars) <= plannedPairs);

            // Поощрение за каждую размещённую пару: минимизация суммы (−1·var) ⇒ максимум пар.
            foreach (var var in vars)
                model.Objective.Add(LinearExpr.Term(var, -1));
        }
    }
}
