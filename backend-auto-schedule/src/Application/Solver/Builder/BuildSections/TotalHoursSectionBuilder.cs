using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Жёсткое ограничение: суммарное число занятий по каждой нагрузке за семестр должно
/// точно соответствовать плановому количеству пар (Hours / 2, т.к. одна пара = 2 ак. часа).
/// Нечётные часы недопустимы (пару нельзя поделить пополам) — это ошибка данных, поэтому
/// при их обнаружении генерация прерывается с явным сообщением, а не молча теряет часы.
/// </summary>
public class TotalHoursSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            int hours = model.Data.SemesterWorkloads[w].Hours;
            if (hours % 2 != 0)
                throw new InvalidOperationException(
                    $"Нагрузка #{w} (CurriculumId={model.Data.SemesterWorkloads[w].CurriculumId}) задаёт " +
                    $"нечётное число часов ({hours}); часы должны быть кратны 2 (одна пара = 2 ак. часа).");

            var vars = new List<LinearExpr>();
            for (int r = 0; r < model.ClassroomCount; r++)
                for (int t = 0; t < model.TimeSlotCount; t++)
                    vars.Add(model.Lessons[w, r, t]);

            model.Model.Add(LinearExpr.Sum(vars) == hours / 2);
        }
    }
}
