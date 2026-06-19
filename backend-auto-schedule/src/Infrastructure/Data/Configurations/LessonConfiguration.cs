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

        // Семестр занятия (денормализация); навигации со стороны Semester нет.
        builder.HasOne<Domain.calendar.Semester>()
               .WithMany()
               .HasForeignKey(l => l.SemesterId)
               .OnDelete(DeleteBehavior.Cascade);

        // Учебный план занятия (дисциплина/преподаватель/тип). Необязателен; при удалении
        // плана связь обнуляется (SetNull), чтобы опубликованное занятие пережило пересоздание
        // нагрузки при синхронизации с ММИС. Обратной коллекции на Curriculum нет.
        builder.HasOne(l => l.Curriculum)
               .WithMany()
               .HasForeignKey(l => l.CurriculumId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => l.SemesterId);
        builder.HasIndex(l => l.CurriculumId);
    }
}
