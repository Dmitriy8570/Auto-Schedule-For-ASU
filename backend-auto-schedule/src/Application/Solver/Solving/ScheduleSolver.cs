using Application.Solver.Model;
using Google.OrTools.Sat;

namespace Application.Solver.Solving;

/// <summary>
/// Обёртка над OR-Tools CP-SAT: настраивает параметры, решает модель и извлекает
/// булевы переменные Lessons[w, r, t] в список назначений.
/// </summary>
public sealed class ScheduleSolver : IScheduleSolver
{
    public ScheduleSolution Solve(ScheduleModel model, SolverOptions? options = null)
    {
        options ??= SolverOptions.Default;

        var solver = new CpSolver { StringParameters = options.ToStringParameters() };
        var status = Map(solver.Solve(model.Model));

        IReadOnlyList<Assignment> assignments = status is ScheduleSolveStatus.Optimal or ScheduleSolveStatus.Feasible
            ? Extract(model, solver)
            : Array.Empty<Assignment>();

        return new ScheduleSolution(status, assignments, solver.ObjectiveValue, solver.WallTime());
    }

    private static List<Assignment> Extract(ScheduleModel model, CpSolver solver)
    {
        var result = new List<Assignment>();
        for (int w = 0; w < model.WorkloadCount; w++)
            for (int r = 0; r < model.ClassroomCount; r++)
                for (int t = 0; t < model.TimeSlotCount; t++)
                    if (solver.Value(model.Lessons[w, r, t]) > 0.5)
                        result.Add(new Assignment(w, r, t));
        return result;
    }

    private static ScheduleSolveStatus Map(CpSolverStatus status) => status switch
    {
        CpSolverStatus.Optimal => ScheduleSolveStatus.Optimal,
        CpSolverStatus.Feasible => ScheduleSolveStatus.Feasible,
        CpSolverStatus.Infeasible => ScheduleSolveStatus.Infeasible,
        _ => ScheduleSolveStatus.Unknown
    };
}
