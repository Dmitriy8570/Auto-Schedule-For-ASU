using Domain.schedule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.Classroom)
               .WithMany(c => c.Lessons)
               .HasForeignKey(l => l.ClassroomId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Stream)
               .WithMany(s => s.Lessons)
               .HasForeignKey(l => l.StreamId)
               .OnDelete(DeleteBehavior.Cascade);

        // У TimeSlot нет обратной коллекции занятий.
        builder.HasOne(l => l.TimeSlot)
               .WithMany()
               .HasForeignKey(l => l.TimeSlotId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
