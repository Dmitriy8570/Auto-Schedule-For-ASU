using Domain.constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ClassroomAvailabilityConfiguration : IEntityTypeConfiguration<ClassroomAvailability>
{
    public void Configure(EntityTypeBuilder<ClassroomAvailability> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Classroom)
               .WithMany(c => c.ClassroomAvailabilities)
               .HasForeignKey(a => a.ClassroomId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
