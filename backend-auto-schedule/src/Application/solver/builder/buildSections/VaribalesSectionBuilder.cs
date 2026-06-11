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
        for (int workload = 0; workload < model.Data.SemesterWorkloads.Count; workload++)
        {
            for (int classroom = 0; classroom < model.Data.Classrooms.Count; classroom++)
            {
                for (int timeslot = 0; timeslot < model.Data.TimeSlots.Count; timeslot++)
                {
                    model.Lessons[workload, classroom, timeslot] =
                        model.Model.NewBoolVar($"lesson_{workload}_{classroom}_{timeslot}");
                }
            }
        }
    }
}
