using Application.Common.Interfaces;
using Application.Common.Lessons.Commands;
using Domain.schedule;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Быстрые модульные тесты команд выгрузки/сброса расписания института (трек C).
/// Репозиторий заменён in-memory подделкой — БД и EF не нужны, проверяется только логика хендлеров.
/// </summary>
public class PublishDiscardScheduleTests
{
    private static readonly Guid Institute = Guid.NewGuid();
    private static readonly Guid Semester = Guid.NewGuid();

    private static Lesson Draft() =>
        Lesson.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Semester);

    private static Lesson Current()
    {
        var l = Draft();
        l.Publish();
        return l;
    }

    // --- Сброс черновика (discard) ---

    [Fact]
    public async Task Discard_RemovesOnlyDrafts_AndKeepsCurrent()
    {
        var current = Current();
        var repo = new FakeLessonRepository(Draft(), Draft(), current);
        var handler = new DiscardInstituteScheduleCommandHandler(repo, new FakeTransactionRunner());

        var result = await handler.Handle(
            new DiscardInstituteScheduleCommand { InstituteId = Institute }, CancellationToken.None);

        Assert.Equal(2, result.Discarded);
        Assert.Equal(2, repo.Removed.Count);
        Assert.All(repo.Removed, l => Assert.Equal(ScheduleVersion.Draft, l.Version));
        Assert.DoesNotContain(current, repo.Removed); // опубликованное не тронуто
        Assert.True(repo.Saved);
    }

    [Fact]
    public async Task Discard_WithoutDrafts_IsIdempotent_NoChanges()
    {
        var repo = new FakeLessonRepository(Current(), Current());
        var handler = new DiscardInstituteScheduleCommandHandler(repo, new FakeTransactionRunner());

        var result = await handler.Handle(
            new DiscardInstituteScheduleCommand { InstituteId = Institute }, CancellationToken.None);

        Assert.Equal(0, result.Discarded);
        Assert.Empty(repo.Removed);
        Assert.False(repo.Saved); // ранний выход — БД не трогаем
    }

    // --- Выгрузка (publish) ---

    [Fact]
    public async Task Publish_PromotesDraftsToCurrent_AndReplacesOldCurrent()
    {
        var oldCurrent = Current();
        var repo = new FakeLessonRepository(Draft(), Draft(), oldCurrent);
        var handler = new PublishInstituteScheduleCommandHandler(repo, new FakeTransactionRunner(), new FakeRealtimeNotifier());

        var result = await handler.Handle(
            new PublishInstituteScheduleCommand { InstituteId = Institute }, CancellationToken.None);

        Assert.Equal(2, result.Published);
        Assert.Contains(oldCurrent, repo.Removed);          // прежнее опубликованное удалено
        Assert.Single(repo.Removed);                         // удалён ровно один (старый Current)
        Assert.Equal(2, repo.AllDrafts.Count(l => l.Version == ScheduleVersion.Current)); // черновики стали Current
        Assert.True(repo.Saved);
    }

    [Fact]
    public async Task Publish_WithoutDrafts_Throws()
    {
        var repo = new FakeLessonRepository(Current());
        var handler = new PublishInstituteScheduleCommandHandler(repo, new FakeTransactionRunner(), new FakeRealtimeNotifier());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(
            new PublishInstituteScheduleCommand { InstituteId = Institute }, CancellationToken.None));
        Assert.False(repo.Saved);
    }

    /// <summary>In-memory подделка: фиксирует удалённые занятия и факт сохранения.</summary>
    private sealed class FakeLessonRepository : ILessonRepository
    {
        private readonly List<Lesson> _lessons;
        public List<Lesson> Removed { get; } = new();
        public bool Saved { get; private set; }
        public IReadOnlyList<Lesson> AllDrafts => _lessons;

        public FakeLessonRepository(params Lesson[] lessons) => _lessons = lessons.ToList();

        public Task<IReadOnlyList<Lesson>> GetByInstituteAsync(Guid instituteId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Lesson>>(_lessons);

        public void RemoveRange(IEnumerable<Lesson> lessons)
        {
            // EF материализует немедленно — повторяем то же поведение, чтобы не зависеть от
            // последующих мутаций Version у черновиков (важно для семантики публикации).
            foreach (var l in lessons.ToList()) { Removed.Add(l); _lessons.Remove(l); }
        }

        public Task SaveChangesAsync(CancellationToken ct) { Saved = true; return Task.CompletedTask; }

        // Не используется этими тестами.
        public Task<Lesson?> GetLessonByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Lesson?> GetTrackedByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task AddAsync(Lesson lesson, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Application.Common.Exceptions.ScheduleConflict>> FindConflictsAsync(Guid classroomId, Guid timeSlotId, Guid streamId, Guid? curriculumId, Guid? excludeLessonId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetByInstituteAndSemesterAsync(Guid instituteId, Guid semesterId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetBySemesterAsync(Guid semesterId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetLessonByTeacherAsync(Guid teacherId, Guid? weekId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetLessonByRoomAsync(Guid classroomId, Guid? weekId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetLessonByGroupAsync(Guid groupId, Guid? weekId, CancellationToken ct) => throw new NotSupportedException();
    }
}
