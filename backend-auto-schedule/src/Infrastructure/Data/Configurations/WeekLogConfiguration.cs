using Domain.workload.logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class WeekLogConfiguration : IEntityTypeConfiguration<WeekLog>
{
    public void Configure(EntityTypeBuilder<WeekLog> builder)
    {
        builder.HasKey(l => l.Id);

        // Связь опциональна: при физическом удалении нагрузки запись Delete остаётся,
        // а внешний ключ обнуляется (SetNull) — журнал переживает удаление.
        builder.HasOne(l => l.WeekWorkload)
               .WithMany(ww => ww.WeekLogs)
               .HasForeignKey(l => l.WeekWorkloadId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
