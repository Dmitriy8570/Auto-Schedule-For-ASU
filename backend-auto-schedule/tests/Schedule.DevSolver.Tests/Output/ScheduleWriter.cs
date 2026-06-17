using System.Text;
using Domain.calendar;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.teachers;
using Domain.university.groups;
using Domain.workload;
using Schedule.DevSolver.Tests.Data;

namespace Schedule.DevSolver.Tests.Output;

/// <summary>Одно назначение занятия: индексы в осях модели (нагрузка, аудитория, слот).</summary>
public readonly record struct Assignment(int Workload, int Room, int Slot);

/// <summary>Статистика прогона солвера для отчёта.</summary>
public readonly record struct SolveStats(string Status, double ObjectiveValue, double WallTimeSeconds, long Variables, long Branches);

/// <summary>
/// Превращает решение dev-солвера в удобочитаемые файлы: исходные данные + три папки
/// (преподаватели, группы, аудитории), по файлу на сущность.
/// </summary>
public sealed class ScheduleWriter
{
    private readonly ScheduleDataset _ds;
    private readonly IReadOnlyList<Assignment> _assignments;
    private readonly SolveStats _stats;
    private readonly string? _inputDir;

    public ScheduleWriter(ScheduleDataset ds, IReadOnlyList<Assignment> assignments, SolveStats stats, string? inputDir = null)
    {
        _ds = ds;
        _assignments = assignments;
        _stats = stats;
        _inputDir = inputDir;
    }

    public string Write()
    {
        var root = OutputRoot();
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        Directory.CreateDirectory(root);

        CopyInputFiles(EnsureDir(root, "input"));
        WriteReadme(root);
        WriteSourceData(Path.Combine(root, "00_исходные_данные.md"));
        WriteTeachers(EnsureDir(root, "Преподаватели"));
        WriteGroups(EnsureDir(root, "Группы"));
        WriteRooms(EnsureDir(root, "Аудитории"));
        return root;
    }

    private void CopyInputFiles(string dir)
    {
        if (_inputDir is null || !Directory.Exists(_inputDir)) return;
        foreach (var file in Directory.GetFiles(_inputDir, "*.json"))
            File.Copy(file, Path.Combine(dir, Path.GetFileName(file)), overwrite: true);
    }

    // ----------------------------------------------------------- доступ к сущностям по индексам
    private Curriculum Cur(int w) => _ds.Workloads[w].Curriculum;
    private Classroom Room(int r) => _ds.Classrooms[r];
    private TimeSlot Slot(int t) => _ds.TimeSlots[t];

    private static string TypeName(LessonType t) => t switch
    {
        LessonType.Lecture => "Лекция",
        LessonType.Seminar => "Семинар",
        LessonType.Laboratory => "Лабораторная",
        LessonType.Consultation => "Консультация",
        LessonType.Examination => "Экзамен",
        _ => t.ToString()
    };

    private static string DayName(WeekDayType d) => d switch
    {
        WeekDayType.Monday => "Понедельник",
        WeekDayType.Tuesday => "Вторник",
        WeekDayType.Wednesday => "Среда",
        WeekDayType.Thursday => "Четверг",
        WeekDayType.Friday => "Пятница",
        WeekDayType.Saturday => "Суббота",
        WeekDayType.Sunday => "Воскресенье",
        _ => d.ToString()
    };

    private static string WeekName(WeekType t) => t == WeekType.Red ? "Числитель (Red)" : "Знаменатель (Blue)";

    private IReadOnlyList<string> GroupsOf(int w) =>
        Cur(w).Stream.StreamGroups.Select(sg => sg.Group.Name).OrderBy(n => n).ToList();

    /// <summary>Полная строка занятия: время, предмет, тип, преподаватель, аудитория, группы.</summary>
    private string LessonLine(Assignment a)
    {
        var cur = Cur(a.Workload);
        var slot = Slot(a.Slot);
        var room = Room(a.Room);
        var groups = GroupsOf(a.Workload);
        var time = ScheduleDataset.BellSchedule[slot.Number];

        var flags = new List<string>();
        if (groups.Count > 1) flags.Add("ПОТОК");
        if (cur.Double) flags.Add("двойная");
        var flag = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";

        return $"пара {slot.Number} ({time}) — {cur.Subject.Name} ({TypeName(cur.LessonType)}){flag} | " +
               $"препод.: {cur.Teacher.Name} | ауд.: {room.Name} ({room.Building.Name}) | " +
               $"группы: {string.Join(", ", groups)}";
    }

    /// <summary>
    /// Печатает занятия из набора, сгруппированные по дню → паре. Если в данных несколько типов
    /// недель (числитель/знаменатель), они разносятся по секциям; для одной шаблонной недели
    /// печатается единый список (он повторяется весь семестр).
    /// </summary>
    private void AppendSchedule(StringBuilder sb, IEnumerable<Assignment> items)
    {
        var list = items.ToList();
        sb.AppendLine();
        if (list.Count == 0)
        {
            sb.AppendLine("_Занятий нет._");
            return;
        }

        var weekTypes = list.Select(a => _ds.WeekTypeOf(Slot(a.Slot))).Distinct().OrderBy(w => (int)w).ToList();

        foreach (var weekType in weekTypes)
        {
            var weekItems = list.Where(a => _ds.WeekTypeOf(Slot(a.Slot)) == weekType).ToList();
            if (weekTypes.Count > 1)
                sb.AppendLine($"### Неделя — {WeekName(weekType)}");

            var byDay = weekItems
                .GroupBy(a => Slot(a.Slot).WeekDay.DayOfWeek)
                .OrderBy(g => (int)g.Key);

            foreach (var day in byDay)
            {
                sb.AppendLine($"**{DayName(day.Key)}**");
                foreach (var a in day.OrderBy(x => Slot(x.Slot).Number))
                    sb.AppendLine($"- {LessonLine(a)}");
                sb.AppendLine();
            }
        }
    }

    // ----------------------------------------------------------- преподаватели
    private void WriteTeachers(string dir)
    {
        var index = new StringBuilder().AppendLine("# Расписание преподавателей").AppendLine();
        foreach (var teacher in _ds.Teachers.OrderBy(t => t.Name))
        {
            var items = _assignments.Where(a => Cur(a.Workload).Teacher.Id == teacher.Id).ToList();
            var sb = new StringBuilder();
            sb.AppendLine($"# Расписание — {teacher.Name}");
            sb.AppendLine($"Кафедра: {teacher.Department.Name}");
            sb.AppendLine($"Пар в неделю: {items.Count}");
            AppendSchedule(sb, items);

            var file = $"{Sanitize(teacher.Name)}.md";
            File.WriteAllText(Path.Combine(dir, file), sb.ToString(), Encoding.UTF8);
            index.AppendLine($"- [{teacher.Name}]({file}) — {teacher.Department.Name}, пар: {items.Count}");
        }
        File.WriteAllText(Path.Combine(dir, "_индекс.md"), index.ToString(), Encoding.UTF8);
    }

    // ----------------------------------------------------------- группы
    private void WriteGroups(string dir)
    {
        var index = new StringBuilder().AppendLine("# Расписание групп").AppendLine();
        foreach (var group in _ds.Groups.OrderBy(g => g.Name))
        {
            var items = _assignments
                .Where(a => Cur(a.Workload).Stream.StreamGroups.Any(sg => sg.Group.Id == group.Id))
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"# Расписание — группа {group.Name}");
            sb.AppendLine($"Смена: {ShiftName(group.Shift)}; студентов: {group.StudentCount}" +
                          (group.ParentGroup is not null ? $"; подгруппа группы {group.ParentGroup.Name}" : ""));
            sb.AppendLine($"Пар в неделю: {items.Count}");
            AppendSchedule(sb, items);

            var file = $"{Sanitize(group.Name)}.md";
            File.WriteAllText(Path.Combine(dir, file), sb.ToString(), Encoding.UTF8);
            index.AppendLine($"- [{group.Name}]({file}) — {ShiftName(group.Shift)}, пар: {items.Count}");
        }
        File.WriteAllText(Path.Combine(dir, "_индекс.md"), index.ToString(), Encoding.UTF8);
    }

    private static string ShiftName(Shift s) => s switch
    {
        Shift.First => "первая",
        Shift.Second => "вторая",
        Shift.Evening => "вечерняя",
        _ => s.ToString()
    };

    // ----------------------------------------------------------- аудитории
    private void WriteRooms(string dir)
    {
        var index = new StringBuilder().AppendLine("# Расписание аудиторий").AppendLine();
        for (int r = 0; r < _ds.Classrooms.Count; r++)
        {
            var room = _ds.Classrooms[r];
            int rr = r;
            var items = _assignments.Where(a => a.Room == rr).ToList();

            var equip = room.EquipmentRooms.Select(e => e.Equipment.Name).ToList();
            var sb = new StringBuilder();
            sb.AppendLine($"# Расписание — аудитория {room.Name}");
            sb.AppendLine($"Корпус: {room.Building.Name}; вместимость: {room.Capacity}" +
                          (equip.Count > 0 ? $"; оснащение: {string.Join(", ", equip)}" : ""));
            sb.AppendLine($"Пар в неделю: {items.Count}");
            AppendSchedule(sb, items);

            var file = $"{Sanitize(room.Name)}.md";
            File.WriteAllText(Path.Combine(dir, file), sb.ToString(), Encoding.UTF8);
            index.AppendLine($"- [{room.Name}]({file}) — {room.Building.Name}, пар: {items.Count}");
        }
        File.WriteAllText(Path.Combine(dir, "_индекс.md"), index.ToString(), Encoding.UTF8);
    }

    // ----------------------------------------------------------- исходные данные
    private void WriteSourceData(string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Исходные данные генерации расписания");
        sb.AppendLine();
        sb.AppendLine($"**Институт:** {_ds.Institute.Name}");
        sb.AppendLine($"**Семестр:** старт {_ds.SemesterStart:dd.MM.yyyy}, {_ds.SemesterWeeks} недель.");
        sb.AppendLine($"**Модель:** одна шаблонная неделя (повторяется весь семестр), {_ds.TimeSlots.Count} временных слотов, " +
                      $"{_ds.Classrooms.Count} аудиторий, {_ds.Workloads.Count} нагрузок, " +
                      $"{_ds.Teachers.Count} преподавателей, {_ds.Groups.Count} групп.");
        sb.AppendLine();

        sb.AppendLine("## Расписание звонков");
        foreach (var (n, time) in ScheduleDataset.BellSchedule.OrderBy(x => x.Key))
            sb.AppendLine($"- Пара {n}: {time}");
        sb.AppendLine();

        sb.AppendLine("## Корпуса и аудитории");
        foreach (var b in _ds.Buildings)
        {
            sb.AppendLine($"### {b.Name}");
            sb.AppendLine("| Аудитория | Вместимость | Оснащение |");
            sb.AppendLine("|---|---:|---|");
            foreach (var room in _ds.Classrooms.Where(c => c.BuildingId == b.Id))
            {
                var eq = room.EquipmentRooms.Select(e => e.Equipment.Name);
                sb.AppendLine($"| {room.Name} | {room.Capacity} | {string.Join(", ", eq)} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## Кафедры и преподаватели");
        foreach (var d in _ds.Departments)
        {
            sb.AppendLine($"### {d.Name}");
            foreach (var t in _ds.Teachers.Where(x => x.DepartmentId == d.Id))
            {
                var unavail = t.TeacherAvailabilities
                    .Select(a => $"{DayName(a.DayOfWeek)} пара {a.NumberLesson} (штраф {a.Penalty})");
                var note = t.TeacherAvailabilities.Count > 0 ? $" — нежелательно: {string.Join("; ", unavail)}" : "";
                sb.AppendLine($"- {t.Name}{note}");
            }
            sb.AppendLine();
        }

        var weeklyPairs = _ds.WeeklyPairsPerGroup();

        sb.AppendLine("## Учебные группы");
        sb.AppendLine($"Всего групп: {_ds.Groups.Count}. Пар в неделю у основной группы — минимум " +
                      $"{_ds.Groups.Where(g => g.ParentGroup is null).Min(g => weeklyPairs.GetValueOrDefault(g.Id))}.");
        sb.AppendLine();
        sb.AppendLine("| Группа | Смена | Студентов | Родительская | Пар/нед |");
        sb.AppendLine("|---|---|---:|---|---:|");
        foreach (var g in _ds.Groups)
            sb.AppendLine($"| {g.Name} | {ShiftName(g.Shift)} | {g.StudentCount} | {g.ParentGroup?.Name ?? "—"} | " +
                          $"{weeklyPairs.GetValueOrDefault(g.Id)} |");
        sb.AppendLine();

        sb.AppendLine($"## Учебные планы (нагрузки): {_ds.Workloads.Count}");
        sb.AppendLine("| Предмет | Тип | Преподаватель | Группы | Пар/нед | Двойная | Паралл. | Оборуд. | Корпус |");
        sb.AppendLine("|---|---|---|---|---:|:---:|:---:|---|---|");
        foreach (var w in _ds.Workloads)
        {
            var c = w.Curriculum;
            var groups = string.Join(", ", c.Stream.StreamGroups.Select(s => s.Group.Name));
            var eq = string.Join(", ", c.NeededEquipments.Select(n => n.Equipment.Name));
            var fav = c.FavoriteBuilding?.Name ?? "—";
            sb.AppendLine($"| {c.Subject.Name} | {TypeName(c.LessonType)} | {c.Teacher.Name} | {groups} | " +
                          $"{w.Hours / 2} | {(c.Double ? "да" : "—")} | {(c.Parallelism ? "да" : "—")} | " +
                          $"{(eq.Length == 0 ? "—" : eq)} | {fav} |");
        }
        sb.AppendLine();

        sb.AppendLine("## Веса мягких ограничений");
        foreach (var p in _ds.Penalties)
            sb.AppendLine($"- {p.ConstraintType}: {p.Penalty}");
        sb.AppendLine();

        sb.AppendLine("## Недоступность аудиторий (мягко)");
        foreach (var room in _ds.Classrooms.Where(c => c.ClassroomAvailabilities.Count > 0))
            foreach (var a in room.ClassroomAvailabilities)
                sb.AppendLine($"- {room.Name}: {DayName(a.DayOfWeek)} пара {a.NumberLesson} (штраф {a.Penalty})");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    // ----------------------------------------------------------- отчёт
    private void WriteReadme(string root)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Результат генерации расписания (dev-солвер)");
        sb.AppendLine();
        sb.AppendLine("Сгенерировано тестом `SemesterScheduleGenerationTests`. Входные данные читаются из");
        sb.AppendLine("файлов `input/teachers.json`, `input/groups.json`, `input/classrooms.json`. Решение");
        sb.AppendLine("задаёт расписание на одну неделю, которое повторяется весь семестр.");
        sb.AppendLine();
        sb.AppendLine("## Статистика солвера");
        sb.AppendLine($"- Статус: **{_stats.Status}**");
        sb.AppendLine($"- Значение целевой функции (сумма штрафов): {_stats.ObjectiveValue:0}");
        sb.AppendLine($"- Время решения: {_stats.WallTimeSeconds:0.00} с");
        sb.AppendLine($"- Переменных в модели: {_stats.Variables}");
        sb.AppendLine($"- Назначено занятий за неделю: {_assignments.Count}");
        sb.AppendLine();
        sb.AppendLine("## Структура");
        sb.AppendLine("- `input/` — исходные файлы преподавателей, групп и аудиторий (JSON).");
        sb.AppendLine("- `00_исходные_данные.md` — сводка входных данных и синтезированных нагрузок.");
        sb.AppendLine("- `Преподаватели/` — по файлу на преподавателя.");
        sb.AppendLine("- `Группы/` — по файлу на группу.");
        sb.AppendLine("- `Аудитории/` — по файлу на аудиторию.");
        sb.AppendLine();
        sb.AppendLine("В каждой строке занятия: время, предмет, тип, преподаватель, аудитория и все группы");
        sb.AppendLine("(для потоковых пар помечено `[ПОТОК]`, для двойных — `[двойная]`).");
        sb.AppendLine();
        sb.AppendLine("## Заложенные граничные ситуации");
        sb.AppendLine("- **Потоковые лекции** на весь курс направления (несколько групп в одной паре).");
        sb.AppendLine("- **Двойные лабораторные** (электроника) у подгрупп — спаренные соседние слоты.");
        sb.AppendLine("- **Параллельные подгруппы** (`…/1` и `…/2`) — стремятся в одну пару.");
        sb.AppendLine("- **Вторая смена** (пары 5–6) и **вечерние группы** (пары 7–8).");
        sb.AppendLine("- **Требования к оборудованию**: компьютерные классы, лаборатории, проекторы.");
        sb.AppendLine("- **Предпочитаемый корпус** (мягко) и **переезд между корпусами** (жёстко).");
        sb.AppendLine("- **Недоступность** преподавателей (пн, пара 1) и аудиторий (лаборатории, сб вечер).");
        sb.AppendLine();
        sb.AppendLine("> Замечание dev-модели: студенты родительской группы и её подгрупп моделируются как");
        sb.AppendLine("> разные субъекты, поэтому пересечение «лекция родителя ↔ лаб. подгруппы» не запрещается");
        sb.AppendLine("> жёстко. Это известное упрощение текущей версии солвера.");

        File.WriteAllText(Path.Combine(root, "README.md"), sb.ToString(), Encoding.UTF8);
    }

    // ----------------------------------------------------------- инфраструктура
    private static string EnsureDir(string root, string name)
    {
        var dir = Path.Combine(root, name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace('/', '_');
    }

    private static string OutputRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend-auto-schedule.slnx")))
            dir = dir.Parent;

        var baseDir = dir?.FullName ?? AppContext.BaseDirectory;
        return Path.Combine(baseDir, "TestOutput", "schedule-dev");
    }
}
