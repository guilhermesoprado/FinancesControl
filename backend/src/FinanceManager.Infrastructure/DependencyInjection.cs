using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Security;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Infrastructure.Options;
using FinanceManager.Infrastructure.Persistence.Context;
using FinanceManager.Infrastructure.Persistence.Repositories;
using FinanceManager.Infrastructure.Services.Identity;
using FinanceManager.Infrastructure.Services.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FinanceManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("A connection string 'DefaultConnection' nao foi configurada.");

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = new JwtOptions
        {
            Issuer = jwtSection["Issuer"] ?? string.Empty,
            Audience = jwtSection["Audience"] ?? string.Empty,
            SecretKey = jwtSection["SecretKey"] ?? string.Empty,
            ExpirationInMinutes = int.TryParse(jwtSection["ExpirationInMinutes"], out var expirationInMinutes)
                ? expirationInMinutes
                : 120
        };

        services.AddSingleton<IOptions<JwtOptions>>(global::Microsoft.Extensions.Options.Options.Create(jwtOptions));

        services.AddDbContext<FinanceManagerDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFinancialAccountRepository, FinancialAccountRepository>();
        services.AddScoped<ITransactionCategoryRepository, TransactionCategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}
