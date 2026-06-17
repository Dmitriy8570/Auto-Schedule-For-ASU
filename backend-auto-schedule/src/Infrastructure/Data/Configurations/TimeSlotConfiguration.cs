using Domain.schedule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> builder)
    {
        builder.HasKey(ts => ts.Id);

        builder.HasOne(ts => ts.WeekDay)
               .WithMany(wd => wd.TimeSlots)
               .HasForeignKey(ts => ts.WeekDayId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
