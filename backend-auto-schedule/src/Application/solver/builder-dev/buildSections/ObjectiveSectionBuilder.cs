using Application.Solver.BuilderDev.BuilderInterface;
using Application.Solver.ModelDev;
using Google.OrTools.Sat;

namespace Application.Solver.BuilderDev.BuildSections;

/// <summary>
/// Завершающая секция: фиксирует целевую функцию как сумму всех накопленных штрафов мягких
/// ограничений и задаёт её минимизацию. Должна применяться последней в конвейере.
/// </summary>
public class ObjectiveSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        if (model.Objective.Count == 0) return;

        model.Model.Minimize(LinearExpr.Sum(model.Objective));
    }
}
