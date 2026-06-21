using System.Diagnostics;
using Application.Common.Interfaces;
using Domain.calendar;
using Domain.schedule;
using Domain.university.common;
using Domain.university.groups;
using Domain.university.teachers;
using Domain.workload;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Mmis;

/// <summary>
/// Переносит снимок MMIS в БД Auto-Schedule. Справочники — идемпотентный upsert
/// (insert-or-update по детерминированному Guid). Нагрузка — полная синхронизация с
/// журналированием: новые строки → Add, изменённые часы → Update, пропавшие в источнике →
/// Delete (запись журнала остаётся, сама строка удаляется).
/// </summary>
public sealed class MmisSyncService(
    ApplicationDbContext db,
    MmisReader reader,
    MmisSyncStatus status,
    IInfrastructureSeeder seeder,
    ILogger<MmisSyncService> logger) : IMmisSyncService
{
    private static Guid Id(string type, int mmisId) => DeterministicGuid.For(type, mmisId);

    public async Task<MmisSyncResult> SyncAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var snapshot = await reader.ReadAsync(cancellationToken);
            var now = DateTime.UtcNow;

            int refUpserts = 0, added = 0, updated = 0, deleted = 0;

            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                // Чистый старт на каждую попытку (execution strategy может повторить делегат).
                db.ChangeTracker.Clear();
                refUpserts = added = updated = deleted = 0;

                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                var (upserts, curriculumsToDelete) = await UpsertReferenceAsync(snapshot, cancellationToken);
                refUpserts = upserts;

                var diff = await DiffWorkloadsAsync(snapshot, now, cancellationToken);
                added = diff.Added;
                updated = diff.Updated;
                deleted = diff.Deleted;

                // Фаза 1: добавления/изменения + журнальные записи Delete (нагрузка ещё на месте).
                await db.SaveChangesAsync(cancellationToken);

                // Фаза 2: физическое удаление. SetNull обнулит FK у Delete-логов, и они переживут удаление.
                if (diff.WeekToDelete.Count > 0) db.WeekWorkloads.RemoveRange(diff.WeekToDelete);
                if (diff.SemesterToDelete.Count > 0) db.SemesterWorkloads.RemoveRange(diff.SemesterToDelete);
                if (curriculumsToDelete.Count > 0) db.Curriculums.RemoveRange(curriculumsToDelete);

                if (diff.WeekToDelete.Count + diff.SemesterToDelete.Count + curriculumsToDelete.Count > 0)
                    await db.SaveChangesAsync(cancellationToken);

                await tx.CommitAsync(cancellationToken);
            });

            // Недели семестра уже в БД — достраиваем рабочие дни и пары (ось «время» солвера).
            // Идемпотентно: новые недели получают сетку, существующие пропускаются.
            await seeder.SeedCalendarGridAsync(cancellationToken);

            var result = new MmisSyncResult(added, updated, deleted, refUpserts, sw.Elapsed);
            status.RecordSuccess(result);

            if (result.HasWorkloadChanges)
                logger.LogInformation(
                    "MMIS-синхронизация завершена за {Duration}: нагрузка +{Added}/~{Updated}/-{Deleted}, справочников {Refs}.",
                    sw.Elapsed, added, updated, deleted, refUpserts);
            else
                logger.LogInformation(
                    "MMIS-синхронизация завершена за {Duration}: изменений нагрузки нет (справочников {Refs}).",
                    sw.Elapsed, refUpserts);

            return result;
        }
        catch (Exception ex)
        {
            status.RecordFailure(ex);
            logger.LogError(ex, "MMIS-синхронизация завершилась с ошибкой.");
            throw;
        }
    }

    /// <summary>Идемпотентный upsert справочников в порядке внешних ключей. Возвращает кол-во вставок и пропавшие планы на удаление.</summary>
    private async Task<(int Upserts, List<Curriculum> CurriculumsToDelete)> UpsertReferenceAsync(
        MmisSnapshot s, CancellationToken ct)
    {
        var upserts = 0;
        void Set(object entity, string prop, object? value) => db.Entry(entity).Property(prop).CurrentValue = value;

        // Институты.
        var institutes = await db.Institutes.ToDictionaryAsync(x => x.Id, ct);
        foreach (var m in s.Institutes)
        {
            var id = Id("Institute", m.Id);
            if (institutes.TryGetValue(id, out var e)) Set(e, nameof(Institute.Name), m.Name);
            else { db.Institutes.Add(Institute.Create(id, m.Name)); upserts++; }
        }

        // Кафедры.
        var departments = await db.Departments.ToDictionaryAsync(x => x.Id, ct);
        foreach (var m in s.Departments)
        {
            var id = Id("Department", m.Id);
            var instituteId = Id("Institute", m.InstituteId);
            if (departments.TryGetValue(id, out var e))
            {
                Set(e, nameof(Department.Name), m.Name);
                Set(e, nameof(Department.InstituteId), instituteId);
            }
            else { db.Departments.Add(Department.Create(id, m.Name, instituteId)); upserts++; }
        }

        // Преподаватели.
        var teachers = await db.Teachers.ToDictionaryAsync(x => x.Id, ct);
        foreach (var m in s.Teachers)
        {
            var id = Id("Teacher", m.Id);
            var departmentId = Id("Department", m.DepartmentId);
            if (teachers.TryGetValue(id, out var e))
            {
                Set(e, nameof(Teacher.Name), m.FullName);
                Set(e, nameof(Teacher.DepartmentId), departmentId);
            }
            else { db.Teachers.Add(Teacher.Create(id, m.FullName, departmentId)); upserts++; }
        }

        // Ступени.
        var degrees = await db.Degrees.ToDictionaryAsync(x => x.Id, ct);
        foreach (var m in s.Degrees)
        {
            var id = Id("Degree", m.Id);
            var instituteId = Id("Institute", m.InstituteId);
            var type = MapDegree(m.Type);
            if (degrees.TryGetValue(id, out var e))
            {
                Set(e, nameof(Degree.TypeDegree), type);
                Set(e, nameof(Degree.InstituteId), instituteId);
            }
            else { db.Degrees.Add(Degree.Create(id, type, instituteId)); upserts++; }
        }

        // Курсы.
        var courses = await db.Courses.ToDictionaryAsync(x => x.Id, ct);
        foreach (var m in s.Courses)
        {
            var id = Id("Course", m.Id);
            var degreeId = Id("Degree", m.DegreeId);
            if (courses.TryGetValue(id, out var e))
            {
                Set(e, nameof(Course.Number), m.Number);
                Set(e, nameof(Course.DegreeId), degreeId);
            }
            else { db.Courses.Add(Course.Create(id, m.Number, degreeId)); upserts++; }
        }

        // Группы.
        var groups = await db.Groups.ToDictionaryAsync(x => x.Id, ct);
        foreach (var m in s.Groups)
        {
            var id = Id("Group", m.Id);
            var courseId = Id("Course", m.CourseId);
            var shift = (Shift)m.Shift;
            if (groups.TryGetValue(id, out var e))
            {
                Set(e, nameof(Group.Name), m.Name);
                Set(e, nameof(Group.Shift), shift);
                Set(e, nameof(Group.StudentCount), m.StudentCount);
                Set(e, nameof(Group.CourseId), courseId);
            }
            else { db.Groups.Add(Group.Create(id, m.Name, shift, m.StudentCount, courseId)); upserts++; }
        }

        // Потоки: 1 группа → 1 поток (+ связка StreamGroups).
        var streams = await db.AcademicStreams.ToDictionaryAsync(x => x.Id, ct);
        var links = (await db.StreamGroups.ToListAsync(ct))
            .Select(l => (l.StreamId, l.GroupId)).ToHashSet();
        foreach (var m in s.Groups)
        {
            var streamId = Id("Stream", m.Id);
            var groupId = Id("Group", m.Id);
            if (streams.TryGetValue(streamId, out var e)) Set(e, nameof(AcademicStream.StudentsCount), m.StudentCount);
            else { db.AcademicStreams.Add(AcademicStream.Create(streamId, m.StudentCount)); upserts++; }

            if (links.Add((streamId, groupId)))
                db.StreamGroups.Add(StreamGroups.Create(groupId, streamId));
        }

        // Дисциплины.
        var subjects = await db.Subjects.ToDictionaryAsync(x => x.Id, ct);
        foreach (var m in s.Subjects)
        {
            var id = Id("Subject", m.Id);
            if (subjects.TryGetValue(id, out var e)) Set(e, nameof(Subject.Name), m.Name);
            else { db.Subjects.Add(Subject.Create(id, m.Name)); upserts++; }
        }

        // Семестры.
        var semesters = await db.Semesters.ToDictionaryAsync(x => x.Id, ct);
        foreach (var m in s.Semesters)
        {
            var id = Id("Semester", m.Id);
            var start = DateOnly.FromDateTime(m.StartDate);
            var end = DateOnly.FromDateTime(m.EndDate);
            if (semesters.TryGetValue(id, out var e))
            {
                Set(e, nameof(Semester.StartDate), start);
                Set(e, nameof(Semester.EndDate), end);
            }
            else { db.Semesters.Add(Semester.Create(id, start, end)); upserts++; }
        }

        // Недели.
        var weeks = await db.Weeks.ToDictionaryAsync(x => x.Id, ct);
        foreach (var m in s.Weeks)
        {
            var id = Id("Week", m.Id);
            var semesterId = Id("Semester", m.SemesterId);
            var start = DateOnly.FromDateTime(m.StartDate);
            var end = DateOnly.FromDateTime(m.EndDate);
            var type = (WeekType)m.WeekType;
            if (weeks.TryGetValue(id, out var e))
            {
                Set(e, nameof(Week.StartDate), start);
                Set(e, nameof(Week.EndDate), end);
                Set(e, nameof(Week.WeekType), type);
                Set(e, nameof(Week.SemesterId), semesterId);
            }
            else { db.Weeks.Add(Week.Create(id, start, end, type, semesterId)); upserts++; }
        }

        // Учебные планы (Curriculum → AcademicStream группы).
        var curriculums = await db.Curriculums.ToDictionaryAsync(x => x.Id, ct);
        var seenCurriculums = new HashSet<Guid>();
        foreach (var m in s.Curriculums)
        {
            var id = Id("Curriculum", m.Id);
            seenCurriculums.Add(id);
            var teacherId = Id("Teacher", m.TeacherId);
            var streamId = Id("Stream", m.GroupId);
            var subjectId = Id("Subject", m.SubjectId);
            var lessonType = (LessonType)m.LessonType;
            if (curriculums.TryGetValue(id, out var e))
            {
                Set(e, nameof(Curriculum.TeacherId), teacherId);
                Set(e, nameof(Curriculum.StreamId), streamId);
                Set(e, nameof(Curriculum.SubjectId), subjectId);
                Set(e, nameof(Curriculum.LessonType), lessonType);
                Set(e, nameof(Curriculum.Parallelism), m.IsParallel);
                Set(e, nameof(Curriculum.Double), m.IsDouble);
            }
            else
            {
                db.Curriculums.Add(Curriculum.Create(id, teacherId, streamId, subjectId, lessonType, m.IsParallel, m.IsDouble));
                upserts++;
            }
        }

        // Пропавшие в MMIS планы — на удаление (их нагрузка снимется отдельно в diff'е).
        var curriculumsToDelete = curriculums.Where(kv => !seenCurriculums.Contains(kv.Key))
                                             .Select(kv => kv.Value).ToList();

        return (upserts, curriculumsToDelete);
    }

    /// <summary>Диф нагрузки с журналированием. Сами удаления (RemoveRange) выполняет вызывающий код во 2-й фазе.</summary>
    private async Task<WorkloadDiff> DiffWorkloadsAsync(MmisSnapshot s, DateTime now, CancellationToken ct)
    {
        var diff = new WorkloadDiff();

        // Семестровая нагрузка.
        var existingSem = await db.SemesterWorkloads.ToDictionaryAsync(x => x.Id, ct);
        var seenSem = new HashSet<Guid>();
        foreach (var m in s.SemesterWorkloads)
        {
            var id = Id("SemWl", m.Id);
            seenSem.Add(id);
            if (existingSem.TryGetValue(id, out var e))
            {
                if (e.ChangeHours(m.Hours, now)) diff.Updated++;
            }
            else
            {
                var wl = SemesterWorkload.Create(id, m.Hours, Id("Curriculum", m.CurriculumId), Id("Semester", m.SemesterId));
                wl.RecordAdded(now);
                db.SemesterWorkloads.Add(wl);
                diff.Added++;
            }
        }
        foreach (var (id, e) in existingSem)
        {
            if (seenSem.Contains(id)) continue;
            e.RecordDeleted(now);
            diff.SemesterToDelete.Add(e);
            diff.Deleted++;
        }

        // Понедельная нагрузка.
        var existingWeek = await db.WeekWorkloads.ToDictionaryAsync(x => x.Id, ct);
        var seenWeek = new HashSet<Guid>();
        foreach (var m in s.WeekWorkloads)
        {
            var id = Id("WeekWl", m.Id);
            seenWeek.Add(id);
            if (existingWeek.TryGetValue(id, out var e))
            {
                if (e.ChangeHours(m.Hours, now)) diff.Updated++;
            }
            else
            {
                var wl = WeekWorkload.Create(
                    id, m.Hours, Id("Curriculum", m.CurriculumId), Id("Week", m.WeekId), Id("SemWl", m.SemesterWorkloadId));
                wl.RecordAdded(now);
                db.WeekWorkloads.Add(wl);
                diff.Added++;
            }
        }
        foreach (var (id, e) in existingWeek)
        {
            if (seenWeek.Contains(id)) continue;
            e.RecordDeleted(now);
            diff.WeekToDelete.Add(e);
            diff.Deleted++;
        }

        return diff;
    }

    private sealed class WorkloadDiff
    {
        public int Added;
        public int Updated;
        public int Deleted;
        public List<SemesterWorkload> SemesterToDelete { get; } = new();
        public List<WeekWorkload> WeekToDelete { get; } = new();
    }

    private static TypeDegree MapDegree(string type) => type.Trim().ToLowerInvariant() switch
    {
        "secondary" => TypeDegree.Secondary,
        "bachelor" => TypeDegree.Bachelor,
        "specialist" => TypeDegree.Specialist,
        "master" => TypeDegree.Master,
        "postgraduate" => TypeDegree.Postgraduate,
        "doctoral" => TypeDegree.Doctoral,
        _ => TypeDegree.Bachelor
    };
}
