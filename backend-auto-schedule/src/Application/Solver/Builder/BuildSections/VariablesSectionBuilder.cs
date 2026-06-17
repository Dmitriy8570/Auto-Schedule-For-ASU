using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Инициализирует трёхмерный массив булевых переменных модели:
/// для каждой тройки (нагрузка, аудитория, временной слот) создаётся отдельная BoolVar.
/// </summary>
public class VariablesSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
            for (int r = 0; r < model.ClassroomCount; r++)
                for (int t = 0; t < model.TimeSlotCount; t++)
                    model.Lessons[w, r, t] = model.Model.NewBoolVar($"lesson_{w}_{r}_{t}");
    }
}
