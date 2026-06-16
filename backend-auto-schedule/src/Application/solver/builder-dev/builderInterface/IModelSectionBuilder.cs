using Application.Solver.ModelDev;

namespace Application.Solver.BuilderDev.BuilderInterface;

/// <summary>
/// Строитель одной секции модели расписания (ограничение или слагаемое целевой функции).
/// Расширяемость: новое правило добавляется отдельным строителем без изменения существующих.
/// </summary>
public interface IModelSectionBuilder
{
    void Build(ScheduleModel model);
}
