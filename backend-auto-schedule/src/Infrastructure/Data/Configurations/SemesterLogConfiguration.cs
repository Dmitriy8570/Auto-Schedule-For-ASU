using Domain.workload.logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class SemesterLogConfiguration : IEntityTypeConfiguration<SemesterLog>
{
    public void Configure(EntityTypeBuilder<SemesterLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.SemesterWorkload)
               .WithMany(sw => sw.SemesterLogs)
               .HasForeignKey(l => l.SemesterWorkloadId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
