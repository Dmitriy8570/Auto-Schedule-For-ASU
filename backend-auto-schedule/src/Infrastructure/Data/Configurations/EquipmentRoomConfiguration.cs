using Domain.constraints.equipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class EquipmentRoomConfiguration : IEntityTypeConfiguration<EquipmentRoom>
{
    public void Configure(EntityTypeBuilder<EquipmentRoom> builder)
    {
        builder.HasKey(er => new { er.EquipmentId, er.ClassroomId });

        builder.HasOne(er => er.Equipment)
               .WithMany(e => e.EquipmentRooms)
               .HasForeignKey(er => er.EquipmentId);

        builder.HasOne(er => er.Classroom)
               .WithMany(c => c.EquipmentRooms)
               .HasForeignKey(er => er.ClassroomId);
    }
}
