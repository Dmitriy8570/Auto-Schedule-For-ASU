using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Мягкое ограничение компактности недели: штрафует за каждый ДЕНЬ, в который у субъекта (учебной
/// группы или преподавателя) есть хотя бы одно занятие. Минимизируя число задействованных дней,
/// солвер собирает недельную нагрузку в меньшее число плотных дней (в связке с дневным лимитом —
/// около 3–4 пар в день), вместо того чтобы размазывать 1–2 пары по всем шести дням.
///
/// Зачем отдельно от <see cref="WindowSectionBuilder"/>: штраф за «окно» видит лишь разрывы ВНУТРИ
/// дня, поэтому расписание «одна пара в день × шесть дней» имеет ноль окон, но максимально разрежено —
/// сколько вес окна ни поднимай, штрафовать нечего. Этот строитель штрафует саму разреженность по
/// дням. Доп. переменных немного (субъект × день), поэтому памяти почти не добавляет.
/// </summary>
public sealed class DayCompactnessSectionBuilder : IModelSectionBuilder
{
    private readonly SolverPenaltyWeights _weights;

    public DayCompactnessSectionBuilder(SolverPenaltyWeights? weights = null)
        => _weights = weights ?? SolverPenaltyWeights.Default;

    public void Build(ScheduleModel model)
    {
        var slotsByDay = model.Data.TimeSlots
            .Select((slot, index) => (slot, index))
            .GroupBy(x => x.slot.WeekDay)
            .Select(g => g.Select(x => x.index).ToList())
            .ToList();

        var byGroup = Enumerable.Range(0, model.WorkloadCount)
            .SelectMany(w => model.Data.Workloads[w].Curriculum.Stream.StreamGroups
                .Select(sg => (Workload: w, sg.GroupId)))
            .GroupBy(x => x.GroupId, x => x.Workload);
        foreach (var g in byGroup)
            PenalizeDays(model, g.ToList(), slotsByDay, _weights.GroupDayUsage, $"gd{g.Key:N}");

        var byTeacher = Enumerable.Range(0, model.WorkloadCount)
            .GroupBy(w => model.Data.Workloads[w].Curriculum.Teacher.Id);
        foreach (var t in byTeacher)
            PenalizeDays(model, t.ToList(), slotsByDay, _weights.TeacherDayUsage, $"td{t.Key:N}");
    }

    /// <summary>
    /// Для каждого дня вводит булеву «день задействован» = OR всех занятий субъекта в этот день и
    /// штрафует её. Минимизация суммы по дням сжимает нагрузку в меньшее число дней.
    /// </summary>
    private static void PenalizeDays(
        ScheduleModel model, IReadOnlyList<int> workloads,
        IReadOnlyList<List<int>> slotsByDay, int weight, string prefix)
    {
        if (weight <= 0 || workloads.Count == 0) return;

        for (int d = 0; d < slotsByDay.Count; d++)
        {
            var literals = new List<ILiteral>();
            foreach (int w in workloads)
                foreach (int r in Enumerable.Range(0, model.ClassroomCount))
                    foreach (int t in slotsByDay[d])
                        if (model.Lessons[w, r, t] is { } var) literals.Add(var); // пропуск запрещённых (прунинг)

            if (literals.Count == 0) continue;

            var used = model.Model.NewBoolVar($"useday_{prefix}_{d}");
            // used == OR(literals): любой урок ⇒ день задействован; день не задействован ⇒ все уроки ложны.
            model.Model.AddBoolOr(literals.Append(used.Not()).ToArray());
            foreach (var lit in literals)
                model.Model.AddImplication(lit, used);

            model.Objective.Add(LinearExpr.Term(used, weight));
        }
    }
}
