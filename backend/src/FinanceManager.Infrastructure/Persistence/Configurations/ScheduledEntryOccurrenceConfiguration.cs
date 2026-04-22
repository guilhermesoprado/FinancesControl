using FinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configurations;

public sealed class ScheduledEntryOccurrenceConfiguration : IEntityTypeConfiguration<ScheduledEntryOccurrence>
{
    public void Configure(EntityTypeBuilder<ScheduledEntryOccurrence> builder)
    {
        builder.ToTable("scheduled_entry_occurrences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.ScheduledEntryId)
            .IsRequired();

        builder.Property(x => x.OccurrenceDate)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<ScheduledEntry>()
            .WithMany()
            .HasForeignKey(x => x.ScheduledEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.Status, x.OccurrenceDate });
        builder.HasIndex(x => new { x.ScheduledEntryId, x.OccurrenceDate });
    }
}
