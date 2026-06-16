using Application.Solver.BuilderDev.BuilderInterface;
using Application.Solver.ModelDev;
using Google.OrTools.Sat;

namespace Application.Solver.BuilderDev.BuildSections;

/// <summary>
/// Жёсткое ограничение: суммарное число занятий по каждой нагрузке за семестр должно
/// точно соответствовать плановому количеству часов (Hours / 2, т.к. одна пара = 2 ак. часа).
/// </summary>
public class TotalHoursSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            var vars = new List<LinearExpr>();
            for (int r = 0; r < model.ClassroomCount; r++)
                for (int t = 0; t < model.TimeSlotCount; t++)
                    vars.Add(model.Lessons[w, r, t]);

            model.Model.Add(LinearExpr.Sum(vars) == model.Data.SemesterWorkloads[w].Hours / 2);
        }
    }
}
