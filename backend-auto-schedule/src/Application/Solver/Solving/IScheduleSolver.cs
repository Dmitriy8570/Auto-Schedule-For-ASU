using Application.Solver.Model;

namespace Application.Solver.Solving;

/// <summary>Запускает построенную модель расписания и извлекает решение.</summary>
public interface IScheduleSolver
{
    ScheduleSolution Solve(ScheduleModel model, SolverOptions? options = null);
}
