using Domain.constraints.equipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class NeededEquipmentConfiguration : IEntityTypeConfiguration<NeededEquipment>
{
    public void Configure(EntityTypeBuilder<NeededEquipment> builder)
    {
        builder.HasKey(ne => new { ne.CurriculumId, ne.EquipmentId });

        builder.HasOne(ne => ne.Curriculum)
               .WithMany(c => c.NeededEquipments)
               .HasForeignKey(ne => ne.CurriculumId);

        builder.HasOne(ne => ne.Equipment)
               .WithMany(e => e.NeededEquipments)
               .HasForeignKey(ne => ne.EquipmentId);
    }
}
