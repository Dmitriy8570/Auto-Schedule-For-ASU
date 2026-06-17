using Domain.university.groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
               .IsRequired()
               .HasMaxLength(20);

        builder.HasOne(g => g.Course)
               .WithMany(c => c.Groups)
               .HasForeignKey(g => g.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

        // Самоссылка: подгруппы → родительская группа; для основной группы родителя нет.
        builder.HasOne(g => g.ParentGroup)
               .WithMany(g => g.Groups)
               .HasForeignKey(g => g.ParentGroupId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
