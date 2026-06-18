using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Infrastructure.Mmis;

/// <summary>Считывает полный снимок справочников и нагрузки из MS SQL БД MMIS через Dapper.</summary>
public sealed class MmisReader(IOptions<MmisSyncOptions> options)
{
    private readonly MmisSyncOptions _options = options.Value;

    public async Task<MmisSnapshot> ReadAsync(CancellationToken ct)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(ct);

        async Task<IReadOnlyList<T>> Q<T>(string sql) =>
            (await connection.QueryAsync<T>(new CommandDefinition(sql, cancellationToken: ct))).AsList();

        return new MmisSnapshot
        {
            Institutes = await Q<MmisInstitute>("SELECT Id, Name FROM mmis.Institutes"),
            Departments = await Q<MmisDepartment>("SELECT Id, Name, InstituteId FROM mmis.Departments"),
            Teachers = await Q<MmisTeacher>("SELECT Id, FullName, DepartmentId FROM mmis.Teachers"),
            Degrees = await Q<MmisDegree>("SELECT Id, InstituteId, Type FROM mmis.Degrees"),
            Courses = await Q<MmisCourse>("SELECT Id, DegreeId, Number FROM mmis.Courses"),
            Groups = await Q<MmisGroup>("SELECT Id, Name, CourseId, Shift, StudentCount FROM mmis.Groups"),
            Subjects = await Q<MmisSubject>("SELECT Id, Name FROM mmis.Subjects"),
            Semesters = await Q<MmisSemester>("SELECT Id, StartDate, EndDate FROM mmis.Semesters"),
            Weeks = await Q<MmisWeek>("SELECT Id, SemesterId, StartDate, EndDate, WeekType FROM mmis.Weeks"),
            Curriculums = await Q<MmisCurriculum>(
                "SELECT Id, TeacherId, GroupId, SubjectId, LessonType, IsParallel, IsDouble FROM mmis.Curriculums"),
            SemesterWorkloads = await Q<MmisSemesterWorkload>(
                "SELECT Id, CurriculumId, SemesterId, Hours FROM mmis.SemesterWorkloads"),
            WeekWorkloads = await Q<MmisWeekWorkload>(
                "SELECT Id, CurriculumId, WeekId, SemesterWorkloadId, Hours FROM mmis.WeekWorkloads"),
        };
    }
}
