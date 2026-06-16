using Domain.constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class TeacherAvailabilityConfiguration : IEntityTypeConfiguration<TeacherAvailability>
{
    public void Configure(EntityTypeBuilder<TeacherAvailability> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Teacher)
               .WithMany(t => t.TeacherAvailabilities)
               .HasForeignKey(a => a.TeacherId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
