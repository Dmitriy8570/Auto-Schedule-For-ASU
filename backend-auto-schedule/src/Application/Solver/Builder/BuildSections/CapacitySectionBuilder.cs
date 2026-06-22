using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Жёсткое ограничение вместимости: нагрузку нельзя назначить в аудиторию, чья вместимость
/// меньше числа студентов потока (<c>AcademicStream.StudentsCount</c>). Для неподходящих
/// аудиторий все переменные (нагрузка, аудитория, *) обнуляются.
/// </summary>
public class CapacitySectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            int students = model.Data.SemesterWorkloads[w].Curriculum.Stream.StudentsCount;
            if (students <= 0) continue;

            for (int r = 0; r < model.ClassroomCount; r++)
            {
                if (model.Data.Classrooms[r].Capacity >= students) continue;

                // Аудитория не вмещает поток — запрещаем нагрузку в ней во всех слотах.
                // (Обычно такие тройки уже отсечены прунингом => ячейки null и пропускаются.)
                for (int t = 0; t < model.TimeSlotCount; t++)
                    if (model.Lessons[w, r, t] is { } var) model.Model.Add(var == 0);
            }
        }
    }
}
