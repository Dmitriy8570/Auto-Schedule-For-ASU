using Application.Solver.BuilderDev.BuilderInterface;
using Application.Solver.ModelDev;
using Google.OrTools.Sat;

namespace Application.Solver.BuilderDev.BuildSections;

/// <summary>
/// Жёсткое ограничение двойных пар: занятия по нагрузкам с <c>Curriculum.Double == true</c>
/// (как правило, лабораторные) должны идти спаренно — в той же аудитории в соседнем слоте
/// того же дня. Реализовано как: если занятие стоит в (нагрузка, аудитория, слот), то у этой
/// же нагрузки и аудитории должно быть занятие в предыдущем или следующем слоте дня.
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
                    for (int i = 0; i < slots.Count; i++)
                    {
                        // Допустимые «соседи» по спариванию: предыдущий и следующий слот дня.
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

                        // current ⇒ (хотя бы один сосед занят этой же нагрузкой и аудиторией).
                        model.Model.AddBoolOr(neighbours.Append(current.Not()).ToArray());
                    }
        }
    }
}
