using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Мягкое ограничение параллельности подгрупп: занятия с <c>Curriculum.Parallelism == true</c>,
/// которые ведутся у подгрупп одной родительской группы по одной дисциплине, желательно
/// проводить одновременно. Реализовано как штраф за число различных слотов, занятых таким
/// набором: чем в большем числе слотов «размазаны» подгруппы, тем выше штраф, поэтому солвер
/// стремится поставить их в одни и те же пары.
/// </summary>
public class ParallelismSectionBuilder : IModelSectionBuilder
{
    private readonly SolverPenaltyWeights _weights;

    public ParallelismSectionBuilder(SolverPenaltyWeights? weights = null)
        => _weights = weights ?? SolverPenaltyWeights.Default;

    public void Build(ScheduleModel model)
    {
        // Наборы нагрузок: ключ — (дисциплина, родительская группа).
        var parallelSets = Enumerable.Range(0, model.WorkloadCount)
            .Where(w => model.Data.Workloads[w].Curriculum.Parallelism)
            .SelectMany(w => model.Data.Workloads[w].Curriculum.Stream.StreamGroups
                .Where(sg => sg.Group.ParentGroupId is not null)
                .Select(sg => (Workload: w,
                               Key: (model.Data.Workloads[w].Curriculum.SubjectId,
                                     ParentId: sg.Group.ParentGroupId!.Value))))
            .GroupBy(x => x.Key, x => x.Workload)
            .Select(g => g.Distinct().ToList())
            .Where(set => set.Count >= 2);

        int n = 0;
        foreach (var set in parallelSets)
            PenalizeSpread(model, set, $"par{n++}");
    }

    private void PenalizeSpread(ScheduleModel model, IReadOnlyList<int> workloads, string prefix)
    {
        for (int t = 0; t < model.TimeSlotCount; t++)
        {
            var busy = model.Model.NewBoolVar($"pbusy_{prefix}_{t}");
            var lessons = workloads
                .SelectMany(w => Enumerable.Range(0, model.ClassroomCount)
                    .Select(r => model.Lessons[w, r, t]))
                .Where(v => v is not null)
                .Select(v => v!)
                .ToList<ILiteral>();

            model.Model.AddBoolOr(lessons.Append(busy.Not()).ToArray());
            foreach (var lesson in lessons)
                model.Model.AddImplication(lesson, busy);

            model.Objective.Add(LinearExpr.Term(busy, _weights.Parallelism));
        }
    }
}
