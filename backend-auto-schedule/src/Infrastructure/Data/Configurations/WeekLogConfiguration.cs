using Domain.workload.logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class WeekLogConfiguration : IEntityTypeConfiguration<WeekLog>
{
    public void Configure(EntityTypeBuilder<WeekLog> builder)
    {
        builder.HasKey(l => l.Id);

        // Журнал сортируется и пагинируется по времени — индекс ускоряет ORDER BY/диапазонные фильтры.
        builder.HasIndex(l => l.TimeStamp);

        // Связь опциональна: при физическом удалении нагрузки запись Delete остаётся,
        // а внешний ключ обнуляется (SetNull) — журнал переживает удаление.
        builder.HasOne(l => l.WeekWorkload)
               .WithMany(ww => ww.WeekLogs)
               .HasForeignKey(l => l.WeekWorkloadId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
