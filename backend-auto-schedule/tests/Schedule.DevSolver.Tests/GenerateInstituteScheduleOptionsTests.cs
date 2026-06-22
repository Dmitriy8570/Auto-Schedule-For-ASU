using Application.Common.Interfaces;
using Application.Common.Lessons.Commands;
using Application.Solver.Mapping;
using Application.Solver.Model;
using Application.Solver.Solving;
using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.groups;
using Domain.university.teachers;
using Domain.workload;
using Schedule.DevSolver.Tests.Reflection;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Параметры солвера в генерации по институту берутся из конфигурации (SolverOptions),
/// но переопределяются на конкретный запрос. Солвер подменён фейком, фиксирующим переданные
/// параметры; данные — один компонент → фейк-солвер возвращает Infeasible, маппинг не вызывается
/// и БД не трогается.
/// </summary>
public sealed class GenerateInstituteScheduleOptionsTests
{
    private static readonly SolverOptions ConfigDefaults =
        new() { MaxTimeInSeconds = 180, SearchWorkers = 8, LogSearchProgress = false };

    [Fact]
    public async Task UsesConfiguredOptions_WhenNoOverride()
    {
        var solver = new CapturingSolver();
        var handler = NewHandler(solver);

        await handler.Handle(
            new GenerateInstituteScheduleCommand { SemesterId = Guid.NewGuid(), InstituteId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.NotNull(solver.Captured);
        Assert.Equal(180, solver.Captured!.MaxTimeInSeconds);
        Assert.Equal(8, solver.Captured.SearchWorkers);
    }

    [Fact]
    public async Task OverridesMaxTime_PerRequest_KeepingOtherDefaults()
    {
        var solver = new CapturingSolver();
        var handler = NewHandler(solver);

        await handler.Handle(
            new GenerateInstituteScheduleCommand
            {
                SemesterId = Guid.NewGuid(),
                InstituteId = Guid.NewGuid(),
                MaxTimeInSeconds = 30
            },
            CancellationToken.None);

        Assert.Equal(30, solver.Captured!.MaxTimeInSeconds);
        Assert.Equal(8, solver.Captured.SearchWorkers); // не задан -> остаётся из конфигурации
    }

    [Fact]
    public async Task OverridesSearchWorkers_PerRequest()
    {
        var solver = new CapturingSolver();
        var handler = NewHandler(solver);

        await handler.Handle(
            new GenerateInstituteScheduleCommand
            {
                SemesterId = Guid.NewGuid(),
                InstituteId = Guid.NewGuid(),
                SearchWorkers = 2
            },
            CancellationToken.None);

        Assert.Equal(2, solver.Captured!.SearchWorkers);
        Assert.Equal(180, solver.Captured.MaxTimeInSeconds);
    }

    private static GenerateInstituteScheduleCommandHandler NewHandler(IScheduleSolver solver) =>
        new(new MinimalDataProvider(), solver, new ThrowingMapper(), new NoopLessonRepository(),
            new FakeTransactionRunner(), ConfigDefaults);

    // --- Фейки ---

    private sealed class CapturingSolver : IScheduleSolver
    {
        public SolverOptions? Captured { get; private set; }

        public ScheduleSolution Solve(ScheduleModel model, SolverOptions? options = null)
        {
            // Фиксируем параметры ПЕРВОГО (основного) прогона. После Infeasible хендлер делает второй,
            // аварийный (best-effort) прогон с отдельным, заведомо урезанным лимитом времени — эти
            // тесты проверяют плумбинг конфигурации именно основного прогона, поэтому его и ловим.
            Captured ??= options;
            // Infeasible -> основной прогон не приводит к маппингу/сохранению.
            return new ScheduleSolution(ScheduleSolveStatus.Infeasible, Array.Empty<Assignment>(), 0, 0);
        }
    }

    private sealed class MinimalDataProvider : IScheduleDataProvider
    {
        // Один компонент (одна нагрузка с группой) — без аудиторий и слотов, поэтому модель строится
        // тривиально (переменных нет), но декомпозиция находит ровно один компонент и вызывает солвер.
        private static ScheduleData OneComponent()
        {
            var group = DomainFactory.New<Group>().Set(nameof(Group.Id), Guid.NewGuid());
            var streamGroup = DomainFactory.New<StreamGroups>()
                .Set(nameof(StreamGroups.GroupId), group.Id)
                .Set(nameof(StreamGroups.Group), group);
            var stream = DomainFactory.New<AcademicStream>()
                .Set(nameof(AcademicStream.Id), Guid.NewGuid())
                .Set(nameof(AcademicStream.StudentsCount), 1)
                .Set(nameof(AcademicStream.StreamGroups), new List<StreamGroups> { streamGroup });
            var teacher = DomainFactory.New<Teacher>().Set(nameof(Teacher.Id), Guid.NewGuid());
            var curriculum = DomainFactory.New<Curriculum>()
                .Set(nameof(Curriculum.Teacher), teacher)
                .Set(nameof(Curriculum.TeacherId), teacher.Id)
                .Set(nameof(Curriculum.Stream), stream)
                .Set(nameof(Curriculum.StreamId), stream.Id);
            var workload = DomainFactory.New<SemesterWorkload>()
                .Set(nameof(SemesterWorkload.Id), Guid.NewGuid())
                .Set(nameof(SemesterWorkload.Hours), 0)
                .Set(nameof(SemesterWorkload.Curriculum), curriculum);

            return new ScheduleData(
                new[] { workload }, Array.Empty<Classroom>(),
                Array.Empty<TimeSlot>(), Array.Empty<ConstraintConfig>());
        }

        public Task<ScheduleData> GetAsync(Guid semesterId, CancellationToken ct) => Task.FromResult(OneComponent());
        public Task<ScheduleData> GetForInstituteAsync(Guid semesterId, Guid instituteId, CancellationToken ct)
            => Task.FromResult(OneComponent());
    }

    private sealed class ThrowingMapper : IScheduleResultMapper
    {
        public IReadOnlyList<Lesson> ToLessons(ScheduleData data, IReadOnlyList<Assignment> assignments)
            => throw new InvalidOperationException("Маппер не должен вызываться при Infeasible.");
    }

    private sealed class NoopLessonRepository : ILessonRepository
    {
        public Task<IReadOnlyList<Lesson>> GetByInstituteAndSemesterAsync(Guid instituteId, Guid semesterId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Lesson>>(Array.Empty<Lesson>());
        public void RemoveRange(IEnumerable<Lesson> lessons) { }
        public Task AddAsync(Lesson lesson, CancellationToken ct) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

        // Не используются этими тестами.
        public Task<Lesson?> GetLessonByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Lesson?> GetTrackedByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Application.Common.Exceptions.ScheduleConflict>> FindConflictsAsync(Guid classroomId, Guid timeSlotId, Guid streamId, Guid? curriculumId, Guid? excludeLessonId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetByInstituteAsync(Guid instituteId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetBySemesterAsync(Guid semesterId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetLessonByTeacherAsync(Guid teacherId, Guid? weekId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetLessonByRoomAsync(Guid classroomId, Guid? weekId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetLessonByGroupAsync(Guid groupId, Guid? weekId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetBuildingTravelWarningsAsync(Guid lessonId, CancellationToken ct) => throw new NotSupportedException();
    }
}
