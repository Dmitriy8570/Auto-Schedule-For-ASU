using Domain.calendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class WeekConfiguration : IEntityTypeConfiguration<Week>
{
    public void Configure(EntityTypeBuilder<Week> builder)
    {
        builder.HasKey(w => w.Id);

        builder.HasOne(w => w.Semester)
               .WithMany(s => s.Weeks)
               .HasForeignKey(w => w.SemesterId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
