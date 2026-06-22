using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Жёсткое ограничение по оборудованию: нагрузка может быть назначена только в ту аудиторию,
/// которая оснащена всем необходимым для неё оборудованием. Если аудитория не удовлетворяет
/// требованиям, все переменные (нагрузка, аудитория, *) обнуляются.
/// </summary>
public class EquipmentSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            var needed = model.Data.SemesterWorkloads[w].Curriculum.NeededEquipments
                .Select(ne => ne.EquipmentId)
                .ToHashSet();

            if (needed.Count == 0) continue;

            for (int r = 0; r < model.ClassroomCount; r++)
            {
                var available = model.Data.Classrooms[r].EquipmentRooms
                    .Select(er => er.EquipmentId)
                    .ToHashSet();

                if (needed.IsSubsetOf(available)) continue;

                // Аудитория не оснащена — запрещаем нагрузку в ней во всех слотах.
                // (Обычно такие тройки уже отсечены прунингом => ячейки null и пропускаются.)
                for (int t = 0; t < model.TimeSlotCount; t++)
                    if (model.Lessons[w, r, t] is { } var) model.Model.Add(var == 0);
            }
        }
    }
}
