namespace Infrastructure.Mmis;

/// <summary>Сырые строки таблиц MMIS (как они лежат в MS SQL). Все идентификаторы — INT.</summary>
public sealed record MmisInstitute(int Id, string Name);
public sealed record MmisDepartment(int Id, string Name, int InstituteId);
public sealed record MmisTeacher(int Id, string FullName, int DepartmentId);
public sealed record MmisDegree(int Id, int InstituteId, string Type);
public sealed record MmisCourse(int Id, int DegreeId, int Number);
public sealed record MmisGroup(int Id, string Name, int CourseId, byte Shift, int StudentCount);
public sealed record MmisSubject(int Id, string Name);
public sealed record MmisSemester(int Id, DateTime StartDate, DateTime EndDate);
public sealed record MmisWeek(int Id, int SemesterId, DateTime StartDate, DateTime EndDate, byte WeekType);
public sealed record MmisCurriculum(
    int Id, int TeacherId, int GroupId, int SubjectId, byte LessonType, bool IsParallel, bool IsDouble);
public sealed record MmisSemesterWorkload(int Id, int CurriculumId, int SemesterId, int Hours);
public sealed record MmisWeekWorkload(int Id, int CurriculumId, int WeekId, int SemesterWorkloadId, int Hours);

/// <summary>Полный снимок справочников и нагрузки MMIS, считанный за один проход.</summary>
public sealed class MmisSnapshot
{
    public required IReadOnlyList<MmisInstitute> Institutes { get; init; }
    public required IReadOnlyList<MmisDepartment> Departments { get; init; }
    public required IReadOnlyList<MmisTeacher> Teachers { get; init; }
    public required IReadOnlyList<MmisDegree> Degrees { get; init; }
    public required IReadOnlyList<MmisCourse> Courses { get; init; }
    public required IReadOnlyList<MmisGroup> Groups { get; init; }
    public required IReadOnlyList<MmisSubject> Subjects { get; init; }
    public required IReadOnlyList<MmisSemester> Semesters { get; init; }
    public required IReadOnlyList<MmisWeek> Weeks { get; init; }
    public required IReadOnlyList<MmisCurriculum> Curriculums { get; init; }
    public required IReadOnlyList<MmisSemesterWorkload> SemesterWorkloads { get; init; }
    public required IReadOnlyList<MmisWeekWorkload> WeekWorkloads { get; init; }
}
