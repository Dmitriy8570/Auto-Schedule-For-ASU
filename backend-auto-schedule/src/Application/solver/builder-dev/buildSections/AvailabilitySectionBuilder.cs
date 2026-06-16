using Application.Solver.BuilderDev.BuilderInterface;
using Application.Solver.ModelDev;
using Google.OrTools.Sat;

namespace Application.Solver.BuilderDev.BuildSections;

/// <summary>
/// Мягкое ограничение доступности: назначение занятия в нежелательный для преподавателя или
/// аудитории слот штрафуется весом из соответствующей записи доступности
/// (<c>TeacherAvailability.Penalty</c> / <c>ClassroomAvailability.Penalty</c>).
/// </summary>
public class AvailabilitySectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        TeacherAvailability(model);
        ClassroomAvailability(model);
    }

    private static void TeacherAvailability(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            var teacher = model.Data.SemesterWorkloads[w].Curriculum.Teacher;
            if (teacher.TeacherAvailabilities is null) continue;

            foreach (var a in teacher.TeacherAvailabilities)
                for (int t = 0; t < model.TimeSlotCount; t++)
                {
                    var slot = model.Data.TimeSlots[t];
                    if (slot.WeekDay.DayOfWeek != a.DayOfWeek || slot.Number != a.NumberLesson) continue;

                    for (int r = 0; r < model.ClassroomCount; r++)
                        model.Objective.Add(LinearExpr.Term(model.Lessons[w, r, t], a.Penalty));
                }
        }
    }

    private static void ClassroomAvailability(ScheduleModel model)
    {
        for (int r = 0; r < model.ClassroomCount; r++)
        {
            var classroom = model.Data.Classrooms[r];
            if (classroom.ClassroomAvailabilities is null) continue;

            foreach (var a in classroom.ClassroomAvailabilities)
                for (int t = 0; t < model.TimeSlotCount; t++)
                {
                    var slot = model.Data.TimeSlots[t];
                    if (slot.WeekDay.DayOfWeek != a.DayOfWeek || slot.Number != a.NumberLesson) continue;

                    for (int w = 0; w < model.WorkloadCount; w++)
                        model.Objective.Add(LinearExpr.Term(model.Lessons[w, r, t], a.Penalty));
                }
        }
    }
}
