using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Domain.university.groups;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Жёсткое ограничение смены обучения: занятия группы ставятся только в слоты, относящиеся
/// к её смене. Соответствие «смена → номера пар» задано <see cref="SlotMatchesShift"/>
/// (предположение для dev-версии; в проде границы смен стоит вынести в конфигурацию).
/// </summary>
public class ShiftSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            var groups = model.Data.Workloads[w].Curriculum.Stream.StreamGroups;
            if (groups.Count == 0) continue;

            for (int t = 0; t < model.TimeSlotCount; t++)
            {
                int number = model.Data.TimeSlots[t].Number;

                // Если хотя бы одна группа потока не может заниматься в этот слот — запрещаем.
                bool allowed = groups.All(sg => SlotMatchesShift(sg.Group.Shift, number));
                if (allowed) continue;

                for (int r = 0; r < model.ClassroomCount; r++)
                    if (model.Lessons[w, r, t] is { } var) model.Model.Add(var == 0);
            }
        }
    }

    /// <summary>
    /// Номер пары допустим для смены: 1-я — пары 1..4, 2-я — 5..6, вечерняя — 7+.
    /// Для <see cref="Shift.Unspecified"/> ограничение смены не применяется (допустим любой слот).
    /// </summary>
    internal static bool SlotMatchesShift(Shift shift, int lessonNumber) => shift switch
    {
        Shift.First => lessonNumber <= 4,
        Shift.Second => lessonNumber is >= 5 and <= 6,
        Shift.Evening => lessonNumber >= 7,
        _ => true
    };
}
