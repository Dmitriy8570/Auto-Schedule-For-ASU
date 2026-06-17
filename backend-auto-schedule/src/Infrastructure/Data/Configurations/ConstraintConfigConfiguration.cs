using Domain.constraints.penalty;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ConstraintConfigConfiguration : IEntityTypeConfiguration<ConstraintConfig>
{
    public void Configure(EntityTypeBuilder<ConstraintConfig> builder)
    {
        builder.HasKey(c => c.Id);

        // Один вес штрафа на каждый тип мягкого ограничения.
        builder.HasIndex(c => c.ConstraintType).IsUnique();
    }
}
