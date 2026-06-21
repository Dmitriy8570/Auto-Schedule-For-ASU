using Domain.workload.logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class SemesterLogConfiguration : IEntityTypeConfiguration<SemesterLog>
{
    public void Configure(EntityTypeBuilder<SemesterLog> builder)
    {
        builder.HasKey(l => l.Id);

        // Журнал сортируется и пагинируется по времени — индекс ускоряет ORDER BY/диапазонные фильтры.
        builder.HasIndex(l => l.TimeStamp);

        // Связь опциональна: при физическом удалении нагрузки запись Delete остаётся,
        // а внешний ключ обнуляется (SetNull) — журнал переживает удаление.
        builder.HasOne(l => l.SemesterWorkload)
               .WithMany(sw => sw.SemesterLogs)
               .HasForeignKey(l => l.SemesterWorkloadId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
