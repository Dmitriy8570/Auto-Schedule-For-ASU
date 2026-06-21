using Application.Common.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Schedule.DevSolver.Tests;

/// <summary>
/// Тесты репозитория журнала изменений нагрузки на SQLite (in-memory).
/// Главное — убедиться, что объединение семестрового и понедельного журналов (UNION ALL)
/// с сортировкой и постраничной выборкой ТРАНСЛИРУЕТСЯ в SQL и выполняется на реляционной БД,
/// а схема с новыми индексами IX_*Logs_TimeStamp успешно создаётся. Плюс проверка клампинга
/// номера/размера страницы. Данные не сидируются (граф нагрузки глубокий) — корректность
/// сортировки на реальных данных проверяется отдельно через docker-стек.
/// </summary>
public sealed class WorkloadChangesPaginationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public WorkloadChangesPaginationTests()
    {
        // Соединение держим открытым — для in-memory SQLite это сохраняет схему на время теста.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetChanges_TranslatesUnionAll_AndReturnsEmptyPage()
    {
        var repo = new WorkloadLogRepository(_context);

        // Если UNION ALL/OrderBy/Skip/Take не транслируется — здесь будет исключение.
        var result = await repo.GetChangesAsync(
            new WorkloadChangeFilter(), page: 1, pageSize: 20, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0, result.TotalPages);
    }

    [Theory]
    [InlineData(0, 1)]   // page < 1 -> 1
    [InlineData(-5, 1)]
    [InlineData(3, 3)]   // валидное значение проходит как есть
    public async Task GetChanges_ClampsPage(int requested, int expected)
    {
        var repo = new WorkloadLogRepository(_context);
        var result = await repo.GetChangesAsync(
            new WorkloadChangeFilter(), requested, 20, CancellationToken.None);
        Assert.Equal(expected, result.Page);
    }

    [Theory]
    [InlineData(0, 20)]     // pageSize < 1 -> default 20
    [InlineData(-1, 20)]
    [InlineData(50, 50)]    // в пределах [1..100] проходит как есть
    [InlineData(5000, 100)] // > max -> 100
    public async Task GetChanges_ClampsPageSize(int requested, int expected)
    {
        var repo = new WorkloadLogRepository(_context);
        var result = await repo.GetChangesAsync(
            new WorkloadChangeFilter(), 1, requested, CancellationToken.None);
        Assert.Equal(expected, result.PageSize);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
