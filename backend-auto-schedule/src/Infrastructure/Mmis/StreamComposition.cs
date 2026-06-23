namespace Infrastructure.Mmis;

/// <summary>
/// Композиция потоков по снимку MMIS. MMIS хранит учебный план <em>по группам</em>: одна и та же
/// лекция (дисциплина + преподаватель) для каждой группы курса лежит отдельной строкой. На занятии
/// же лекция читается всем группам курса вместе — это <b>поток</b>. Здесь по снимку определяется,
/// какие по-групповые лекции схлопнуть в один поток.
///
/// Ключ объединения — <c>(курс, дисциплина, преподаватель)</c> при <c>LessonType = Лекция</c>
/// (курс уже подразумевает институт: Course → Degree → Institute). Семинары и лабораторные остаются
/// по группам (их ведут раздельно, нередко разными преподавателями).
///
/// Для объединяемой лекции выбирается «первичный» участник (с наименьшим MMIS-id) — его нагрузка
/// перенаправляется на план потока, нагрузки прочих участников поглощаются (не создаются). Так
/// поток получает ровно один учебный план и одну нагрузку, и солвер ставит лекцию один раз на все
/// группы потока.
/// </summary>
internal sealed class StreamComposition
{
    /// <summary>MMIS-код типа занятия «лекция» (см. <c>02_seed.sql</c> / <see cref="Domain.workload.LessonType"/>).</summary>
    public const byte LectureType = 0;

    private StreamComposition(
        IReadOnlyList<MergedStream> mergedStreams,
        IReadOnlyList<MergedCurriculum> mergedCurriculums,
        IReadOnlySet<int> absorbedCurriculumIds,
        IReadOnlyDictionary<int, Guid> primaryCurriculumToMerged)
    {
        MergedStreams = mergedStreams;
        MergedCurriculums = mergedCurriculums;
        AbsorbedCurriculumIds = absorbedCurriculumIds;
        PrimaryCurriculumToMerged = primaryCurriculumToMerged;
    }

    /// <summary>Лекционные потоки (по одному на ключ объединения), с составом групп.</summary>
    public IReadOnlyList<MergedStream> MergedStreams { get; }

    /// <summary>Учебные планы потоков — по одному на поток (план первичного участника, перенесённый на поток).</summary>
    public IReadOnlyList<MergedCurriculum> MergedCurriculums { get; }

    /// <summary>MMIS-id лекционных планов, поглощённых потоком: их по-групповой <c>Curriculum:{id}</c> не создаётся.</summary>
    public IReadOnlySet<int> AbsorbedCurriculumIds { get; }

    /// <summary>MMIS-id «первичного» участника потока → Guid плана потока (на него перенаправляется нагрузка).</summary>
    public IReadOnlyDictionary<int, Guid> PrimaryCurriculumToMerged { get; }

    public static StreamComposition Build(MmisSnapshot snapshot)
    {
        var groupCourse = snapshot.Groups.ToDictionary(g => g.Id, g => g.CourseId);
        var groupStudents = snapshot.Groups.ToDictionary(g => g.Id, g => g.StudentCount);

        var mergedStreams = new List<MergedStream>();
        var mergedCurriculums = new List<MergedCurriculum>();
        var absorbed = new HashSet<int>();
        var primaryToMerged = new Dictionary<int, Guid>();

        var lectureKeys = snapshot.Curriculums
            .Where(c => c.LessonType == LectureType && groupCourse.ContainsKey(c.GroupId))
            .GroupBy(c => (Course: groupCourse[c.GroupId], c.SubjectId, c.TeacherId));

        foreach (var key in lectureKeys)
        {
            // Участники по возрастанию MMIS-id; первый — «первичный» (стабильный выбор).
            var members = key.OrderBy(c => c.Id).ToList();
            var memberGroups = members.Select(c => c.GroupId).Distinct().ToList();

            // Поток имеет смысл только при двух и более группах; одиночная лекция остаётся по-групповой.
            if (memberGroups.Count < 2) continue;

            var (course, subject, teacher) = key.Key;
            var streamId = DeterministicGuid.For($"Stream:Lecture:{course}:{subject}", teacher);
            var curriculumId = DeterministicGuid.For($"Curriculum:Lecture:{course}:{subject}", teacher);
            var primary = members[0];

            mergedStreams.Add(new MergedStream(
                streamId, memberGroups.Sum(g => groupStudents[g]), memberGroups));
            mergedCurriculums.Add(new MergedCurriculum(
                curriculumId, streamId, teacher, subject, primary.IsParallel, primary.IsDouble));

            foreach (var m in members) absorbed.Add(m.Id);
            primaryToMerged[primary.Id] = curriculumId;
        }

        return new StreamComposition(mergedStreams, mergedCurriculums, absorbed, primaryToMerged);
    }
}

/// <summary>Лекционный поток: общий поток, его суммарная численность и MMIS-id групп-участников.</summary>
internal sealed record MergedStream(Guid StreamId, int StudentsCount, IReadOnlyList<int> GroupMmisIds);

/// <summary>Учебный план потока (одна потоковая лекция): Guid плана/потока и MMIS-коды преподавателя и дисциплины.</summary>
internal sealed record MergedCurriculum(
    Guid Id, Guid StreamId, int TeacherMmisId, int SubjectMmisId, bool IsParallel, bool IsDouble);
