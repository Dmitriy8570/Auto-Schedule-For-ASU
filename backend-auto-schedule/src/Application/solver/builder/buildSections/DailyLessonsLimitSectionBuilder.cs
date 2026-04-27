using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/*
 *
 *
 *           НЕОБХОДИМО ИЗМЕНИТЬ НА СИСТЕМУ ШТРАФОВ
 *
 *
 */

/// <summary>
/// Добавляет жёсткие ограничения на максимальное количество занятий в день
/// для преподавателей и студенческих групп.
/// </summary>
public class DailyLessonsLimitSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        AddTeacherLimit(model, 7);
        AddGroupLimit(model, 4);
    }

    /// <summary>Ограничивает количество пар преподавателя в один день.</summary>
    private void AddTeacherLimit(ScheduleModel model, int lessonLimit)
    {
        var workloadsByTeacher = model.Data.SemesterWorkloads
            .Index()
            .GroupBy(x => x.Item.Curriculum.Teacher);

        var slotsByDay = model.Data.TimeSlots
            .Index()
            .GroupBy(x => x.Item.WeekDay);

        foreach (var teacherGroup in workloadsByTeacher)
        {
            foreach (var dayGroup in slotsByDay)
            {
                var taskVars = teacherGroup
                    .SelectMany(x =>
                        Enumerable.Range(0, model.Data.Classrooms.Count)
                            .SelectMany(room =>
                                dayGroup.Select(slot => model.Lessons[x.Index, room, slot.Index])))
                    .ToArray();

                model.Model.Add(LinearExpr.Sum(taskVars) <= lessonLimit);
            }
        }
    }

    /// <summary>Ограничивает количество пар студенческой группы в один день.</summary>
    private void AddGroupLimit(ScheduleModel model, int lessonLimit)
    {
        var workloadsByGroup = model.Data.SemesterWorkloads
            .Index()
            .SelectMany(x => x.Item.Curriculum.Stream.StreamGroups
                .Select(sg => (IndexedWorkload: x, Group: sg.Group)))
            .GroupBy(x => x.Group, x => x.IndexedWorkload);

        var slotsByDay = model.Data.TimeSlots
            .Index()
            .GroupBy(x => x.Item.WeekDay);

        foreach (var groups in workloadsByGroup)
        {
            foreach (var dayGroup in slotsByDay)
            {
                var taskVars = groups
                    .SelectMany(x =>
                        Enumerable.Range(0, model.Data.Classrooms.Count)
                            .SelectMany(room =>
                                dayGroup.Select(slot => model.Lessons[x.Index, room, slot.Index])))
                    .ToArray();

                model.Model.Add(LinearExpr.Sum(taskVars) <= lessonLimit);
            }
        }
    }
}
