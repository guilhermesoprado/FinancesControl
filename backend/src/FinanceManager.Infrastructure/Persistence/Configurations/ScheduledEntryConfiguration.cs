using FinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configurations;

public sealed class ScheduledEntryConfiguration : IEntityTypeConfiguration<ScheduledEntry>
{
    public void Configure(EntityTypeBuilder<ScheduledEntry> builder)
    {
        builder.ToTable("scheduled_entries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.FinancialAccountId)
            .IsRequired();

        builder.Property(x => x.TransactionCategoryId)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.PlanningMode)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.RecurrenceFrequency)
            .HasConversion<int?>();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.NextOccurrenceDate);

        builder.Property(x => x.EndDate);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FinancialAccount>()
            .WithMany()
            .HasForeignKey(x => x.FinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TransactionCategory>()
            .WithMany()
            .HasForeignKey(x => x.TransactionCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UserId, x.Status, x.NextOccurrenceDate });
        builder.HasIndex(x => new { x.UserId, x.FinancialAccountId });
        builder.HasIndex(x => new { x.UserId, x.TransactionCategoryId });
    }
}
