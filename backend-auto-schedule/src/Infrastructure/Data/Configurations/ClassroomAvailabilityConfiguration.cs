using Domain.constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ClassroomAvailabilityConfiguration : IEntityTypeConfiguration<ClassroomAvailability>
{
    public void Configure(EntityTypeBuilder<ClassroomAvailability> builder)
    {
        builder.HasKey(a => a.Id);

        // Id задаётся доменной фабрикой (ClassroomAvailability.Create). Иначе EF считает Guid-ключ
        // генерируемым БД и при добавлении через навигацию отслеживаемой аудитории помечает запись
        // как Modified (UPDATE 0 строк) вместо Added.
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.HasOne(a => a.Classroom)
               .WithMany(c => c.ClassroomAvailabilities)
               .HasForeignKey(a => a.ClassroomId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
