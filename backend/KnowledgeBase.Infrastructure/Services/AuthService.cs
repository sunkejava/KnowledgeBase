using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Auth;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>JWT 身份认证服务，密码使用 PBKDF2-SHA256 保存。</summary>
public sealed class AuthService(KnowledgeDbContext dbContext, IConfiguration configuration) : IAuthService
{
    public async Task EnsureDefaultAdminAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken)) return;

        var (hash, salt) = HashPassword("Admin123!");
        var user = new SysUser("admin", "系统管理员", hash, salt);
        var role = new SysRole("SUPER_ADMIN", "超级管理员");
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.UserRoles.Add(new SysUserRole(user.Id, role.Id));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserName == request.UserName && x.Enabled, cancellationToken);
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt)) return null;

        var roles = await (from ur in dbContext.UserRoles
                           join role in dbContext.Roles on ur.RoleId equals role.Id
                           where ur.UserId == user.Id && role.Enabled
                           select role.Code).ToListAsync(cancellationToken);

        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("display_name", user.DisplayName)
        };
        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key 未配置")));
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt,
            new CurrentUserDto(user.Id, user.UserName, user.DisplayName, roles));
    }

    private static (string Hash, string Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    private static bool VerifyPassword(string password, string expectedHash, string salt)
    {
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), 120_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(expectedHash));
    }
}
