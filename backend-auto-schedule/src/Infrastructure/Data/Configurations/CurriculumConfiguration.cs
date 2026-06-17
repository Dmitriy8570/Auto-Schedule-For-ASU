using Domain.workload;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class CurriculumConfiguration : IEntityTypeConfiguration<Curriculum>
{
    public void Configure(EntityTypeBuilder<Curriculum> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasOne(c => c.Teacher)
               .WithMany(t => t.Curriculums)
               .HasForeignKey(c => c.TeacherId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Stream)
               .WithMany(s => s.Curriculums)
               .HasForeignKey(c => c.StreamId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Subject)
               .WithMany(s => s.Curriculums)
               .HasForeignKey(c => c.SubjectId)
               .OnDelete(DeleteBehavior.Cascade);

        // Предпочтительный корпус — мягкое ограничение, связь опциональна.
        builder.HasOne(c => c.FavoriteBuilding)
               .WithMany(b => b.Curriculums)
               .HasForeignKey(c => c.FavoriteBuildingId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
