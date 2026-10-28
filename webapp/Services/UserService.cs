using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public enum LoginResult
{
    Success,
    InvalidCredentials,
    Inactive,
    LockedOut,
}

public enum ChangePasswordResult
{
    Success,
    WrongCurrentPassword,
}

public class UserService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPasswordHasher<User> _hasher;
    private readonly CurrentUserService _currentUser;

    public UserService(IDbContextFactory<AppDbContext> dbFactory, IPasswordHasher<User> hasher, CurrentUserService currentUser)
    {
        _dbFactory = dbFactory;
        _hasher = hasher;
        _currentUser = currentUser;
    }

    private async Task EnsureAdminAsync()
    {
        if (!await _currentUser.IsAdminAsync())
            throw new UnauthorizedAccessException("Cette action est réservée aux administrateurs.");
    }

    // The timestamp columns are `without time zone` and reject a Kind=Utc DateTime.
    // Value stays UTC, just tagged the way Npgsql needs.
    private static DateTime NowForDb() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<(LoginResult Result, User? User)> LoginAsync(string login, string password)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Login == login);
        if (user is null) return (LoginResult.InvalidCredentials, null);
        if (!user.IsActive) return (LoginResult.Inactive, null);

        if (user.LockoutUntil is { } lockedUntil && lockedUntil > NowForDb())
            return (LoginResult.LockedOut, null);

        // Seeded accounts carry an empty hash: the first login SETS the password
        // instead of checking one. See database/05_users_and_roles.sql.
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
            user.MustChangePassword = false;
            user.LastLoginAt = NowForDb();
            user.FailedLoginCount = 0;
            user.LockoutUntil = null;
            db.LoginHistories.Add(new LoginHistory { Login = user.Login, LoggedInAt = NowForDb() });
            await db.SaveChangesAsync();
            return (LoginResult.Success, user);
        }

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verify == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
                user.LockoutUntil = NowForDb() + LockoutDuration;
            await db.SaveChangesAsync();
            return (LoginResult.InvalidCredentials, null);
        }

        user.LastLoginAt = NowForDb();
        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        db.LoginHistories.Add(new LoginHistory { Login = user.Login, LoggedInAt = NowForDb() });
        await db.SaveChangesAsync();
        return (LoginResult.Success, user);
    }

    public async Task<List<User>> ListAsync()
    {
        await EnsureAdminAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.OrderBy(u => u.Login).ToListAsync();
    }

    public async Task CreateAsync(string login, string role)
    {
        await EnsureAdminAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Users.Add(new User
        {
            Login = login,
            Role = role,
            PasswordHash = "",
            MustChangePassword = true,
            IsActive = true,
            CreatedAt = NowForDb(),
        });
        await db.SaveChangesAsync();
    }

    public async Task CreateWithPasswordAsync(string login, string role, string password)
    {
        await EnsureAdminAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = new User
        {
            Login = login,
            Role = role,
            MustChangePassword = false,
            IsActive = true,
            CreatedAt = NowForDb(),
        };
        user.PasswordHash = _hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateRoleAsync(string login, string role)
    {
        await EnsureAdminAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Users.Where(u => u.Login == login).ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, role));
    }

    public async Task SetActiveAsync(string login, bool isActive)
    {
        await EnsureAdminAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Users.Where(u => u.Login == login).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, isActive));
    }

    public async Task ForcePasswordResetAsync(string login)
    {
        await EnsureAdminAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Users.Where(u => u.Login == login).ExecuteUpdateAsync(s => s
            .SetProperty(u => u.PasswordHash, "")
            .SetProperty(u => u.MustChangePassword, true));
    }

    public async Task<ChangePasswordResult> ChangeOwnPasswordAsync(string login, string currentPassword, string newPassword)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstAsync(u => u.Login == login);

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (verify == PasswordVerificationResult.Failed) return ChangePasswordResult.WrongCurrentPassword;

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        await db.SaveChangesAsync();
        return ChangePasswordResult.Success;
    }

    public async Task<List<LoginHistory>> GetLoginHistoryAsync(int limit = 200)
    {
        await EnsureAdminAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.LoginHistories
            .OrderByDescending(h => h.LoggedInAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<(List<string> AssistantesWithoutLogin, List<string> LoginsWithoutAssistanteProfile)> GetIdentityDriftAsync()
    {
        await EnsureAdminAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var assistanteNames = await db.Assistantes.Select(a => a.PrenomNom).ToListAsync();
        var loginNames = await db.Users.Select(u => u.Login).ToListAsync();

        var withoutLogin = assistanteNames.Except(loginNames).OrderBy(n => n).ToList();
        var withoutProfile = loginNames.Except(assistanteNames).OrderBy(n => n).ToList();
        return (withoutLogin, withoutProfile);
    }
}
