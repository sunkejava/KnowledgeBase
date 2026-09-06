using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 知识库与文档资源权限服务。
/// 文档显式权限优先于知识库成员角色；没有文档显式权限时再回退到知识库角色。
/// </summary>
public sealed class AccessControlService(KnowledgeDbContext db) : IAccessControlService
{
    public async Task<IReadOnlyList<KnowledgeBaseMemberDto>> GetKnowledgeBaseMembersAsync(Guid knowledgeBaseId, CancellationToken ct)
        => await (
            from member in db.KnowledgeBaseMembers.AsNoTracking()
            join user in db.Users.AsNoTracking() on member.UserId equals user.Id
            where member.KnowledgeBaseId == knowledgeBaseId
            orderby user.DisplayName
            select new KnowledgeBaseMemberDto(member.KnowledgeBaseId, member.UserId, user.UserName, user.DisplayName, member.Role, member.CreatedAt)
        ).ToListAsync(ct);

    public async Task<KnowledgeBaseMemberDto> SetKnowledgeBaseMemberAsync(Guid knowledgeBaseId, SetKnowledgeBaseMemberRequest request, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstAsync(x => x.Id == request.UserId, ct);
        var entity = await db.KnowledgeBaseMembers.FirstOrDefaultAsync(
            x => x.KnowledgeBaseId == knowledgeBaseId && x.UserId == request.UserId, ct);

        if (entity is null)
        {
            entity = new KnowledgeBaseMember(knowledgeBaseId, request.UserId, NormalizeRole(request.Role));
            db.KnowledgeBaseMembers.Add(entity);
        }
        else
        {
            entity.SetRole(NormalizeRole(request.Role));
        }

        await db.SaveChangesAsync(ct);
        return new(entity.KnowledgeBaseId, entity.UserId, user.UserName, user.DisplayName, entity.Role, entity.CreatedAt);
    }

    public async Task<bool> RemoveKnowledgeBaseMemberAsync(Guid knowledgeBaseId, Guid userId, CancellationToken ct)
    {
        var entity = await db.KnowledgeBaseMembers.FirstOrDefaultAsync(
            x => x.KnowledgeBaseId == knowledgeBaseId && x.UserId == userId, ct);
        if (entity is null) return false;

        db.KnowledgeBaseMembers.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<DocumentPermissionDto>> GetDocumentPermissionsAsync(Guid documentId, CancellationToken ct)
        => await (
            from permission in db.DocumentUserPermissions.AsNoTracking()
            join user in db.Users.AsNoTracking() on permission.UserId equals user.Id
            where permission.DocumentId == documentId
            orderby user.DisplayName
            select new DocumentPermissionDto(
                permission.DocumentId,
                permission.UserId,
                user.UserName,
                user.DisplayName,
                permission.CanView,
                permission.CanEdit,
                permission.CanManage)
        ).ToListAsync(ct);

    public async Task<DocumentPermissionDto> SetDocumentPermissionAsync(Guid documentId, SetDocumentPermissionRequest request, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstAsync(x => x.Id == request.UserId, ct);
        var entity = await db.DocumentUserPermissions.FirstOrDefaultAsync(
            x => x.DocumentId == documentId && x.UserId == request.UserId, ct);

        if (entity is null)
        {
            entity = new DocumentUserPermission(documentId, request.UserId, request.CanView, request.CanEdit, request.CanManage);
            db.DocumentUserPermissions.Add(entity);
        }
        else
        {
            entity.Update(request.CanView, request.CanEdit, request.CanManage);
        }

        await db.SaveChangesAsync(ct);
        return new(entity.DocumentId, entity.UserId, user.UserName, user.DisplayName, entity.CanView, entity.CanEdit, entity.CanManage);
    }

    public async Task<bool> RemoveDocumentPermissionAsync(Guid documentId, Guid userId, CancellationToken ct)
    {
        var entity = await db.DocumentUserPermissions.FirstOrDefaultAsync(
            x => x.DocumentId == documentId && x.UserId == userId, ct);
        if (entity is null) return false;

        db.DocumentUserPermissions.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> CanViewKnowledgeBaseAsync(Guid knowledgeBaseId, Guid userId, CancellationToken ct)
        => HasKnowledgeBaseRoleAsync(knowledgeBaseId, userId, ["Viewer", "Editor", "Manager"], ct);

    public Task<bool> CanEditKnowledgeBaseAsync(Guid knowledgeBaseId, Guid userId, CancellationToken ct)
        => HasKnowledgeBaseRoleAsync(knowledgeBaseId, userId, ["Editor", "Manager"], ct);

    public Task<bool> CanManageKnowledgeBaseAsync(Guid knowledgeBaseId, Guid userId, CancellationToken ct)
        => HasKnowledgeBaseRoleAsync(knowledgeBaseId, userId, ["Manager"], ct);

    public async Task<bool> CanViewDocumentAsync(Guid documentId, Guid userId, CancellationToken ct)
    {
        var explicitPermission = await db.DocumentUserPermissions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocumentId == documentId && x.UserId == userId, ct);
        if (explicitPermission is not null) return explicitPermission.CanView || explicitPermission.CanEdit || explicitPermission.CanManage;

        var knowledgeBaseId = await GetDocumentKnowledgeBaseIdAsync(documentId, ct);
        return knowledgeBaseId.HasValue && await CanViewKnowledgeBaseAsync(knowledgeBaseId.Value, userId, ct);
    }

    public async Task<bool> CanEditDocumentAsync(Guid documentId, Guid userId, CancellationToken ct)
    {
        var explicitPermission = await db.DocumentUserPermissions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocumentId == documentId && x.UserId == userId, ct);
        if (explicitPermission is not null) return explicitPermission.CanEdit || explicitPermission.CanManage;

        var knowledgeBaseId = await GetDocumentKnowledgeBaseIdAsync(documentId, ct);
        return knowledgeBaseId.HasValue && await CanEditKnowledgeBaseAsync(knowledgeBaseId.Value, userId, ct);
    }

    public async Task<bool> CanManageDocumentAsync(Guid documentId, Guid userId, CancellationToken ct)
    {
        var explicitPermission = await db.DocumentUserPermissions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocumentId == documentId && x.UserId == userId, ct);
        if (explicitPermission is not null) return explicitPermission.CanManage;

        var knowledgeBaseId = await GetDocumentKnowledgeBaseIdAsync(documentId, ct);
        return knowledgeBaseId.HasValue && await CanManageKnowledgeBaseAsync(knowledgeBaseId.Value, userId, ct);
    }

    private Task<bool> HasKnowledgeBaseRoleAsync(Guid knowledgeBaseId, Guid userId, string[] roles, CancellationToken ct)
        => db.KnowledgeBaseMembers.AsNoTracking().AnyAsync(
            x => x.KnowledgeBaseId == knowledgeBaseId && x.UserId == userId && roles.Contains(x.Role), ct);

    private Task<Guid?> GetDocumentKnowledgeBaseIdAsync(Guid documentId, CancellationToken ct)
        => db.Documents.AsNoTracking()
            .Where(x => x.Id == documentId)
            .Select(x => (Guid?)x.KnowledgeBaseId)
            .FirstOrDefaultAsync(ct);

    private static string NormalizeRole(string role)
        => role.Trim().ToLowerInvariant() switch
        {
            "viewer" => "Viewer",
            "editor" => "Editor",
            "manager" => "Manager",
            _ => throw new ArgumentOutOfRangeException(nameof(role), "知识库成员角色仅支持 Viewer、Editor、Manager。")
        };
}
