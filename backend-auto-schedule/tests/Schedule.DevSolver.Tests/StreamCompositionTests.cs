using Infrastructure.Mmis;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Тесты композиции потоков по снимку MMIS (<see cref="StreamComposition"/>): по-групповые лекции
/// одного преподавателя по одной дисциплине у групп одного курса схлопываются в один поток;
/// семинары и одиночные лекции остаются по группам.
/// </summary>
public sealed class StreamCompositionTests
{
    private const byte Lecture = 0;
    private const byte Seminar = 1;

    [Fact]
    public void Build_MergesCourseLecturesIntoSingleStream()
    {
        // Курс с тремя группами: общая лекция (преп. 100, дисц. 10) + по-групповые семинары.
        var groups = new[]
        {
            new MmisGroup(1, "G1", CourseId: 7, Shift: 0, StudentCount: 20),
            new MmisGroup(2, "G2", CourseId: 7, Shift: 0, StudentCount: 25),
            new MmisGroup(3, "G3", CourseId: 7, Shift: 0, StudentCount: 30),
        };
        var curriculums = new[]
        {
            new MmisCurriculum(1001, TeacherId: 100, GroupId: 1, SubjectId: 10, LessonType: Lecture, IsParallel: true, IsDouble: false),
            new MmisCurriculum(1002, TeacherId: 100, GroupId: 2, SubjectId: 10, LessonType: Lecture, IsParallel: true, IsDouble: false),
            new MmisCurriculum(1003, TeacherId: 100, GroupId: 3, SubjectId: 10, LessonType: Lecture, IsParallel: true, IsDouble: false),
            // Семинары — по группам, не объединяются.
            new MmisCurriculum(2001, TeacherId: 101, GroupId: 1, SubjectId: 10, LessonType: Seminar, IsParallel: false, IsDouble: false),
            new MmisCurriculum(2002, TeacherId: 102, GroupId: 2, SubjectId: 10, LessonType: Seminar, IsParallel: false, IsDouble: false),
        };

        var composition = StreamComposition.Build(Snapshot(groups, curriculums));

        var stream = Assert.Single(composition.MergedStreams);
        Assert.Equal(20 + 25 + 30, stream.StudentsCount);
        Assert.Equal(new[] { 1, 2, 3 }, stream.GroupMmisIds.OrderBy(x => x));

        var merged = Assert.Single(composition.MergedCurriculums);
        Assert.Equal(stream.StreamId, merged.StreamId);
        Assert.Equal(100, merged.TeacherMmisId);
        Assert.Equal(10, merged.SubjectMmisId);

        // Все три лекционных плана поглощены; семинары — нет.
        Assert.Equal(new[] { 1001, 1002, 1003 }, composition.AbsorbedCurriculumIds.OrderBy(x => x));
        Assert.DoesNotContain(2001, composition.AbsorbedCurriculumIds);

        // Первичный участник — с наименьшим MMIS-id; на него перенаправляется нагрузка потока.
        Assert.Equal(merged.Id, composition.PrimaryCurriculumToMerged[1001]);
        Assert.False(composition.PrimaryCurriculumToMerged.ContainsKey(1002));
    }

    [Fact]
    public void Build_DoesNotMergeSingleGroupLecture()
    {
        var groups = new[] { new MmisGroup(1, "G1", CourseId: 7, Shift: 0, StudentCount: 20) };
        var curriculums = new[]
        {
            new MmisCurriculum(1001, TeacherId: 100, GroupId: 1, SubjectId: 10, LessonType: Lecture, IsParallel: true, IsDouble: false),
        };

        var composition = StreamComposition.Build(Snapshot(groups, curriculums));

        Assert.Empty(composition.MergedStreams);
        Assert.Empty(composition.MergedCurriculums);
        Assert.Empty(composition.AbsorbedCurriculumIds);
    }

    [Fact]
    public void Build_SeparatesLecturesByTeacherAndSubject()
    {
        // Две группы, та же дисциплина, но РАЗНЫЕ преподаватели → два разных потока (по одной группе) → не объединяются.
        var groups = new[]
        {
            new MmisGroup(1, "G1", CourseId: 7, Shift: 0, StudentCount: 20),
            new MmisGroup(2, "G2", CourseId: 7, Shift: 0, StudentCount: 25),
        };
        var curriculums = new[]
        {
            new MmisCurriculum(1001, TeacherId: 100, GroupId: 1, SubjectId: 10, LessonType: Lecture, IsParallel: true, IsDouble: false),
            new MmisCurriculum(1002, TeacherId: 200, GroupId: 2, SubjectId: 10, LessonType: Lecture, IsParallel: true, IsDouble: false),
        };

        var composition = StreamComposition.Build(Snapshot(groups, curriculums));

        Assert.Empty(composition.MergedStreams);
    }

    private static MmisSnapshot Snapshot(
        IReadOnlyList<MmisGroup> groups, IReadOnlyList<MmisCurriculum> curriculums) => new()
    {
        Institutes = Array.Empty<MmisInstitute>(),
        Departments = Array.Empty<MmisDepartment>(),
        Teachers = Array.Empty<MmisTeacher>(),
        Degrees = Array.Empty<MmisDegree>(),
        Courses = Array.Empty<MmisCourse>(),
        Groups = groups,
        Subjects = Array.Empty<MmisSubject>(),
        Semesters = Array.Empty<MmisSemester>(),
        Weeks = Array.Empty<MmisWeek>(),
        Curriculums = curriculums,
        SemesterWorkloads = Array.Empty<MmisSemesterWorkload>(),
        WeekWorkloads = Array.Empty<MmisWeekWorkload>(),
    };
}
