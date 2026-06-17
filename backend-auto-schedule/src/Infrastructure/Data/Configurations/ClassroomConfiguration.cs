using Domain.university.buildings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasOne(c => c.Building)
               .WithMany(b => b.Classrooms)
               .HasForeignKey(c => c.BuildingId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
