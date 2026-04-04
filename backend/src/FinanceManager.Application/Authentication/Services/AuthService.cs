using FinanceManager.Application.Authentication.Contracts;
using FinanceManager.Application.Common.Abstractions.Persistence;
using FinanceManager.Application.Common.Abstractions.Security;
using FinanceManager.Application.Common.Abstractions.Time;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Authentication.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterUserInput input, CancellationToken cancellationToken)
    {
        ValidateRegistrationInput(input);

        var normalizedEmail = NormalizeEmail(input.Email);
        var userAlreadyExists = await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);

        if (userAlreadyExists)
        {
            throw new AppValidationException("Ja existe um usuario cadastrado com este e-mail.");
        }

        var passwordHash = _passwordHasher.HashPassword(input.Password);
        var user = User.Register(input.FullName, input.Email, passwordHash, _dateTimeProvider.UtcNow);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user);

        return CreateAuthResult(user, accessToken);
    }

    public async Task<AuthResultDto> LoginAsync(LoginInput input, CancellationToken cancellationToken)
    {
        ValidateLoginInput(input);

        var normalizedEmail = NormalizeEmail(input.Email);
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !_passwordHasher.VerifyPassword(user.PasswordHash, input.Password))
        {
            throw new AppUnauthorizedException("E-mail ou senha invalidos.");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new AppUnauthorizedException("O usuario esta inativo e nao pode acessar a plataforma.");
        }

        user.RegisterSuccessfulLogin(_dateTimeProvider.UtcNow);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user);

        return CreateAuthResult(user, accessToken);
    }

    public async Task<AuthenticatedUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado.");
        }

        return new AuthenticatedUserDto(user.Id, user.FullName, user.Email);
    }

    private static AuthResultDto CreateAuthResult(User user, AccessTokenResult accessToken)
    {
        return new AuthResultDto(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            new AuthenticatedUserDto(user.Id, user.FullName, user.Email));
    }

    private static void ValidateRegistrationInput(RegisterUserInput input)
    {
        if (string.IsNullOrWhiteSpace(input.FullName))
        {
            throw new AppValidationException("O nome completo e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(input.Email))
        {
            throw new AppValidationException("O e-mail e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            throw new AppValidationException("A senha e obrigatoria.");
        }

        if (input.Password.Trim().Length < 8)
        {
            throw new AppValidationException("A senha deve ter pelo menos 8 caracteres.");
        }
    }

    private static void ValidateLoginInput(LoginInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Email))
        {
            throw new AppValidationException("O e-mail e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            throw new AppValidationException("A senha e obrigatoria.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}
