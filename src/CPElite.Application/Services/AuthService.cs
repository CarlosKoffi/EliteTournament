using CPElite.Application.Abstractions;
using CPElite.Contracts.Auth;
using CPElite.Contracts.Common;
using CPElite.Contracts.Users;
using CPElite.Domain.Entities;
using DomainPlatform = CPElite.Domain.Enums.Platform;

namespace CPElite.Application.Services;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly ITeamRepository _teams;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokens;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUserRepository users, ITeamRepository teams, IPasswordHasher passwordHasher, ITokenService tokens, IClock clock, IUnitOfWork unitOfWork)
    {
        _users = users;
        _teams = teams;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterPlayerRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateRegistration(request);
        if (validation is not null)
        {
            return Result<AuthResponse>.Failure(ErrorType.Validation, "auth.validation", validation);
        }

        var normalizedEmail = Normalize(request.Email);
        if (await _users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            return Result<AuthResponse>.Failure(ErrorType.Conflict, "auth.email_taken", "An account already exists for this email.");
        }

        if (!string.IsNullOrWhiteSpace(request.EaSportsId))
        {
            var existingPlayer = await _users.GetByEaIdentityAsync(request.EaSportsId, request.Gamertag ?? request.DisplayName, cancellationToken);
            if (existingPlayer is not null)
            {
                return Result<AuthResponse>.Failure(ErrorType.Conflict, "auth.ea_player_taken", "This EA player is already linked to an account.");
            }
        }

        var user = new User(
            Guid.NewGuid(),
            request.Email.Trim(),
            normalizedEmail,
            _passwordHasher.Hash(request.Password),
            request.DisplayName.Trim(),
            string.IsNullOrWhiteSpace(request.Gamertag) ? null : request.Gamertag.Trim(),
            string.IsNullOrWhiteSpace(request.EaSportsId) ? null : request.EaSportsId.Trim(),
            string.IsNullOrWhiteSpace(request.DiscordUserId) ? null : request.DiscordUserId.Trim(),
            (DomainPlatform)(int)request.Platform,
            string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "en" : request.PreferredLanguage.Trim(),
            string.IsNullOrWhiteSpace(request.TimeZone) ? "UTC" : request.TimeZone.Trim(),
            _clock.UtcNow,
            eaClubId: request.EaClubId,
            eaClubName: string.IsNullOrWhiteSpace(request.EaClubName) ? null : request.EaClubName.Trim());

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(CreateAuthResponse(user));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByNormalizedEmailAsync(Normalize(request.Email), cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Failure(ErrorType.Unauthorized, "auth.invalid_credentials", "Invalid email or password.");
        }

        user.MarkLoggedIn(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(CreateAuthResponse(user));
    }

    public async Task<Result<MeResponse>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<MeResponse>.Failure(ErrorType.NotFound, "user.not_found", "User was not found.");
        }

        var memberships = await _teams.GetMembershipsForUserAsync(userId, cancellationToken);
        return Result<MeResponse>.Success(new MeResponse(Mapping.ToSummary(user), memberships.Select(Mapping.ToMemberResponse).ToArray()));
    }

    public async Task<Result<UserSummaryResponse>> UpdatePlayerProfileAsync(Guid userId, UpdatePlayerProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Result<UserSummaryResponse>.Failure(ErrorType.Validation, "user.display_name_required", "Display name is required.");
        }

        if (request.Platform == Platform.Unknown)
        {
            return Result<UserSummaryResponse>.Failure(ErrorType.Validation, "user.platform_required", "Platform is required.");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<UserSummaryResponse>.Failure(ErrorType.NotFound, "user.not_found", "User was not found.");
        }

        user.UpdatePlayerProfile(
            request.DisplayName.Trim(),
            string.IsNullOrWhiteSpace(request.Gamertag) ? null : request.Gamertag.Trim(),
            string.IsNullOrWhiteSpace(request.EaSportsId) ? null : request.EaSportsId.Trim(),
            string.IsNullOrWhiteSpace(request.DiscordUserId) ? null : request.DiscordUserId.Trim(),
            (DomainPlatform)(int)request.Platform,
            string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "en" : request.PreferredLanguage.Trim(),
            string.IsNullOrWhiteSpace(request.TimeZone) ? "UTC" : request.TimeZone.Trim(),
            string.IsNullOrWhiteSpace(request.ProfileImageUrl) ? null : request.ProfileImageUrl.Trim());

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<UserSummaryResponse>.Success(Mapping.ToSummary(user));
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = _tokens.Create(user);
        return new AuthResponse(token.AccessToken, token.ExpiresAt, Mapping.ToSummary(user));
    }

    private static string? ValidateRegistration(RegisterPlayerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return "A valid email is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return "Password must contain at least 8 characters.";
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return "Display name is required.";
        }

        if (request.Platform == Platform.Unknown)
        {
            return "Platform is required.";
        }

        return null;
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
