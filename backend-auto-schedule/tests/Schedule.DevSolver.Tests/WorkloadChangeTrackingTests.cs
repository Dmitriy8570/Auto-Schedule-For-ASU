using Domain.workload;
using Domain.workload.logs;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Проверяет журналирование изменений нагрузки (RecordAdded / ChangeHours / RecordDeleted) —
/// то, что наполняет SemesterLog/WeekLog при синхронизации с MMIS.
/// </summary>
public class WorkloadChangeTrackingTests
{
    private static SemesterWorkload NewSemester(int hours) =>
        SemesterWorkload.Create(Guid.NewGuid(), hours, Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void RecordAdded_WritesAddLog_FromZeroToHours()
    {
        var wl = NewSemester(72);
        var now = DateTime.UtcNow;

        wl.RecordAdded(now);

        var log = Assert.Single(wl.SemesterLogs);
        Assert.Equal(LogAction.Add, log.Action);
        Assert.Equal(0, log.OldValue);
        Assert.Equal(72, log.NewValue);
        Assert.Equal(now, log.TimeStamp);
        Assert.Equal(wl.Id, log.SemesterWorkloadId);
    }

    [Fact]
    public void ChangeHours_WhenDifferent_WritesUpdateLog_AndUpdatesValue()
    {
        var wl = NewSemester(72);

        var changed = wl.ChangeHours(80, DateTime.UtcNow);

        Assert.True(changed);
        Assert.Equal(80, wl.Hours);
        var log = Assert.Single(wl.SemesterLogs);
        Assert.Equal(LogAction.Update, log.Action);
        Assert.Equal(72, log.OldValue);
        Assert.Equal(80, log.NewValue);
    }

    [Fact]
    public void ChangeHours_WhenSame_IsNoOp_AndWritesNoLog()
    {
        var wl = NewSemester(72);

        var changed = wl.ChangeHours(72, DateTime.UtcNow);

        Assert.False(changed);
        Assert.Empty(wl.SemesterLogs);
    }

    [Fact]
    public void RecordDeleted_WritesDeleteLog_FromHoursToZero()
    {
        var wl = NewSemester(72);

        wl.RecordDeleted(DateTime.UtcNow);

        var log = Assert.Single(wl.SemesterLogs);
        Assert.Equal(LogAction.Delete, log.Action);
        Assert.Equal(72, log.OldValue);
        Assert.Equal(0, log.NewValue);
    }

    [Fact]
    public void WeekWorkload_ChangeHours_WritesUpdateLog()
    {
        var wl = WeekWorkload.Create(Guid.NewGuid(), 4, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var changed = wl.ChangeHours(6, DateTime.UtcNow);

        Assert.True(changed);
        Assert.Equal(6, wl.Hours);
        var log = Assert.Single(wl.WeekLogs);
        Assert.Equal(LogAction.Update, log.Action);
        Assert.Equal(4, log.OldValue);
        Assert.Equal(6, log.NewValue);
        Assert.Equal(wl.Id, log.WeekWorkloadId);
    }
}
