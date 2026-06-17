namespace Schedule.DevSolver.Tests.Data;

/// <summary>DTO преподавателя из входного файла teachers.json.</summary>
public sealed record TeacherDto(string Name, string Department);

/// <summary>DTO аудитории из входного файла classrooms.json.</summary>
public sealed record ClassroomDto(string Name, string Building, int Capacity, string[] Equipment);

/// <summary>DTO учебной группы из входного файла groups.json.</summary>
public sealed record GroupDto(
    string Name,
    string Program,
    int Course,
    string Shift,          // First | Second | Evening
    int Students,
    string? Parent);       // имя родительской группы для подгрупп, иначе null
