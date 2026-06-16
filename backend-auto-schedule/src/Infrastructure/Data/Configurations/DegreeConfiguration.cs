using Domain.university.groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class DegreeConfiguration : IEntityTypeConfiguration<Degree>
{
    public void Configure(EntityTypeBuilder<Degree> builder)
    {
        builder.HasKey(d => d.Id);

        builder.HasOne(d => d.Institute)
               .WithMany(i => i.Degrees)
               .HasForeignKey(d => d.InstituteId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
