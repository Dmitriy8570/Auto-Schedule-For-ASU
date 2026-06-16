using Domain.calendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class WeekDayConfiguration : IEntityTypeConfiguration<WeekDay>
{
    public void Configure(EntityTypeBuilder<WeekDay> builder)
    {
        builder.HasKey(wd => wd.Id);

        builder.HasOne(wd => wd.Week)
               .WithMany(w => w.WeekDays)
               .HasForeignKey(wd => wd.WeekId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
