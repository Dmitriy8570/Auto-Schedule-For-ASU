using Domain.calendar;
using Domain.constraints;
using Domain.constraints.equipments;
using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.common;
using Domain.university.groups;
using Domain.university.teachers;
using Domain.workload;
using Schedule.DevSolver.Tests.Reflection;

namespace Schedule.DevSolver.Tests.Data;

/// <summary>
/// Загружает входные файлы (преподаватели, группы, аудитории), создаёт из них доменные сущности
/// и достраивает учебные планы (нагрузки) так, чтобы у каждой основной группы было не менее
/// <see cref="MinWeeklyPairs"/> пар в неделю. Результат — заполненный <see cref="ScheduleDataset"/>,
/// готовый к подаче в dev-солвер.
/// </summary>
public sealed class UniversityDataLoader
{
    public const int MinWeeklyPairs = 8;
    private const int PairsPerDay = 8;

    private static readonly WeekDayType[] WorkingDays =
    {
        WeekDayType.Monday, WeekDayType.Tuesday, WeekDayType.Wednesday,
        WeekDayType.Thursday, WeekDayType.Friday, WeekDayType.Saturday,
    };

    // Профильные дисциплины кафедр (для лекций/семинаров/лабораторных направления).
    private static readonly Dictionary<string, string[]> ProgramSubjects = new()
    {
        ["ПИ"] = new[] { "Программирование", "ООП", "Базы данных", "Веб-разработка", "Тестирование ПО", "Архитектура ПО" },
        ["ИВТ"] = new[] { "Схемотехника", "Микропроцессоры", "Операционные системы", "Компьютерные сети", "Электроника", "Архитектура ЭВМ" },
        ["ИБ"] = new[] { "Криптография", "Защита информации", "Сетевая безопасность", "Безопасность ОС" },
        ["ПМ"] = new[] { "Численные методы", "Исследование операций", "Математическое моделирование", "Методы оптимизации" },
        ["БИ"] = new[] { "Информационные системы", "Управление проектами", "Бизнес-аналитика", "Корпоративные БД" },
    };

    // Дисциплины, требующие компьютерного класса.
    private static readonly HashSet<string> ComputerSubjects = new()
    {
        "Программирование", "ООП", "Базы данных", "Веб-разработка", "Тестирование ПО",
        "Операционные системы", "Компьютерные сети", "Численные методы", "Информационные системы",
        "Корпоративные БД", "Криптография", "Сетевая безопасность",
    };

    private static readonly string[] MathSubjects =
        { "Математический анализ", "Линейная алгебра", "Дискретная математика", "Теория вероятностей", "Высшая математика" };
    private static readonly string[] HumanitySubjects = { "Философия", "История", "Экономика", "Социология" };

    private const string MathDept = "Кафедра высшей математики";
    private const string LangDept = "Кафедра иностранных языков";
    private const string HumDept = "Кафедра гуманитарных дисциплин";

    private static readonly Dictionary<string, string> ProgramHomeBuilding = new()
    {
        ["ПИ"] = "Корпус Б", ["ИВТ"] = "Корпус Б", ["ИБ"] = "Корпус В", ["ПМ"] = "Корпус А", ["БИ"] = "Корпус А",
    };

    private readonly ScheduleDataset _ds = new();
    private readonly Dictionary<string, Subject> _subjects = new();
    private readonly Dictionary<string, Equipment> _equipment = new();
    private readonly Dictionary<string, Building> _buildings = new();
    private readonly Dictionary<string, Department> _departments = new();
    private readonly Dictionary<string, Group> _groups = new();
    private readonly Dictionary<string, List<Teacher>> _teachersByDept = new();
    private readonly Dictionary<Guid, int> _teacherLoad = new();
    private readonly Dictionary<string, int> _subjectToDept = new();

    public static ScheduleDataset Load(string dir)
    {
        var (teachers, groups, classrooms) = DataFileGenerator.Read(dir);
        return new UniversityDataLoader().Build(teachers, groups, classrooms);
    }

    private ScheduleDataset Build(List<TeacherDto> teachers, List<GroupDto> groups, List<ClassroomDto> classrooms)
    {
        BuildInstituteAndDepartments(teachers);
        BuildTeachers(teachers);
        BuildClassrooms(classrooms);
        BuildGroups(groups);
        BuildCalendar();
        BuildPenalties();
        BuildSoftConstraints();
        BuildCurriculums();
        return _ds;
    }

    // ----------------------------------------------------------------- институт, кафедры
    private void BuildInstituteAndDepartments(List<TeacherDto> teachers)
    {
        _ds.Institute = DomainFactory.New<Institute>()
            .Set(nameof(Institute.Id), Guid.NewGuid())
            .Set(nameof(Institute.Name), "Технологический университет (ИТ-институт)");

        foreach (var deptName in teachers.Select(t => t.Department).Distinct())
        {
            var dept = DomainFactory.New<Department>()
                .Set(nameof(Department.Id), Guid.NewGuid())
                .Set(nameof(Department.Name), deptName)
                .Set(nameof(Department.InstituteId), _ds.Institute.Id)
                .Set(nameof(Department.Institute), _ds.Institute);
            _departments[deptName] = dept;
            _ds.Departments.Add(dept);
        }
    }

    private void BuildTeachers(List<TeacherDto> teachers)
    {
        foreach (var dto in teachers)
        {
            var dept = _departments[dto.Department];
            var teacher = DomainFactory.New<Teacher>()
                .Set(nameof(Teacher.Id), Guid.NewGuid())
                .Set(nameof(Teacher.Name), dto.Name)
                .Set(nameof(Teacher.DepartmentId), dept.Id)
                .Set(nameof(Teacher.Department), dept)
                .Set(nameof(Teacher.TeacherAvailabilities), new List<TeacherAvailability>());
            _ds.Teachers.Add(teacher);
            _teacherLoad[teacher.Id] = 0;
            if (!_teachersByDept.TryGetValue(dto.Department, out var list))
                _teachersByDept[dto.Department] = list = new List<Teacher>();
            list.Add(teacher);
        }
    }

    // ----------------------------------------------------------------- аудитории, оборудование
    private Equipment Equip(string name)
    {
        if (_equipment.TryGetValue(name, out var e)) return e;
        e = DomainFactory.New<Equipment>()
            .Set(nameof(Equipment.Id), Guid.NewGuid())
            .Set(nameof(Equipment.Name), name);
        _equipment[name] = e;
        _ds.Equipments.Add(e);
        return e;
    }

    private Building Build(string name)
    {
        if (_buildings.TryGetValue(name, out var b)) return b;
        b = DomainFactory.New<Building>()
            .Set(nameof(Building.Id), Guid.NewGuid())
            .Set(nameof(Building.Name), name);
        _buildings[name] = b;
        _ds.Buildings.Add(b);
        return b;
    }

    private void BuildClassrooms(List<ClassroomDto> classrooms)
    {
        foreach (var dto in classrooms)
        {
            var building = Build(dto.Building);
            var room = DomainFactory.New<Classroom>()
                .Set(nameof(Classroom.Id), Guid.NewGuid())
                .Set(nameof(Classroom.Name), dto.Name)
                .Set(nameof(Classroom.Capacity), dto.Capacity)
                .Set(nameof(Classroom.BuildingId), building.Id)
                .Set(nameof(Classroom.Building), building)
                .Set(nameof(Classroom.ClassroomAvailabilities), new List<ClassroomAvailability>());

            var equipRooms = dto.Equipment.Select(name =>
            {
                var e = Equip(name);
                return DomainFactory.New<EquipmentRoom>()
                    .Set(nameof(EquipmentRoom.EquipmentId), e.Id)
                    .Set(nameof(EquipmentRoom.Equipment), e)
                    .Set(nameof(EquipmentRoom.ClassroomId), room.Id)
                    .Set(nameof(EquipmentRoom.Classroom), room);
            }).ToList();
            room.Set(nameof(Classroom.EquipmentRooms), equipRooms);

            _ds.Classrooms.Add(room);
        }
    }

    // ----------------------------------------------------------------- группы
    private static Shift ParseShift(string s) => s switch
    {
        "Second" => Shift.Second,
        "Evening" => Shift.Evening,
        _ => Shift.First,
    };

    private void BuildGroups(List<GroupDto> groups)
    {
        // Сначала основные группы, затем подгруппы (нужен родитель).
        foreach (var dto in groups.Where(g => g.Parent is null))
            CreateGroup(dto, parent: null);
        foreach (var dto in groups.Where(g => g.Parent is not null))
            CreateGroup(dto, parent: _groups[dto.Parent!]);
    }

    private void CreateGroup(GroupDto dto, Group? parent)
    {
        var group = DomainFactory.New<Group>()
            .Set(nameof(Group.Id), Guid.NewGuid())
            .Set(nameof(Group.Name), dto.Name)
            .Set(nameof(Group.Shift), ParseShift(dto.Shift))
            .Set(nameof(Group.StudentCount), dto.Students)
            .Set(nameof(Group.ParentGroupId), parent?.Id)
            .Set(nameof(Group.ParentGroup), parent)
            .Set(nameof(Group.StreamGroups), new List<StreamGroups>());
        _groups[dto.Name] = group;
        _ds.Groups.Add(group);
    }

    // ----------------------------------------------------------------- календарь
    private void BuildCalendar()
    {
        var week = DomainFactory.New<Week>()
            .Set(nameof(Week.Id), Guid.NewGuid())
            .Set(nameof(Week.WeekType), WeekType.Red)
            .Set(nameof(Week.WeekDays), new List<WeekDay>());
        _ds.Weeks.Add(week);

        foreach (var day in WorkingDays)
        {
            var weekDay = DomainFactory.New<WeekDay>()
                .Set(nameof(WeekDay.Id), Guid.NewGuid())
                .Set(nameof(WeekDay.WeekId), week.Id)
                .Set(nameof(WeekDay.Week), week)
                .Set(nameof(WeekDay.DayOfWeek), day)
                .Set(nameof(WeekDay.TimeSlots), new List<TimeSlot>());
            _ds.WeekDays.Add(weekDay);
            week.WeekDays.Add(weekDay);

            for (int n = 1; n <= PairsPerDay; n++)
            {
                var slot = DomainFactory.New<TimeSlot>()
                    .Set(nameof(TimeSlot.Id), Guid.NewGuid())
                    .Set(nameof(TimeSlot.WeekDay), weekDay)
                    .Set(nameof(TimeSlot.WeekDayId), weekDay.Id)
                    .Set(nameof(TimeSlot.Number), n);
                weekDay.TimeSlots.Add(slot);
                _ds.TimeSlots.Add(slot);
            }
        }
    }

    private void BuildPenalties()
    {
        void Add(ConstraintType type, int penalty) => _ds.Penalties.Add(DomainFactory.New<ConstraintConfig>()
            .Set(nameof(ConstraintConfig.Id), Guid.NewGuid())
            .Set(nameof(ConstraintConfig.ConstraintType), type)
            .Set(nameof(ConstraintConfig.Penalty), penalty));

        Add(ConstraintType.StudentGap, 4);
        Add(ConstraintType.TeacherGap, 3);
        Add(ConstraintType.ClassroomAvailability, 1);
        Add(ConstraintType.TeacherAvailability, 1);
    }

    /// <summary>Несколько мягких ограничений доступности — для проверки соответствующих секций.</summary>
    private void BuildSoftConstraints()
    {
        // Первые два преподавателя не любят первую пару понедельника.
        foreach (var t in _ds.Teachers.Take(2))
            t.TeacherAvailabilities.Add(DomainFactory.New<TeacherAvailability>()
                .Set(nameof(TeacherAvailability.Id), Guid.NewGuid())
                .Set(nameof(TeacherAvailability.TeacherId), t.Id)
                .Set(nameof(TeacherAvailability.Teacher), t)
                .Set(nameof(TeacherAvailability.DayOfWeek), WeekDayType.Monday)
                .Set(nameof(TeacherAvailability.NumberLesson), 1)
                .Set(nameof(TeacherAvailability.Penalty), 5));

        // Лаборатории нежелательны в субботу вечером.
        foreach (var room in _ds.Classrooms.Where(c => c.Name.EndsWith("-401")))
            room.ClassroomAvailabilities.Add(DomainFactory.New<ClassroomAvailability>()
                .Set(nameof(ClassroomAvailability.Id), Guid.NewGuid())
                .Set(nameof(ClassroomAvailability.ClassroomId), room.Id)
                .Set(nameof(ClassroomAvailability.DayOfWeek), WeekDayType.Saturday)
                .Set(nameof(ClassroomAvailability.NumberLesson), 6)
                .Set(nameof(ClassroomAvailability.Penalty), 4));
    }

    // ----------------------------------------------------------------- учебные планы
    private Subject GetOrAddSubject(string name)
    {
        if (_subjects.TryGetValue(name, out var s)) return s;
        s = DomainFactory.New<Subject>()
            .Set(nameof(Subject.Id), Guid.NewGuid())
            .Set(nameof(Subject.Name), name);
        _subjects[name] = s;
        _ds.Subjects.Add(s);
        return s;
    }

    private Teacher PickTeacher(string department)
    {
        var pool = _teachersByDept[department];
        var teacher = pool.MinBy(t => _teacherLoad[t.Id])!;
        return teacher;
    }

    private AcademicStream MakeStream(IEnumerable<Group> groups)
    {
        var stream = DomainFactory.New<AcademicStream>()
            .Set(nameof(AcademicStream.Id), Guid.NewGuid())
            .Set(nameof(AcademicStream.StreamGroups), new List<StreamGroups>());

        var links = new List<StreamGroups>();
        int students = 0;
        foreach (var g in groups)
        {
            students += g.StudentCount;
            var link = DomainFactory.New<StreamGroups>()
                .Set(nameof(StreamGroups.GroupId), g.Id)
                .Set(nameof(StreamGroups.Group), g)
                .Set(nameof(StreamGroups.StreamId), stream.Id)
                .Set(nameof(StreamGroups.Stream), stream);
            links.Add(link);
            g.StreamGroups.Add(link);
        }
        stream.Set(nameof(AcademicStream.StreamGroups), links)
              .Set(nameof(AcademicStream.StudentsCount), students);
        _ds.Streams.Add(stream);
        return stream;
    }

    private void AddCurriculum(
        Teacher teacher, string subjectName, LessonType type, AcademicStream stream, int weeklyPairs,
        bool doubleLesson = false, bool parallelism = false, string? favoriteBuilding = null, string? neededEquipment = null)
    {
        var subject = GetOrAddSubject(subjectName);
        var curriculum = DomainFactory.New<Curriculum>()
            .Set(nameof(Curriculum.Id), Guid.NewGuid())
            .Set(nameof(Curriculum.Teacher), teacher)
            .Set(nameof(Curriculum.TeacherId), teacher.Id)
            .Set(nameof(Curriculum.Subject), subject)
            .Set(nameof(Curriculum.SubjectId), subject.Id)
            .Set(nameof(Curriculum.Stream), stream)
            .Set(nameof(Curriculum.StreamId), stream.Id)
            .Set(nameof(Curriculum.LessonType), type)
            .Set(nameof(Curriculum.Double), doubleLesson)
            .Set(nameof(Curriculum.Parallelism), parallelism)
            .Set(nameof(Curriculum.NeededEquipments), new List<NeededEquipment>());

        if (favoriteBuilding is not null && _buildings.TryGetValue(favoriteBuilding, out var b))
            curriculum.Set(nameof(Curriculum.FavoriteBuildingId), b.Id).Set(nameof(Curriculum.FavoriteBuilding), b);

        if (neededEquipment is not null)
        {
            var e = Equip(neededEquipment);
            curriculum.Set(nameof(Curriculum.NeededEquipments), new List<NeededEquipment>
            {
                DomainFactory.New<NeededEquipment>()
                    .Set(nameof(NeededEquipment.CurriculumId), curriculum.Id)
                    .Set(nameof(NeededEquipment.Curriculum), curriculum)
                    .Set(nameof(NeededEquipment.EquipmentId), e.Id)
                    .Set(nameof(NeededEquipment.Equipment), e)
            });
        }

        _ds.Curriculums.Add(curriculum);
        _ds.Workloads.Add(DomainFactory.New<SemesterWorkload>()
            .Set(nameof(SemesterWorkload.Id), Guid.NewGuid())
            .Set(nameof(SemesterWorkload.Hours), 2 * weeklyPairs) // одношаговая модель: Hours/2 = пар в неделю
            .Set(nameof(SemesterWorkload.Curriculum), curriculum)
            .Set(nameof(SemesterWorkload.CurriculumId), curriculum.Id));

        _teacherLoad[teacher.Id] += weeklyPairs;
    }

    private void BuildCurriculums()
    {
        var mains = _ds.Groups.Where(g => g.ParentGroup is null).ToList();

        // --- Потоковые лекции по (направление, курс, СМЕНА) ---
        // Смена обязательно одинаковая у всех групп потока, иначе для лекции не найдётся
        // ни одного допустимого по смене слота.
        var lectureStreams = mains
            .GroupBy(g => (Program: ProgramOf(g.Name), Course: CourseOf(g.Name), g.Shift))
            .OrderBy(g => g.Key.Program).ThenBy(g => g.Key.Course).ThenBy(g => g.Key.Shift);

        foreach (var grp in lectureStreams)
        {
            string program = grp.Key.Program;
            int course = grp.Key.Course;
            string home = ProgramHomeBuilding.GetValueOrDefault(program);
            var core = ProgramSubjects[program];

            var stream = MakeStream(grp.ToList());
            var lectureCore = core[(course - 1) % core.Length];
            var lectureGeneral = (course % 2 == 0 ? MathSubjects : HumanitySubjects)[course % 4];
            AddCurriculum(PickTeacher(DeptOfProgram(program)), lectureCore, LessonType.Lecture, stream, 1,
                favoriteBuilding: home, neededEquipment: "Проектор");
            AddCurriculum(PickTeacher(course % 2 == 0 ? MathDept : HumDept), lectureGeneral, LessonType.Lecture, stream, 1,
                favoriteBuilding: home, neededEquipment: "Проектор");
        }

        // --- Семинары/лабораторные у каждой основной группы ---
        // Профиль (2+2 пары) несут профильные кафедры (их 5, есть запас по слотам),
        // математика и язык — по 1 паре, чтобы не перегружать общие кафедры.
        foreach (var group in mains)
        {
            string program = ProgramOf(group.Name);
            int course = CourseOf(group.Name);
            string home = ProgramHomeBuilding.GetValueOrDefault(program);
            var core = ProgramSubjects[program];
            int seed = group.Name.Sum(c => c);

            // Профильный семинар A — часто в компьютерном классе (лабораторная).
            var subjA = core[seed % core.Length];
            bool pcA = ComputerSubjects.Contains(subjA);
            AddCurriculum(PickTeacher(DeptOfProgram(program)), subjA,
                pcA ? LessonType.Laboratory : LessonType.Seminar, MakeStream(new[] { group }), 2,
                favoriteBuilding: home, neededEquipment: pcA ? "Компьютерный класс" : null);

            // Профильный семинар B.
            var subjB = core[(seed + 1) % core.Length];
            AddCurriculum(PickTeacher(DeptOfProgram(program)), subjB, LessonType.Seminar,
                MakeStream(new[] { group }), 2, favoriteBuilding: home);

            // Математика (1 пара) и английский (1 пара).
            var math = MathSubjects[seed % MathSubjects.Length];
            AddCurriculum(PickTeacher(MathDept), math, LessonType.Seminar, MakeStream(new[] { group }), 1);
            AddCurriculum(PickTeacher(LangDept), "Английский язык", LessonType.Seminar, MakeStream(new[] { group }), 1);

            // Старшие курсы дневной смены — дополнительный профильный семинар (итого 10 пар/нед).
            if (course >= 3 && group.Shift == Shift.First)
            {
                var subjC = core[(seed + 2) % core.Length];
                AddCurriculum(PickTeacher(DeptOfProgram(program)), subjC, LessonType.Seminar,
                    MakeStream(new[] { group }), 2, favoriteBuilding: home);
            }
        }

        // --- Лабораторные у подгрупп: двойные, параллельные (граничный момент) ---
        foreach (var sub in _ds.Groups.Where(g => g.ParentGroup is not null))
        {
            string program = ProgramOf(sub.Name);
            AddCurriculum(PickTeacher(DeptOfProgram(program)), "Электроника", LessonType.Laboratory,
                MakeStream(new[] { sub }), 2, doubleLesson: true, parallelism: true,
                favoriteBuilding: ProgramHomeBuilding.GetValueOrDefault(program), neededEquipment: "Компьютерный класс");
        }
    }

    // ----------------------------------------------------------------- разбор имени группы
    private static string ProgramOf(string groupName) => groupName.Split('-')[0];

    private static int CourseOf(string groupName)
    {
        // Формат "ПИ-205" или "ПИ-205/1": курс — первая цифра после дефиса.
        var tail = groupName.Split('-')[1];
        return tail[0] - '0';
    }

    private static string DeptOfProgram(string program) =>
        DataFileGenerator.AllPrograms.First(p => p.Code == program).Department;
}
