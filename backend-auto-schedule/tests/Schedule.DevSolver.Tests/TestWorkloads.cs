using Application.Solver.Model;
using Domain.workload;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Хелпер тестов: проекция доменной <see cref="SemesterWorkload"/> в ось модели <see cref="WorkloadItem"/>.
/// Тестовые билдеры собирают доменные нагрузки (часы + учебный план), а модель с переходом на
/// понедельную генерацию принимает <see cref="WorkloadItem"/> — конвертация здесь.
/// </summary>
internal static class TestWorkloads
{
    public static WorkloadItem ToItem(this SemesterWorkload w) => new(w.Hours, w.Curriculum);

    public static IReadOnlyList<WorkloadItem> ToItems(this IEnumerable<SemesterWorkload> workloads) =>
        workloads.Select(ToItem).ToList();
}
