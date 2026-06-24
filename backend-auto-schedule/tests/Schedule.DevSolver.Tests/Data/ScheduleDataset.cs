using Application.Solver.Model;
using Domain.calendar;
using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.common;
using Domain.university.groups;
using Domain.university.teachers;
using Domain.workload;

namespace Schedule.DevSolver.Tests.Data;

using Schedule.DevSolver.Tests; // TestWorkloads.ToItems

/// <summary>
/// Полный набор сущностей задачи в виде, удобном и для солвера (через <see cref="ToScheduleData"/>),
/// и для выгрузки отчётов. Заполняется загрузчиком <see cref="UniversityDataLoader"/> из файлов.
///
/// Модель работает на одной шаблонной неделе (6 рабочих дней × 8 пар); эта неделя повторяется на
/// протяжении всего семестра, поэтому одно решение задаёт расписание на семестр.
/// </summary>
public sealed class ScheduleDataset
{
    /// <summary>Расписание звонков: номер пары → интервал времени.</summary>
    public static readonly IReadOnlyDictionary<int, string> BellSchedule = new Dictionary<int, string>
    {
        [1] = "08:00–09:30",
        [2] = "09:40–11:10",
        [3] = "11:20–12:50",
        [4] = "13:30–15:00",
        [5] = "15:10–16:40",
        [6] = "16:50–18:20",
        [7] = "18:30–20:00",
        [8] = "20:10–21:40",
    };

    public DateOnly SemesterStart { get; set; } = new(2025, 9, 1);
    public int SemesterWeeks { get; set; } = 18;

    public Institute Institute { get; set; } = null!;
    public List<Building> Buildings { get; } = new();
    public List<Domain.constraints.equipments.Equipment> Equipments { get; } = new();
    public List<Classroom> Classrooms { get; } = new();
    public List<Department> Departments { get; } = new();
    public List<Teacher> Teachers { get; } = new();
    public List<Subject> Subjects { get; } = new();
    public List<Group> Groups { get; } = new();
    public List<AcademicStream> Streams { get; } = new();
    public List<Curriculum> Curriculums { get; } = new();
    public List<SemesterWorkload> Workloads { get; } = new();
    public List<Week> Weeks { get; } = new();
    public List<WeekDay> WeekDays { get; } = new();
    public List<TimeSlot> TimeSlots { get; } = new();
    public List<ConstraintConfig> Penalties { get; } = new();

    public ScheduleData ToScheduleData() => new(Workloads.ToItems(), Classrooms, TimeSlots, Penalties);

    public WeekType WeekTypeOf(TimeSlot slot) => slot.WeekDay.Week.WeekType;

    /// <summary>Сколько пар в неделю приходится на каждую группу (для проверки нагрузки).</summary>
    public Dictionary<Guid, int> WeeklyPairsPerGroup()
    {
        var result = new Dictionary<Guid, int>();
        foreach (var w in Workloads)
        {
            int pairs = w.Hours / 2; // в одношаговой модели Hours/2 = число пар в неделю
            foreach (var sg in w.Curriculum.Stream.StreamGroups)
                result[sg.Group.Id] = result.GetValueOrDefault(sg.Group.Id) + pairs;
        }
        return result;
    }
}
