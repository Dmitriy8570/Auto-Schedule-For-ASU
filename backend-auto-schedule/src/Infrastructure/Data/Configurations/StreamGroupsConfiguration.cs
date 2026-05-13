using Domain.schedule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class StreamGroupsConfiguration : IEntityTypeConfiguration<StreamGroups>
{
    public void Configure(EntityTypeBuilder<StreamGroups> builder)
    {
        builder.HasKey(sg => new { sg.StreamId, sg.GroupId });

        builder.HasOne(sg => sg.Stream)
               .WithMany(s => s.StreamGroups)
               .HasForeignKey(sg => sg.StreamId);

        builder.HasOne(sg => sg.Group)
               .WithMany(g => g.StreamGroups)
               .HasForeignKey(sg => sg.GroupId);
    }
}
