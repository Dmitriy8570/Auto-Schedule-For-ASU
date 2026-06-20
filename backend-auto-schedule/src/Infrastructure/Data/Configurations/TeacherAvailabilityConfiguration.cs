using Domain.constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class TeacherAvailabilityConfiguration : IEntityTypeConfiguration<TeacherAvailability>
{
    public void Configure(EntityTypeBuilder<TeacherAvailability> builder)
    {
        builder.HasKey(a => a.Id);

        // Id задаётся доменной фабрикой (TeacherAvailability.Create). Без этого EF по соглашению
        // считает Guid-ключ генерируемым на стороне БД и при добавлении через навигацию
        // отслеживаемого преподавателя помечает запись как Modified (UPDATE 0 строк) вместо Added.
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.HasOne(a => a.Teacher)
               .WithMany(t => t.TeacherAvailabilities)
               .HasForeignKey(a => a.TeacherId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
