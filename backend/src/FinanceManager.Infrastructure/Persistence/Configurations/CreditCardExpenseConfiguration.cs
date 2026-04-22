using FinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configurations;

public sealed class CreditCardExpenseConfiguration : IEntityTypeConfiguration<CreditCardExpense>
{
    public void Configure(EntityTypeBuilder<CreditCardExpense> builder)
    {
        builder.ToTable("credit_card_expenses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.CreditCardId)
            .IsRequired();

        builder.Property(x => x.InvoiceId)
            .IsRequired();

        builder.Property(x => x.TransactionCategoryId)
            .IsRequired();

        builder.Property(x => x.InstallmentGroupId)
            .IsRequired();

        builder.Property(x => x.InstallmentNumber)
            .IsRequired();

        builder.Property(x => x.InstallmentCount)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.OccurredOn)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

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

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TransactionCategory>()
            .WithMany()
            .HasForeignKey(x => x.TransactionCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UserId, x.CreditCardId, x.OccurredOn });
        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => x.InstallmentGroupId);
    }
}
