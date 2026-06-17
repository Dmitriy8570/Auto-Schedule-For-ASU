using Domain.university.teachers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.HasOne(d => d.Institute)
               .WithMany(i => i.Departments)
               .HasForeignKey(d => d.InstituteId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
