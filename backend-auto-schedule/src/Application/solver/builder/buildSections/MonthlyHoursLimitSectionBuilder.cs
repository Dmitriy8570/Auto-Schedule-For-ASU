using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Добавляет жёсткое ограничение: суммарное число занятий по каждой нагрузке
/// за семестр должно точно соответствовать плановому количеству часов (Hours / 2,
/// поскольку одна пара = 2 академических часа).
/// </summary>
public class TotalHoursConstraintSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        var workloads = model.Data.SemesterWorkloads.Index();

        foreach (var (index, item) in workloads)
        {
            var taskVars = new List<LinearExpr>();

            for (int room = 0; room < model.Data.Classrooms.Count; room++)
            {
                for (int slot = 0; slot < model.Data.TimeSlots.Count; slot++)
                {
                    taskVars.Add(model.Lessons[index, room, slot]);
                }
            }

            // Одна запись в Lessons соответствует одной паре (2 часа).
            model.Model.Add(LinearExpr.Sum(taskVars) == item.Hours / 2);
        }
    }
}
