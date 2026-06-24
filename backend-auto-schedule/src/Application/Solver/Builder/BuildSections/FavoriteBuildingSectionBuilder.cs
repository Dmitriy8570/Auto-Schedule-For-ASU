using Application.Solver.Builder.BuilderInterface;
using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Builder.BuildSections;

/// <summary>
/// Мягкое ограничение предпочитаемого корпуса: если у учебного плана задан предпочтительный
/// корпус, назначение его занятий в аудитории других корпусов штрафуется.
/// </summary>
public class FavoriteBuildingSectionBuilder : IModelSectionBuilder
{
    private readonly SolverPenaltyWeights _weights;

    public FavoriteBuildingSectionBuilder(SolverPenaltyWeights? weights = null)
        => _weights = weights ?? SolverPenaltyWeights.Default;

    public void Build(ScheduleModel model)
    {
        for (int w = 0; w < model.WorkloadCount; w++)
        {
            var favorite = model.Data.Workloads[w].Curriculum.FavoriteBuildingId;
            if (favorite is null) continue;

            for (int r = 0; r < model.ClassroomCount; r++)
            {
                if (model.Data.Classrooms[r].BuildingId == favorite.Value) continue;

                for (int t = 0; t < model.TimeSlotCount; t++)
                    if (model.Lessons[w, r, t] is { } var)
                        model.Objective.Add(LinearExpr.Term(var, _weights.FavoriteBuilding));
            }
        }
    }
}
