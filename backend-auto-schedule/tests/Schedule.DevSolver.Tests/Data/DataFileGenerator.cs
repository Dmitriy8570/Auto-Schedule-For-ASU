using System.Text.Json;
using System.Text.Json.Serialization;

namespace Schedule.DevSolver.Tests.Data;

/// <summary>
/// Генерирует входные файлы «небольшого, но реального по масштабу» университета:
/// <c>teachers.json</c>, <c>groups.json</c>, <c>classrooms.json</c>. Цифры детерминированы
/// (фиксированный seed), поэтому повторный запуск даёт те же данные.
///
/// Масштаб: 5 направлений × 4 курса × 5 групп = 100 групп, ~50 преподавателей, ~48 аудиторий.
/// </summary>
public static class DataFileGenerator
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public sealed record Programs(string Code, string Department);

    public static readonly Programs[] AllPrograms =
    {
        new("ПИ", "Кафедра программной инженерии"),
        new("ИВТ", "Кафедра вычислительной техники"),
        new("ИБ", "Кафедра информационной безопасности"),
        new("ПМ", "Кафедра прикладной математики"),
        new("БИ", "Кафедра бизнес-информатики"),
    };

    public const int CoursesPerProgram = 4;
    public const int GroupsPerCourse = 5;

    public static void Generate(string dir)
    {
        Directory.CreateDirectory(dir);
        WriteJson(Path.Combine(dir, "teachers.json"), BuildTeachers());
        WriteJson(Path.Combine(dir, "groups.json"), BuildGroups());
        WriteJson(Path.Combine(dir, "classrooms.json"), BuildClassrooms());
    }

    private static void WriteJson<T>(string path, T value) =>
        File.WriteAllText(path, JsonSerializer.Serialize(value, Json), System.Text.Encoding.UTF8);

    // ----------------------------------------------------------------- преподаватели (~50)
    private static List<TeacherDto> BuildTeachers()
    {
        // Фамилии берём из пула и комбинируем с инициалами — получаем уникальные ФИО.
        var surnames = new[]
        {
            "Иванов", "Петров", "Сидоров", "Кузнецов", "Смирнов", "Попов", "Лебедев", "Козлов",
            "Новиков", "Морозов", "Волков", "Соловьёв", "Васильев", "Зайцев", "Павлов", "Семёнов",
            "Голубев", "Виноградов", "Богданов", "Воробьёв", "Фёдоров", "Михайлов", "Беляев", "Тарасов",
            "Белов", "Комаров", "Орлов", "Киселёв", "Макаров", "Андреев", "Ковалёв", "Ильин",
            "Гусев", "Титов", "Кудрявцев", "Баранов", "Куликов", "Алексеев", "Степанов", "Яковлев",
            "Сорокин", "Сергеев", "Романов", "Захаров", "Борисов", "Королёв", "Герасимов", "Пономарёв",
            "Григорьев", "Лазарев",
        };
        var initials = new[] { "А.А.", "В.С.", "Д.И.", "Е.П.", "И.Н.", "К.М.", "Л.О.", "М.Р.", "Н.С.", "О.В." };

        // 8 кафедр: 5 профильных + 3 общеобразовательных.
        var departments = AllPrograms.Select(p => p.Department)
            .Concat(new[] { "Кафедра высшей математики", "Кафедра иностранных языков", "Кафедра гуманитарных дисциплин" })
            .ToArray();

        // Распределение количества преподавателей по кафедрам (сумма = 50).
        // Математика и языки — общие для всех групп, поэтому укомплектованы плотнее.
        var counts = new Dictionary<string, int>
        {
            ["Кафедра программной инженерии"] = 6,
            ["Кафедра вычислительной техники"] = 6,
            ["Кафедра информационной безопасности"] = 6,
            ["Кафедра прикладной математики"] = 6,
            ["Кафедра бизнес-информатики"] = 6,
            ["Кафедра высшей математики"] = 8,
            ["Кафедра иностранных языков"] = 8,
            ["Кафедра гуманитарных дисциплин"] = 4,
        };

        var result = new List<TeacherDto>();
        int s = 0;
        foreach (var dept in departments)
            for (int i = 0; i < counts[dept]; i++)
            {
                var name = $"{surnames[s % surnames.Length]} {initials[s / surnames.Length % initials.Length]}";
                result.Add(new TeacherDto(name, dept));
                s++;
            }
        return result;
    }

    // ----------------------------------------------------------------- группы (100 + подгруппы)
    private static List<GroupDto> BuildGroups()
    {
        var rnd = new Random(20260617);
        var result = new List<GroupDto>();

        foreach (var program in AllPrograms)
            for (int course = 1; course <= CoursesPerProgram; course++)
                for (int n = 1; n <= GroupsPerCourse; n++)
                {
                    // Смена: большинство — дневная; часть старших курсов — вечерняя/вторая.
                    string shift = "First";
                    if (course >= 3 && n == GroupsPerCourse) shift = "Evening";
                    else if (course >= 2 && n == GroupsPerCourse - 1) shift = "Second";

                    string name = $"{program.Code}-{course}{n:00}";
                    int students = 18 + rnd.Next(0, 12);
                    result.Add(new GroupDto(name, program.Code, course, shift, students, Parent: null));

                    // Граничный момент: каждую 1-ю группу 2-го курса делим на две подгруппы (лабораторные).
                    if (course == 2 && n == 1)
                    {
                        result.Add(new GroupDto($"{name}/1", program.Code, course, shift, students / 2, name));
                        result.Add(new GroupDto($"{name}/2", program.Code, course, shift, students - students / 2, name));
                    }
                }

        return result;
    }

    // ----------------------------------------------------------------- аудитории (~48)
    private static List<ClassroomDto> BuildClassrooms()
    {
        var result = new List<ClassroomDto>();
        var buildings = new[] { "Корпус А", "Корпус Б", "Корпус В" };

        foreach (var (building, idx) in buildings.Select((b, i) => (b, i)))
        {
            char letter = (char)('А' + idx);

            // По 2 поточные лекционные на корпус (вмещают самый большой поток: 5 групп × ≤29 = 145).
            for (int i = 1; i <= 2; i++)
                result.Add(new ClassroomDto($"{letter}-10{i}", building, 150, new[] { "Проектор" }));

            // 8 семинарских.
            for (int i = 1; i <= 8; i++)
                result.Add(new ClassroomDto($"{letter}-2{i:00}", building, 30, Array.Empty<string>()));

            // 5 компьютерных классов (вмещают полную основную группу: ≤29 студентов).
            for (int i = 1; i <= 5; i++)
                result.Add(new ClassroomDto($"{letter}-3{i:00}", building, 30, new[] { "Компьютерный класс", "Проектор" }));

            // 1 лаборатория электроники (только в корпусах Б и В).
            if (idx >= 1)
                result.Add(new ClassroomDto($"{letter}-401", building, 20, new[] { "Лаборатория электроники", "Проектор" }));
        }

        return result;
    }

    public static (List<TeacherDto> Teachers, List<GroupDto> Groups, List<ClassroomDto> Classrooms) Read(string dir)
    {
        var teachers = JsonSerializer.Deserialize<List<TeacherDto>>(File.ReadAllText(Path.Combine(dir, "teachers.json")), Json)!;
        var groups = JsonSerializer.Deserialize<List<GroupDto>>(File.ReadAllText(Path.Combine(dir, "groups.json")), Json)!;
        var rooms = JsonSerializer.Deserialize<List<ClassroomDto>>(File.ReadAllText(Path.Combine(dir, "classrooms.json")), Json)!;
        return (teachers, groups, rooms);
    }
}
