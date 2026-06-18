using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Жёсткое ограничение двойных пар: занятия по нагрузкам с <c>Curriculum.Double == true</c>
/// (как правило, лабораторные) должны идти спаренно — в той же аудитории, сериями ровно из двух
/// подряд идущих слотов одного дня. Для каждой пары (нагрузка, аудитория) в пределах дня
/// накладываются два условия:
/// <list type="bullet">
/// <item>нет одиночных: занятие в слоте требует занятия в соседнем слоте (предыдущем или следующем);</item>
/// <item>нет серий из трёх и более: в любых трёх подряд идущих слотах занято не более двух.</item>
/// </list>
/// Вместе это даёт непрерывные блоки длиной ровно 2.
/// </summary>
public class DoubleLessonSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        // Для каждого дня — индексы слотов по порядку пар.
        var days = model.Data.TimeSlots
            .Select((slot, index) => (slot, index))
            .GroupBy(x => x.slot.WeekDay)
            .Select(day => day.OrderBy(x => x.slot.Number).Select(x => x.index).ToList())
            .ToList();

        for (int w = 0; w < model.WorkloadCount; w++)
        {
            if (!model.Data.SemesterWorkloads[w].Curriculum.Double) continue;

            for (int r = 0; r < model.ClassroomCount; r++)
                foreach (var slots in days)
                {
                    // 1. Нет одиночных занятий.
                    for (int i = 0; i < slots.Count; i++)
                    {
                        // Допустимые соседи по спариванию: предыдущий и следующий слот дня.
                        var neighbours = new List<ILiteral>();
                        if (i > 0) neighbours.Add(model.Lessons[w, r, slots[i - 1]]);
                        if (i < slots.Count - 1) neighbours.Add(model.Lessons[w, r, slots[i + 1]]);

                        var current = model.Lessons[w, r, slots[i]];

                        if (neighbours.Count == 0)
                        {
                            // Единственный слот дня не может быть половиной двойной пары.
                            model.Model.Add(current == 0);
                            continue;
                        }

                        // current => хотя бы один сосед занят этой же нагрузкой и аудиторией.
                        model.Model.AddBoolOr(neighbours.Append(current.Not()).ToArray());
                    }

                    // 2. Нет серий из трёх и более: в любом окне из трёх слотов занято не больше двух.
                    for (int i = 0; i + 2 < slots.Count; i++)
                        model.Model.Add(
                            model.Lessons[w, r, slots[i]]
                            + model.Lessons[w, r, slots[i + 1]]
                            + model.Lessons[w, r, slots[i + 2]] <= 2);
                }
        }
    }
}
