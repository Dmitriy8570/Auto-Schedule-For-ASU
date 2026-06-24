using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Жёсткое ограничение последовательной декомпозиции по институтам (вариант «B»):
/// ресурсы, уже занятые расписанием ранее посчитанных институтов этого семестра,
/// запрещено использовать в текущей подзадаче.
/// <list type="bullet">
/// <item>занятую аудиторию нельзя назначить ни одной нагрузке в занятом слоте;</item>
/// <item>занятого в слоте преподавателя нельзя поставить на занятие в этом слоте.</item>
/// </list>
/// Если соответствующие поля <see cref="ScheduleData"/> не заданы (генерация на весь
/// семестр сразу) — секция ничего не делает.
/// </summary>
public class OccupiedResourcesSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        ForbidOccupiedClassrooms(model);
        ForbidOccupiedTeachers(model);
    }

    private static void ForbidOccupiedClassrooms(ScheduleModel model)
    {
        var occupied = model.Data.OccupiedClassroomSlots;
        if (occupied is null || occupied.Count == 0) return;

        var roomIndex = BuildIndex(model.Data.Classrooms.Select(c => c.Id));
        var slotIndex = BuildIndex(model.Data.TimeSlots.Select(s => s.Id));

        foreach (var o in occupied)
        {
            if (!roomIndex.TryGetValue(o.ClassroomId, out int r)) continue;
            if (!slotIndex.TryGetValue(o.TimeSlotId, out int t)) continue;

            for (int w = 0; w < model.WorkloadCount; w++)
                if (model.Lessons[w, r, t] is { } var) model.Model.Add(var == 0);
        }
    }

    private static void ForbidOccupiedTeachers(ScheduleModel model)
    {
        var occupied = model.Data.OccupiedTeacherSlots;
        if (occupied is null || occupied.Count == 0) return;

        var slotIndex = BuildIndex(model.Data.TimeSlots.Select(s => s.Id));

        // Нагрузки, сгруппированные по преподавателю, чтобы за один проход
        // блокировать все его занятия в занятых слотах.
        var workloadsByTeacher = Enumerable.Range(0, model.WorkloadCount)
            .ToLookup(w => model.Data.Workloads[w].Curriculum.TeacherId);

        foreach (var o in occupied)
        {
            if (!slotIndex.TryGetValue(o.TimeSlotId, out int t)) continue;

            foreach (int w in workloadsByTeacher[o.TeacherId])
                for (int r = 0; r < model.ClassroomCount; r++)
                    if (model.Lessons[w, r, t] is { } var) model.Model.Add(var == 0);
        }
    }

    private static Dictionary<Guid, int> BuildIndex(IEnumerable<Guid> ids)
    {
        var map = new Dictionary<Guid, int>();
        int i = 0;
        foreach (var id in ids)
            map[id] = i++;
        return map;
    }
}
