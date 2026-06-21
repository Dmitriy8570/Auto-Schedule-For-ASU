using Application.Common.Interfaces;
using Application.Common.Lessons.Commands;
using Application.Solver.Mapping;
using Application.Solver.Model;
using Application.Solver.Solving;
using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.workload;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Параметры солвера в генерации по институту берутся из конфигурации (SolverOptions),
/// но переопределяются на конкретный запрос. Солвер подменён фейком, фиксирующим переданные
/// параметры; данные пустые → генерация сразу возвращает Infeasible и БД не трогается.
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
        new(new EmptyDataProvider(), solver, new ThrowingMapper(), new NoopLessonRepository(), ConfigDefaults);

    // --- Фейки ---

    private sealed class CapturingSolver : IScheduleSolver
    {
        public SolverOptions? Captured { get; private set; }

        public ScheduleSolution Solve(ScheduleModel model, SolverOptions? options = null)
        {
            Captured = options;
            // Infeasible -> хендлер выходит до маппинга/сохранения.
            return new ScheduleSolution(ScheduleSolveStatus.Infeasible, Array.Empty<Assignment>(), 0, 0);
        }
    }

    private sealed class EmptyDataProvider : IScheduleDataProvider
    {
        private static ScheduleData Empty => new(
            Array.Empty<SemesterWorkload>(), Array.Empty<Classroom>(),
            Array.Empty<TimeSlot>(), Array.Empty<ConstraintConfig>());

        public Task<ScheduleData> GetAsync(Guid semesterId, CancellationToken ct) => Task.FromResult(Empty);
        public Task<ScheduleData> GetForInstituteAsync(Guid semesterId, Guid instituteId, CancellationToken ct)
            => Task.FromResult(Empty);
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
        public Task<IReadOnlyList<Application.Common.Exceptions.ScheduleConflict>> FindConflictsAsync(Guid classroomId, Guid timeSlotId, Guid streamId, Guid? curriculumId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetByInstituteAsync(Guid instituteId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetBySemesterAsync(Guid semesterId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetLessonByTeacherAsync(Guid teacherId, Guid? weekId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetLessonByRoomAsync(Guid classroomId, Guid? weekId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetLessonByGroupAsync(Guid groupId, Guid? weekId, CancellationToken ct) => throw new NotSupportedException();
    }
}
