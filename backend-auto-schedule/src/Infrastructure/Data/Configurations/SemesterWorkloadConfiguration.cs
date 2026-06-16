using Domain.workload;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class SemesterWorkloadConfiguration : IEntityTypeConfiguration<SemesterWorkload>
{
    public void Configure(EntityTypeBuilder<SemesterWorkload> builder)
    {
        builder.HasKey(sw => sw.Id);

        builder.HasOne(sw => sw.Curriculum)
               .WithMany(c => c.SemesterWorkloads)
               .HasForeignKey(sw => sw.CurriculumId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sw => sw.Semester)
               .WithMany(s => s.SemesterWorkloads)
               .HasForeignKey(sw => sw.SemesterId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
