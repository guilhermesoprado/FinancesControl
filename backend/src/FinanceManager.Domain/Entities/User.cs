using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Entities;

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string EmailNormalized { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }

    public static User Register(string fullName, string email, string passwordHash, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("O nome completo do usuario e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("O e-mail do usuario e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new InvalidOperationException("O hash de senha do usuario e obrigatorio.");
        }

        var sanitizedEmail = email.Trim().ToLowerInvariant();

        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = sanitizedEmail,
            EmailNormalized = sanitizedEmail.ToUpperInvariant(),
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }

    public void RegisterSuccessfulLogin(DateTime nowUtc)
    {
        LastLoginAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }
}
