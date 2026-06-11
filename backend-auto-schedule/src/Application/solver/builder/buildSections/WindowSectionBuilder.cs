using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Domain.workload;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Добавляет в целевую функцию штрафы за «окна» в расписании.
/// Окно — пустой слот, перед которым и после которого есть хотя бы одно занятие
/// у одного и того же субъекта (группы или преподавателя) в тот же день.
/// Чем больше вес штрафа, тем сильнее солвер стремится убрать окна.
/// </summary>
public class WindowSectionBuilder : IModelSectionBuilder
{
    /// <summary>
    /// Регистрирует штрафы за окна для студенческих групп и преподавателей.
    /// </summary>
    public void Build(ScheduleModel model)
    {
        GroupWindow(model);
        TeacherWindow(model);
    }

    /// <summary>
    /// Добавляет штрафы за окна между занятиями одной группы (потока).
    /// Использует вес <see cref="ConstraintType.StudentGap"/> из конфигурации.
    /// </summary>
    private void GroupWindow(ScheduleModel model)
    {
        int penalty = model.Data.Penalties.First(x => x.ConstraintType == Domain.constraints.penalty.ConstraintType.StudentGap).Penalty;
        var workloadsByGroup = model.Data.SemesterWorkloads.GroupBy(w => w.Curriculum.Stream);
        foreach (var groupWorkloads in workloadsByGroup)
        {
            AddPenalties(model, groupWorkloads, model.Expr, penalty, "grp");
        }
    }

    /// <summary>
    /// Добавляет штрафы за окна между занятиями одного преподавателя.
    /// Использует вес <see cref="ConstraintType.TeacherGap"/> из конфигурации.
    /// </summary>
    private void TeacherWindow(ScheduleModel model)
    {
        int penalty = model.Data.Penalties.First(x => x.ConstraintType == Domain.constraints.penalty.ConstraintType.TeacherGap).Penalty;
        var workloadsByGroup = model.Data.SemesterWorkloads.GroupBy(w => w.Curriculum.Teacher);
        foreach (var groupWorkloads in workloadsByGroup)
        {
            AddPenalties(model, groupWorkloads, model.Expr, penalty, "tch");
        }
    }

    /// <summary>
    /// Формирует SAT-переменные и ограничения для обнаружения окон, затем
    /// добавляет взвешенный штраф за каждое найденное окно в целевую функцию.
    /// </summary>
    /// <param name="model">Текущая модель расписания.</param>
    /// <param name="workloadSubSet">Нагрузки одного субъекта (группы или преподавателя).</param>
    /// <param name="objective">Список слагаемых целевой функции (минимизируется).</param>
    /// <param name="weight">Штрафной коэффициент за одно окно.</param>
    /// <param name="kind">Префикс для имён переменных: "grp" — группа, "tch" — преподаватель.</param>
    private void AddPenalties(ScheduleModel model,
        IEnumerable<SemesterWorkload> workloadSubSet,
        List<LinearExpr> objective,
        int weight,
        string kind)
    {
        // Собираем индексы нагрузок, принадлежащих данному субъекту,
        // чтобы работать только с его занятиями при переборе слотов.
        var targetIndices = new List<int>();
        for (int i = 0; i < model.Data.SemesterWorkloads.Count; i++)
        {
            if (workloadSubSet.Contains(model.Data.SemesterWorkloads[i]))
                targetIndices.Add(i);
        }

        if (targetIndices.Count == 0) return;

        string prefix = $"{kind}_{string.Join("_", targetIndices)}";

        // Обрабатываем каждый день недели независимо:
        // окно имеет смысл только в рамках одного учебного дня.
        var slotsByDay = model.Data.TimeSlots
            .Index()
            .GroupBy(s => s.Item.WeekDay)
            .ToList();
        foreach (var dayGroup in slotsByDay)
        {
            var slots = dayGroup.OrderBy(s => s.Item.Number).ToList();
            int D = slots.Count;

            // Окно возможно только при наличии минимум трёх слотов в дне:
            // занятие — окно — занятие.
            if (D < 3) continue;

            // busy[i] = 1, если субъект имеет хотя бы одно занятие в i-м слоте дня.
            var busy = new BoolVar[D];

            for (int i = 0; i < D; i++)
            {
                int slotIdx = slots[i].Index;
                busy[i] = model.Model.NewBoolVar($"busy_{prefix}_d{dayGroup.Key}_s{i}");

                // Все пары (нагрузка, аудитория) в данном слоте для данного субъекта.
                var lessonsInSlot = targetIndices
                    .SelectMany(wIdx =>
                        Enumerable.Range(0, model.Data.Classrooms.Count)
                            .Select(room => model.Lessons[wIdx, room, slotIdx]))
                    .ToList();

                // Если ни одно занятие не стоит в слоте, busy[i] должен быть 0.
                model.Model.AddBoolOr(lessonsInSlot.Append(busy[i].Not()).ToArray());

                // Если хотя бы одно занятие стоит в слоте, busy[i] обязан быть 1.
                foreach (var lesson in lessonsInSlot)
                    model.Model.AddImplication(lesson, busy[i]);
            }

            // hasLessonBefore[i] = 1, если среди слотов 0..i-1 есть хотя бы одно занятие.
            // hasLessonAfter[i]  = 1, если среди слотов i+1..D-1 есть хотя бы одно занятие.
            var hasLessonBefore = new BoolVar[D];
            var hasLessonAfter = new BoolVar[D];

            for (int i = 0; i < D; i++)
            {
                hasLessonBefore[i] = model.Model.NewBoolVar($"before_{prefix}_d{dayGroup.Key}_s{i}");
                hasLessonAfter[i] = model.Model.NewBoolVar($"after_{prefix}_d{dayGroup.Key}_s{i}");
            }

            // Граничные условия: перед первым слотом и после последнего занятий нет.
            model.Model.Add(hasLessonBefore[0] == 0);
            model.Model.Add(hasLessonAfter[D - 1] == 0);

            // Прямой проход: hasLessonBefore[i] = busy[i-1] OR hasLessonBefore[i-1].
            for (int i = 1; i < D; i++)
            {
                model.Model.AddImplication(busy[i - 1], hasLessonBefore[i]);
                model.Model.AddImplication(hasLessonBefore[i - 1], hasLessonBefore[i]);

                // Если ни busy[i-1], ни hasLessonBefore[i-1] не истинны,
                // то hasLessonBefore[i] тоже должен быть ложным.
                model.Model.AddBoolOr(new ILiteral[] {
                    busy[i - 1],
                    hasLessonBefore[i - 1],
                    hasLessonBefore[i].Not()
                });
            }

            // Обратный проход: hasLessonAfter[i] = busy[i+1] OR hasLessonAfter[i+1].
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

            // Детектируем окна для внутренних слотов (первый и последний не могут быть окнами).
            // gap[i] = 1 тогда и только тогда, когда:
            //   слот i пустой (busy[i] = 0) И до него было занятие И после него будет занятие.
            for (int i = 1; i < D - 1; i++)
            {
                var gap = model.Model.NewBoolVar($"gap_{prefix}_d{dayGroup.Key}_s{i}");

                // gap => (NOT busy[i]) AND hasLessonBefore[i] AND hasLessonAfter[i]
                model.Model.AddBoolAnd(new ILiteral[] {
                    busy[i].Not(),
                    hasLessonBefore[i],
                    hasLessonAfter[i]
                }).OnlyEnforceIf(gap);

                // NOT gap => busy[i] OR (NOT hasLessonBefore[i]) OR (NOT hasLessonAfter[i])
                model.Model.AddBoolOr(new ILiteral[] {
                    busy[i],
                    hasLessonBefore[i].Not(),
                    hasLessonAfter[i].Not()
                }).OnlyEnforceIf(gap.Not());

                // Добавляем штраф: каждое окно увеличивает целевую функцию на weight.
                objective.Add(LinearExpr.Term(gap, weight));
            }
        }
    }
}
