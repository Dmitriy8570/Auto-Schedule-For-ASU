using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Мягкое ограничение стабильности относительно прошлого семестра (вариант «C»):
/// для каждой нагрузки, у которой в прошлом семестре было аналогичное занятие,
/// штрафуется отклонение от прошлого корпуса и прошлых слотов. Это «якорь», который
/// при прочих равных удерживает расписание близким к предыдущему (привычно для людей)
/// и работает как подсказка солверу.
///
/// Веса намеренно малы (мягче <c>FavoriteBuilding</c>), чтобы якорь служил тай-брейкером,
/// а не подменял жёсткие и более значимые мягкие ограничения. Если <see cref="ScheduleData.Anchors"/>
/// не заданы — секция ничего не делает.
/// </summary>
public class PreviousScheduleAnchorSectionBuilder : IModelSectionBuilder
{
    /// <summary>Штраф за занятие вне корпуса, в котором оно шло в прошлом семестре.</summary>
    private const int BuildingDeviationPenalty = 2;

    /// <summary>Штраф за занятие вне слотов, в которых оно шло в прошлом семестре.</summary>
    private const int TimeSlotDeviationPenalty = 1;

    public void Build(ScheduleModel model)
    {
        var anchors = model.Data.Anchors;
        if (anchors is null || anchors.Count == 0) return;

        foreach (var anchor in anchors)
        {
            if (anchor.WorkloadIndex < 0 || anchor.WorkloadIndex >= model.WorkloadCount) continue;

            AnchorBuilding(model, anchor);
            AnchorTimeSlots(model, anchor);
        }
    }

    private static void AnchorBuilding(ScheduleModel model, WorkloadAnchor anchor)
    {
        if (anchor.PreferredBuildingId is not { } building) return;

        int w = anchor.WorkloadIndex;
        for (int r = 0; r < model.ClassroomCount; r++)
        {
            if (model.Data.Classrooms[r].BuildingId == building) continue;

            for (int t = 0; t < model.TimeSlotCount; t++)
                model.Objective.Add(LinearExpr.Term(model.Lessons[w, r, t], BuildingDeviationPenalty));
        }
    }

    private static void AnchorTimeSlots(ScheduleModel model, WorkloadAnchor anchor)
    {
        if (anchor.PreferredTimeSlotIds is null || anchor.PreferredTimeSlotIds.Count == 0) return;

        var preferred = anchor.PreferredTimeSlotIds as HashSet<Guid> ?? new HashSet<Guid>(anchor.PreferredTimeSlotIds);

        int w = anchor.WorkloadIndex;
        for (int t = 0; t < model.TimeSlotCount; t++)
        {
            if (preferred.Contains(model.Data.TimeSlots[t].Id)) continue;

            for (int r = 0; r < model.ClassroomCount; r++)
                model.Objective.Add(LinearExpr.Term(model.Lessons[w, r, t], TimeSlotDeviationPenalty));
        }
    }
}
