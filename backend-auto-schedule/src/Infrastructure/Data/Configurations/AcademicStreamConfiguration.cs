using Domain.schedule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class AcademicStreamConfiguration : IEntityTypeConfiguration<AcademicStream>
{
    public void Configure(EntityTypeBuilder<AcademicStream> builder)
    {
        builder.HasKey(s => s.Id);
    }
}
