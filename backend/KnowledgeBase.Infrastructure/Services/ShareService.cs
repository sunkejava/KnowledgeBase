using System.Security.Cryptography;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Common;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 文档分享服务，负责分享密码、访问计数与访问日志。
/// </summary>
public sealed class ShareService(KnowledgeDbContext db) : IShareService
{
    private const int PasswordIterations = 120_000;

    /// <summary>创建文档分享链接。</summary>
    public async Task<ShareLinkDto?> CreateAsync(
        Guid documentId,
        Guid? userId,
        DateTimeOffset? expiresAt,
        string? password,
        CancellationToken ct)
    {
        if (!await db.Documents.AnyAsync(x => x.Id == documentId, ct)) return null;
        var entity = new DocumentShareLink(documentId, userId, expiresAt, HashPassword(password));
        db.DocumentShareLinks.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    /// <summary>获取文档分享链接列表。SQLite 下 CreatedAt 排序统一走安全排序扩展。</summary>
    public Task<IReadOnlyList<ShareLinkDto>> GetDocumentLinksAsync(Guid documentId, CancellationToken ct)
        => db.DocumentShareLinks.AsNoTracking()
            .Where(x => x.DocumentId == documentId)
            .Select(x => new ShareLinkDto(
                x.Id,
                x.DocumentId,
                x.Token,
                x.ExpiresAt,
                x.Enabled,
                x.PasswordHash != null,
                x.AccessCount,
                x.LastAccessAt,
                x.CreatedAt))
            .ToSqliteSafeDateTimeOffsetListAsync(x => x.CreatedAt, descending: true, ct);

    /// <summary>停用指定分享链接。</summary>
    public async Task<bool> DisableAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.DocumentShareLinks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;
        entity.Disable();
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>根据分享链接 ID 获取所属文档 ID。</summary>
    public Task<Guid?> GetDocumentIdByLinkAsync(Guid id, CancellationToken ct)
        => db.DocumentShareLinks.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => (Guid?)x.DocumentId)
            .FirstOrDefaultAsync(ct);

    /// <summary>匿名读取公开分享文档，并写入访问审计。</summary>
    public async Task<SharedDocumentDto?> GetSharedDocumentAsync(
        string token,
        string? password,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        var link = await db.DocumentShareLinks.FirstOrDefaultAsync(x => x.Token == token, ct);
        if (link is null) return null;

        if (!link.Enabled)
        {
            await WriteLogAsync(link.Id, ipAddress, userAgent, false, "分享链接已停用", ct);
            return null;
        }

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            await WriteLogAsync(link.Id, ipAddress, userAgent, false, "分享链接已过期", ct);
            return null;
        }

        if (!VerifyPassword(password, link.PasswordHash))
        {
            await WriteLogAsync(link.Id, ipAddress, userAgent, false, "分享密码错误或缺失", ct);
            return null;
        }

        var document = await (
            from d in db.Documents.AsNoTracking()
            join c in db.DocumentContents.AsNoTracking() on d.Id equals c.DocumentId
            where d.Id == link.DocumentId
            select new SharedDocumentDto(d.Id, d.Title, c.Markdown, link.ExpiresAt))
            .FirstOrDefaultAsync(ct);
        if (document is null) return null;

        link.RecordAccess();
        db.ShareAccessLogs.Add(new ShareAccessLog(link.Id, ipAddress, userAgent, true, "访问成功"));
        await db.SaveChangesAsync(ct);
        return document;
    }

    /// <summary>获取分享访问日志。SQLite 下 AccessedAt 排序在投影后由内存完成。</summary>
    public async Task<IReadOnlyList<ShareAccessLogDto>> GetAccessLogsAsync(Guid documentId, int take, CancellationToken ct)
    {
        var source =
            from log in db.ShareAccessLogs.AsNoTracking()
            join link in db.DocumentShareLinks.AsNoTracking() on log.ShareLinkId equals link.Id
            where link.DocumentId == documentId
            select new ShareAccessLogDto(
                log.Id,
                log.ShareLinkId,
                log.IpAddress,
                log.UserAgent,
                log.Success,
                log.Message,
                log.AccessedAt);

        var rows = await source.ToSqliteSafeDateTimeOffsetListAsync(x => x.AccessedAt, descending: true, ct);
        return rows.Take(Math.Clamp(take, 1, 500)).ToList();
    }

    private async Task WriteLogAsync(
        Guid linkId,
        string? ip,
        string? userAgent,
        bool success,
        string message,
        CancellationToken ct)
    {
        db.ShareAccessLogs.Add(new ShareAccessLog(linkId, ip, userAgent, success, message));
        await db.SaveChangesAsync(ct);
    }

    private static ShareLinkDto Map(DocumentShareLink x)
        => new(x.Id, x.DocumentId, x.Token, x.ExpiresAt, x.Enabled, x.PasswordHash != null, x.AccessCount, x.LastAccessAt, x.CreatedAt);

    private static string? HashPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return null;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, 32);
        return $"{PasswordIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string? password, string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded)) return true;
        if (string.IsNullOrEmpty(password)) return false;
        var parts = encoded.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;
        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
