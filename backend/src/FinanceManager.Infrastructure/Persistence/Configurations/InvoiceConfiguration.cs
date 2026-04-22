using FinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.CreditCardId)
            .IsRequired();

        builder.Property(x => x.ReferenceYear)
            .IsRequired();

        builder.Property(x => x.ReferenceMonth)
            .IsRequired();

        builder.Property(x => x.PeriodStart)
            .IsRequired();

        builder.Property(x => x.PeriodEnd)
            .IsRequired();

        builder.Property(x => x.ClosingDate)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PaidAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.LateFeeAppliedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.LateInterestAppliedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.RevolvingInterestAppliedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.ChargesAppliedUntilDate);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.PaidFromFinancialAccountId);

        builder.Property(x => x.PaidAtUtc);

        builder.Property(x => x.ClosedAtUtc);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CreditCard>()
            .WithMany()
            .HasForeignKey(x => x.CreditCardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FinancialAccount>()
            .WithMany()
            .HasForeignKey(x => x.PaidFromFinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UserId, x.CreditCardId, x.ReferenceYear, x.ReferenceMonth })
            .IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreditCardId });
    }
}
