using Domain.university.teachers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasOne(t => t.Department)
               .WithMany(d => d.Teachers)
               .HasForeignKey(t => t.DepartmentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
