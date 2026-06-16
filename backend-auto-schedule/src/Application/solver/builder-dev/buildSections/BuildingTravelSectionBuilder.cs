using Application.Solver.BuilderDev.BuilderInterface;
using Application.Solver.ModelDev;
using Google.OrTools.Sat;

namespace Application.Solver.BuilderDev.BuildSections;

/// <summary>
/// Жёсткое ограничение перемещения между корпусами: один субъект (группа или преподаватель)
/// не может иметь два занятия в соседних парах одного дня в разных корпусах — на переход
/// между корпусами требуется время. Реализовано через вспомогательные переменные
/// «субъект занят в корпусе b в слоте t» и запрет их одновременной активности для соседних
/// слотов разных корпусов.
/// </summary>
public class BuildingTravelSectionBuilder : IModelSectionBuilder
{
    public void Build(ScheduleModel model)
    {
        var roomsByBuilding = Enumerable.Range(0, model.ClassroomCount)
            .GroupBy(r => model.Data.Classrooms[r].BuildingId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Соседние пары слотов в пределах одного дня.
        var adjacentSlots = model.Data.TimeSlots
            .Select((slot, index) => (slot, index))
            .GroupBy(x => x.slot.WeekDay)
            .SelectMany(day =>
            {
                var ordered = day.OrderBy(x => x.slot.Number).Select(x => x.index).ToList();
                return Enumerable.Range(0, ordered.Count - 1).Select(i => (Prev: ordered[i], Next: ordered[i + 1]));
            })
            .ToList();

        foreach (var (subjectKey, workloads) in EnumerateSubjects(model))
            ApplySubject(model, subjectKey, workloads, roomsByBuilding, adjacentSlots);
    }

    private static void ApplySubject(
        ScheduleModel model,
        string subjectKey,
        IReadOnlyList<int> workloads,
        Dictionary<Guid, List<int>> roomsByBuilding,
        IReadOnlyList<(int Prev, int Next)> adjacentSlots)
    {
        // inBuilding[(t, buildingId)] — субъект имеет занятие в корпусе в слоте t.
        var inBuilding = new Dictionary<(int Slot, Guid Building), BoolVar>();

        BoolVar Busy(int t, Guid building)
        {
            if (inBuilding.TryGetValue((t, building), out var existing))
                return existing;

            var v = model.Model.NewBoolVar($"inb_{subjectKey}_t{t}_b{building:N}");
            var lits = new List<ILiteral>();
            foreach (int r in roomsByBuilding[building])
                foreach (int w in workloads)
                    lits.Add(model.Lessons[w, r, t]);

            // v == OR(lits): если все ложны — v ложно; любой истинный ⇒ v истинно.
            model.Model.AddBoolOr(lits.Append(v.Not()).ToArray());
            foreach (var lit in lits)
                model.Model.AddImplication(lit, v);

            inBuilding[(t, building)] = v;
            return v;
        }

        var buildings = roomsByBuilding.Keys.ToList();
        foreach (var (prev, next) in adjacentSlots)
            for (int i = 0; i < buildings.Count; i++)
                for (int j = 0; j < buildings.Count; j++)
                {
                    if (i == j) continue;
                    // Запрет: занят в корпусе i в текущей паре и в корпусе j в следующей.
                    model.Model.AddBoolOr(new ILiteral[] { Busy(prev, buildings[i]).Not(), Busy(next, buildings[j]).Not() });
                }
    }

    /// <summary>Субъекты расписания — преподаватели и группы — с их индексами нагрузок.</summary>
    private static IEnumerable<(string Key, IReadOnlyList<int> Workloads)> EnumerateSubjects(ScheduleModel model)
    {
        var byTeacher = Enumerable.Range(0, model.WorkloadCount)
            .GroupBy(w => model.Data.SemesterWorkloads[w].Curriculum.Teacher.Id);
        foreach (var g in byTeacher)
            yield return ($"t{g.Key:N}", g.ToList());

        var byGroup = Enumerable.Range(0, model.WorkloadCount)
            .SelectMany(w => model.Data.SemesterWorkloads[w].Curriculum.Stream.StreamGroups
                .Select(sg => (Workload: w, sg.Group.Id)))
            .GroupBy(x => x.Id, x => x.Workload);
        foreach (var g in byGroup)
            yield return ($"g{g.Key:N}", g.ToList());
    }
}
