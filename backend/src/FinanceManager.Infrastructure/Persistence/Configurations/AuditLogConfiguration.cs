using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.EntityId)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Summary)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.EntityType, x.EntityId, x.CreatedAtUtc });
    }
}
