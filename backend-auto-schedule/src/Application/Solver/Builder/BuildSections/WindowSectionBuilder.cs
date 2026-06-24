using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Domain.constraints.penalty;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Мягкое ограничение: штраф за «окна» в расписании. Окно — пустой слот, у которого в тот же
/// день есть занятие и до, и после, у одного субъекта (группы или преподавателя). Веса берутся
/// из конфигурации (<see cref="ConstraintType.StudentGap"/> / <see cref="ConstraintType.TeacherGap"/>).
/// </summary>
public class WindowSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        int studentWeight = PenaltyFor(model, ConstraintType.StudentGap);
        int teacherWeight = PenaltyFor(model, ConstraintType.TeacherGap);

        var byGroup = Enumerable.Range(0, model.WorkloadCount)
            .SelectMany(w => model.Data.Workloads[w].Curriculum.Stream.StreamGroups
                .Select(sg => (Workload: w, sg.Group.Id)))
            .GroupBy(x => x.Id, x => x.Workload);
        foreach (var group in byGroup)
            AddGapPenalties(model, group.ToList(), studentWeight, $"grp{group.Key:N}");

        var byTeacher = Enumerable.Range(0, model.WorkloadCount)
            .GroupBy(w => model.Data.Workloads[w].Curriculum.Teacher.Id);
        foreach (var teacher in byTeacher)
            AddGapPenalties(model, teacher.ToList(), teacherWeight, $"tch{teacher.Key:N}");
    }

    private static int PenaltyFor(ScheduleModel model, ConstraintType type) =>
        model.Data.Penalties.FirstOrDefault(p => p.ConstraintType == type)?.Penalty ?? 1;

    private static void AddGapPenalties(ScheduleModel model, IReadOnlyList<int> workloads, int weight, string prefix)
    {
        if (workloads.Count == 0 || weight <= 0) return;

        var days = model.Data.TimeSlots
            .Select((slot, index) => (slot, index))
            .GroupBy(x => x.slot.WeekDay);

        foreach (var day in days)
        {
            var slots = day.OrderBy(x => x.slot.Number).Select(x => x.index).ToList();
            int d = slots.Count;
            if (d < 3) continue; // окно требует минимум три слота: занятие — окно — занятие.

            var busy = new BoolVar[d];
            for (int i = 0; i < d; i++)
            {
                busy[i] = model.Model.NewBoolVar($"busy_{prefix}_{day.Key:N}_{i}");
                var lessons = workloads
                    .SelectMany(w => Enumerable.Range(0, model.ClassroomCount)
                        .Select(r => model.Lessons[w, r, slots[i]]))
                    .Where(v => v is not null)
                    .Select(v => v!)
                    .ToList<ILiteral>();

                model.Model.AddBoolOr(lessons.Append(busy[i].Not()).ToArray());
                foreach (var lesson in lessons)
                    model.Model.AddImplication(lesson, busy[i]);
            }

            var before = new BoolVar[d];
            var after = new BoolVar[d];
            for (int i = 0; i < d; i++)
            {
                before[i] = model.Model.NewBoolVar($"before_{prefix}_{day.Key:N}_{i}");
                after[i] = model.Model.NewBoolVar($"after_{prefix}_{day.Key:N}_{i}");
            }
            model.Model.Add(before[0] == 0);
            model.Model.Add(after[d - 1] == 0);

            for (int i = 1; i < d; i++)
            {
                model.Model.AddImplication(busy[i - 1], before[i]);
                model.Model.AddImplication(before[i - 1], before[i]);
                model.Model.AddBoolOr(new ILiteral[] { busy[i - 1], before[i - 1], before[i].Not() });
            }
            for (int i = d - 2; i >= 0; i--)
            {
                model.Model.AddImplication(busy[i + 1], after[i]);
                model.Model.AddImplication(after[i + 1], after[i]);
                model.Model.AddBoolOr(new ILiteral[] { busy[i + 1], after[i + 1], after[i].Not() });
            }

            for (int i = 1; i < d - 1; i++)
            {
                var gap = model.Model.NewBoolVar($"gap_{prefix}_{day.Key:N}_{i}");
                model.Model.AddBoolAnd(new ILiteral[] { busy[i].Not(), before[i], after[i] }).OnlyEnforceIf(gap);
                model.Model.AddBoolOr(new ILiteral[] { busy[i], before[i].Not(), after[i].Not() }).OnlyEnforceIf(gap.Not());
                model.Objective.Add(LinearExpr.Term(gap, weight));
            }
        }
    }
}
