using Google.OrTools.Sat;

namespace Application.Solver.Model;

/// <summary>
/// Модель SAT-задачи составления расписания (экспериментальная версия).
/// Содержит входные данные, трёхмерный массив булевых переменных и список слагаемых
/// целевой функции, которые наполняются секционными строителями.
/// </summary>
public class ScheduleModel
{
    public ScheduleData Data { get; }

    /// <summary>
    /// Трёхмерный массив булевых переменных: [нагрузка, аудитория, временной слот].
    /// Lessons[w, r, t] == 1 означает, что нагрузка w проводится в аудитории r в слоте t.
    /// <c>null</c> — тройка статически запрещена (прунинг, см. <see cref="Builder.BuildSections.VariablesSectionBuilder"/>):
    /// переменная не создаётся, все читатели обязаны такие ячейки пропускать.
    /// </summary>
    public BoolVar?[,,] Lessons { get; }

    public CpModel Model { get; }

    /// <summary>Слагаемые целевой функции (штрафы), которые солвер минимизирует.</summary>
    public List<LinearExpr> Objective { get; }

    public int WorkloadCount => Data.SemesterWorkloads.Count;
    public int ClassroomCount => Data.Classrooms.Count;
    public int TimeSlotCount => Data.TimeSlots.Count;

    public ScheduleModel(ScheduleData data)
    {
        Data = data;
        Model = new CpModel();
        Lessons = new BoolVar?[data.SemesterWorkloads.Count, data.Classrooms.Count, data.TimeSlots.Count];
        Objective = new List<LinearExpr>();
    }
}
