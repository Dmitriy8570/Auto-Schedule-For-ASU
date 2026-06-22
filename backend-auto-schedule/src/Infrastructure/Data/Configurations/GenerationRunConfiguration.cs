using Domain.schedule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class GenerationRunConfiguration : IEntityTypeConfiguration<GenerationRun>
{
    public void Configure(EntityTypeBuilder<GenerationRun> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.SemesterName).IsRequired().HasMaxLength(300);
        builder.Property(r => r.InstituteName).IsRequired().HasMaxLength(300);
        builder.Property(r => r.Status).IsRequired().HasMaxLength(100);
        builder.Property(r => r.UnplacedJson).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.Error).HasMaxLength(2000);

        // Список истории всегда сортируется по времени завершения убыванием — индекс под это.
        builder.HasIndex(r => r.CompletedAt);
        builder.HasIndex(r => new { r.SemesterId, r.InstituteId });
    }
}
