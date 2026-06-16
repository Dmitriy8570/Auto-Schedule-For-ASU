using Domain.workload;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class WeekWorkloadConfiguration : IEntityTypeConfiguration<WeekWorkload>
{
    public void Configure(EntityTypeBuilder<WeekWorkload> builder)
    {
        builder.HasKey(ww => ww.Id);

        builder.HasOne(ww => ww.SemesterWorkload)
               .WithMany(sw => sw.WeekWorkloads)
               .HasForeignKey(ww => ww.SemesterWorkloadId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ww => ww.Curriculum)
               .WithMany(c => c.WeekWorkloads)
               .HasForeignKey(ww => ww.CurriculumId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ww => ww.Week)
               .WithMany(w => w.WeekWorkloads)
               .HasForeignKey(ww => ww.WeekId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
