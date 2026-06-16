using Domain.university.groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasOne(c => c.Degree)
               .WithMany(d => d.Courses)
               .HasForeignKey(c => c.DegreeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
