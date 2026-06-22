using Application.Common.Interfaces;
using Domain.schedule;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>EF Core-хранилище истории запусков автогенерации.</summary>
public sealed class GenerationRunRepository(ApplicationDbContext context) : IGenerationRunRepository
{
    public async Task AddAsync(GenerationRun run, CancellationToken ct)
    {
        await context.GenerationRuns.AddAsync(run, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<GenerationRun>> GetRecentAsync(
        Guid? semesterId, Guid? instituteId, int limit, CancellationToken ct)
    {
        var query = context.GenerationRuns.AsNoTracking();

        if (semesterId is { } sid) query = query.Where(r => r.SemesterId == sid);
        if (instituteId is { } iid) query = query.Where(r => r.InstituteId == iid);

        return await query
            .OrderByDescending(r => r.CompletedAt)
            .Take(limit)
            .ToListAsync(ct);
    }
}
