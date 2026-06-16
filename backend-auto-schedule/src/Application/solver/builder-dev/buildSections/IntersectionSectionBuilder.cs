using Application.Solver.BuilderDev.BuilderInterface;
using Application.Solver.ModelDev;
using Google.OrTools.Sat;

namespace Application.Solver.BuilderDev.BuildSections;

/// <summary>
/// Жёсткое ограничение запрета пересечений ресурсов: в один временной слот не может быть
/// двух занятий в одной аудитории, у одного преподавателя или у одной учебной группы.
/// </summary>
public class IntersectionSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        ClassroomExclusivity(model);
        TeacherExclusivity(model);
        GroupExclusivity(model);
    }

    /// <summary>В одной аудитории в одном слоте — не более одного занятия.</summary>
    private static void ClassroomExclusivity(ScheduleModel model)
    {
        for (int r = 0; r < model.ClassroomCount; r++)
            for (int t = 0; t < model.TimeSlotCount; t++)
            {
                var literals = new List<ILiteral>();
                for (int w = 0; w < model.WorkloadCount; w++)
                    literals.Add(model.Lessons[w, r, t]);
                model.Model.AddAtMostOne(literals);
            }
    }

    /// <summary>Один преподаватель не ведёт два занятия одновременно.</summary>
    private static void TeacherExclusivity(ScheduleModel model)
    {
        var byTeacher = Enumerable.Range(0, model.WorkloadCount)
            .GroupBy(w => model.Data.SemesterWorkloads[w].Curriculum.Teacher);

        foreach (var group in byTeacher)
            SubjectExclusivity(model, group.ToList());
    }

    /// <summary>Одна группа не находится на двух занятиях одновременно.</summary>
    private static void GroupExclusivity(ScheduleModel model)
    {
        var byGroup = Enumerable.Range(0, model.WorkloadCount)
            .SelectMany(w => model.Data.SemesterWorkloads[w].Curriculum.Stream.StreamGroups
                .Select(sg => (Workload: w, sg.Group)))
            .GroupBy(x => x.Group, x => x.Workload);

        foreach (var group in byGroup)
            SubjectExclusivity(model, group.ToList());
    }

    /// <summary>
    /// Гарантирует, что субъект (преподаватель или группа) занят не более чем в одном
    /// (аудитория, слот) в каждый момент времени.
    /// </summary>
    private static void SubjectExclusivity(ScheduleModel model, IReadOnlyList<int> workloadIndices)
    {
        for (int t = 0; t < model.TimeSlotCount; t++)
        {
            var literals = new List<ILiteral>();
            foreach (int w in workloadIndices)
                for (int r = 0; r < model.ClassroomCount; r++)
                    literals.Add(model.Lessons[w, r, t]);

            model.Model.AddAtMostOne(literals);
        }
    }
}
