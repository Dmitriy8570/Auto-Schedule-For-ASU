using Application.solver.builder.builderInterface;
using Application.solver.model;
using Domain.university.teachers;
using Domain.workload;
using Google.OrTools.ConstraintSolver;
using Google.OrTools.LinearSolver;
using Google.OrTools.Sat;

namespace Application.solver.builder.buildSections
{
    public class WindowSectionBuilder: IModelSectionBuilder
    {
        public void Build(ScheduleModel model)
        {
            GroupWindow(model);
            TeacherWindow(model);
        }
        
        private void GroupWindow(ScheduleModel model)
        {
            int penalty = model.Data.Penalties.First(x => x.ConstraintType == Domain.constraints.penalty.ConstraintType.StudentGap).Penalty;
            var workloadsByGroup = model.Data.SemesterWorkloads.GroupBy(w => w.Curriculum.Stream);
            foreach (var groupWorkloads in workloadsByGroup)
            {
                AddPenalties(model, groupWorkloads, model.Expr, penalty, "grp");
            }
        }

        private void TeacherWindow(ScheduleModel model)
        {
            int penalty = model.Data.Penalties.First(x => x.ConstraintType == Domain.constraints.penalty.ConstraintType.TeacherGap).Penalty;
            var workloadsByGroup = model.Data.SemesterWorkloads.GroupBy(w => w.Curriculum.Teacher);
            foreach (var groupWorkloads in workloadsByGroup)
            {
                AddPenalties(model, groupWorkloads, model.Expr, penalty, "tch");
            }
        }

        private void AddPenalties(ScheduleModel model,
            IEnumerable<SemesterWorkload> workloadSubSet,
            List<Google.OrTools.Sat.LinearExpr> objective,
            int weight,
            string kind)
        {
            var targetIndices = new List<int>();
            for (int i = 0; i < model.Data.SemesterWorkloads.Count; i++)
            {
                if (workloadSubSet.Contains(model.Data.SemesterWorkloads[i]))
                    targetIndices.Add(i);
            }

            if (targetIndices.Count == 0) return;

            string prefix = $"{kind}_{string.Join("_", targetIndices)}";

            var slotsByDay = model.Data.TimeSlots
                .Index()
                .GroupBy(s => s.Item.WeekDay)
                .ToList();
            foreach (var dayGroup in slotsByDay)
            {
                var slots = dayGroup.OrderBy(s => s.Item.Number).ToList();
                int D = slots.Count;

                if (D < 3) continue;

                var busy = new BoolVar[D];

                for (int i = 0; i < D; i++)
                {
                    int slotIdx = slots[i].Index;
                    busy[i] = model.Model.NewBoolVar($"busy_{prefix}_d{dayGroup.Key}_s{i}");

                    var lessonsInSlot = targetIndices
                        .SelectMany(wIdx =>
                            Enumerable.Range(0, model.Data.Classrooms.Count)
                                .Select(room => model.Lessons[wIdx, room, slotIdx]))
                        .ToList();

                    model.Model.AddBoolOr(lessonsInSlot.Append(busy[i].Not()).ToArray());

                    foreach (var lesson in lessonsInSlot)
                        model.Model.AddImplication(lesson, busy[i]);
                }

                var hasLessonBefore = new BoolVar[D];
                var hasLessonAfter = new BoolVar[D];

                for (int i = 0; i < D; i++)
                {
                    hasLessonBefore[i] = model.Model.NewBoolVar($"before_{prefix}_d{dayGroup.Key}_s{i}");
                    hasLessonAfter[i] = model.Model.NewBoolVar($"after_{prefix}_d{dayGroup.Key}_s{i}");
                }

                model.Model.Add(hasLessonBefore[0] == 0); 
                model.Model.Add(hasLessonAfter[D - 1] == 0);

                for (int i = 1; i < D; i++)
                {
                    model.Model.AddImplication(busy[i - 1], hasLessonBefore[i]);
                    model.Model.AddImplication(hasLessonBefore[i - 1], hasLessonBefore[i]);

                    model.Model.AddBoolOr(new ILiteral[] {
                        busy[i - 1],
                        hasLessonBefore[i - 1],
                        hasLessonBefore[i].Not()
                    });
                }

                for (int i = D - 2; i >= 0; i--)
                {
                    model.Model.AddImplication(busy[i + 1], hasLessonAfter[i]);
                    model.Model.AddImplication(hasLessonAfter[i + 1], hasLessonAfter[i]);

                    model.Model.AddBoolOr(new ILiteral[] {
                        busy[i + 1],
                        hasLessonAfter[i + 1],
                        hasLessonAfter[i].Not()
                    });
                }

                for (int i = 1; i < D - 1; i++)
                {
                    var gap = model.Model.NewBoolVar($"gap_{prefix}_d{dayGroup.Key}_s{i}");

                    model.Model.AddBoolAnd(new ILiteral[] {
                        busy[i].Not(),
                        hasLessonBefore[i],
                        hasLessonAfter[i]
                    }).OnlyEnforceIf(gap);

                    model.Model.AddBoolOr(new ILiteral[] {
                        busy[i],
                        hasLessonBefore[i].Not(),
                        hasLessonAfter[i].Not()
                    }).OnlyEnforceIf(gap.Not());

                    objective.Add(Google.OrTools.Sat.LinearExpr.Term(gap, weight));
                }
            }
        }
    }
}
