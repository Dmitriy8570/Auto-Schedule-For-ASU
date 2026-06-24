using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Мягкое ограничение лимита пар в день: согласно тексту диплома лимит переведён из жёсткого
/// в мягкий с большим штрафом — иногда у группы объективно требуется больше пар. За каждую
/// пару сверх лимита (группа — 4, преподаватель — 7) начисляется крупный штраф.
/// </summary>
public class DailyLessonsLimitSectionBuilder : IModelSectionBuilder
{
    private readonly SolverPenaltyWeights _weights;

    public DailyLessonsLimitSectionBuilder(SolverPenaltyWeights? weights = null)
        => _weights = weights ?? SolverPenaltyWeights.Default;

    public void Build(ScheduleModel model)
    {
        var slotsByDay = model.Data.TimeSlots
            .Select((slot, index) => (slot, index))
            .GroupBy(x => x.slot.WeekDay)
            .Select(g => g.Select(x => x.index).ToList())
            .ToList();

        var byTeacher = Enumerable.Range(0, model.WorkloadCount)
            .GroupBy(w => model.Data.Workloads[w].Curriculum.Teacher.Id)
            .Select(g => g.ToList());

        var byGroup = Enumerable.Range(0, model.WorkloadCount)
            .SelectMany(w => model.Data.Workloads[w].Curriculum.Stream.StreamGroups
                .Select(sg => (Workload: w, sg.Group.Id)))
            .GroupBy(x => x.Id, x => x.Workload)
            .Select(g => g.ToList());

        foreach (var teacher in byTeacher)
            PenalizeOverage(model, teacher, slotsByDay, _weights.DailyTeacherLimit, "tch");

        foreach (var group in byGroup)
            PenalizeOverage(model, group, slotsByDay, _weights.DailyGroupLimit, "grp");
    }

    private void PenalizeOverage(
        ScheduleModel model,
        IReadOnlyList<int> workloads,
        IReadOnlyList<List<int>> slotsByDay,
        int limit,
        string kind)
    {
        foreach (var day in slotsByDay)
        {
            var vars = new List<LinearExpr>();
            foreach (int w in workloads)
                foreach (int r in Enumerable.Range(0, model.ClassroomCount))
                    foreach (int t in day)
                        if (model.Lessons[w, r, t] is { } var) vars.Add(var);

            if (vars.Count == 0) continue;

            var excess = model.Model.NewIntVar(0, vars.Count, $"overage_{kind}_{vars.Count}_{day[0]}");
            model.Model.Add(LinearExpr.Sum(vars) - excess <= limit);
            model.Objective.Add(LinearExpr.Term(excess, _weights.DailyOveragePenalty));
        }
    }
}
