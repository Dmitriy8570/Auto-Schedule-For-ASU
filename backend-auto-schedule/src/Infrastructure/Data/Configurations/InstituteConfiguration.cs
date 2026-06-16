using Domain.university.common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class InstituteConfiguration : IEntityTypeConfiguration<Institute>
{
    public void Configure(EntityTypeBuilder<Institute> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
               .IsRequired()
               .HasMaxLength(200);
    }
}
