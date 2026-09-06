using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

public sealed class AccessControlService(KnowledgeDbContext db) : IAccessControlService
{
    public async Task<IReadOnlyList<KnowledgeBaseMemberDto>> GetKnowledgeBaseMembersAsync(Guid knowledgeBaseId,CancellationToken ct)
        => await(from m in db.KnowledgeBaseMembers.AsNoTracking() join u in db.Users.AsNoTracking() on m.UserId equals u.Id where m.KnowledgeBaseId==knowledgeBaseId orderby u.DisplayName select new KnowledgeBaseMemberDto(m.KnowledgeBaseId,m.UserId,u.UserName,u.DisplayName,m.Role,m.CreatedAt)).ToListAsync(ct);

    public async Task<KnowledgeBaseMemberDto> SetKnowledgeBaseMemberAsync(Guid knowledgeBaseId,SetKnowledgeBaseMemberRequest request,CancellationToken ct)
    {
        var user=await db.Users.AsNoTracking().FirstAsync(x=>x.Id==request.UserId,ct);
        var entity=await db.KnowledgeBaseMembers.FirstOrDefaultAsync(x=>x.KnowledgeBaseId==knowledgeBaseId&&x.UserId==request.UserId,ct);
        if(entity is null){entity=new KnowledgeBaseMember(knowledgeBaseId,request.UserId,request.Role);db.KnowledgeBaseMembers.Add(entity);}else entity.SetRole(request.Role);
        await db.SaveChangesAsync(ct);
        return new(entity.KnowledgeBaseId,entity.UserId,user.UserName,user.DisplayName,entity.Role,entity.CreatedAt);
    }

    public async Task<bool> RemoveKnowledgeBaseMemberAsync(Guid knowledgeBaseId,Guid userId,CancellationToken ct)
    {var entity=await db.KnowledgeBaseMembers.FirstOrDefaultAsync(x=>x.KnowledgeBaseId==knowledgeBaseId&&x.UserId==userId,ct);if(entity is null)return false;db.KnowledgeBaseMembers.Remove(entity);await db.SaveChangesAsync(ct);return true;}

    public async Task<IReadOnlyList<DocumentPermissionDto>> GetDocumentPermissionsAsync(Guid documentId,CancellationToken ct)
        => await(from p in db.DocumentUserPermissions.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where p.DocumentId==documentId orderby u.DisplayName select new DocumentPermissionDto(p.DocumentId,p.UserId,u.UserName,u.DisplayName,p.CanView,p.CanEdit,p.CanManage)).ToListAsync(ct);

    public async Task<DocumentPermissionDto> SetDocumentPermissionAsync(Guid documentId,SetDocumentPermissionRequest request,CancellationToken ct)
    {
        var user=await db.Users.AsNoTracking().FirstAsync(x=>x.Id==request.UserId,ct);
        var entity=await db.DocumentUserPermissions.FirstOrDefaultAsync(x=>x.DocumentId==documentId&&x.UserId==request.UserId,ct);
        if(entity is null){entity=new DocumentUserPermission(documentId,request.UserId,request.CanView,request.CanEdit,request.CanManage);db.DocumentUserPermissions.Add(entity);}else entity.Update(request.CanView,request.CanEdit,request.CanManage);
        await db.SaveChangesAsync(ct);
        return new(entity.DocumentId,entity.UserId,user.UserName,user.DisplayName,entity.CanView,entity.CanEdit,entity.CanManage);
    }

    public async Task<bool> RemoveDocumentPermissionAsync(Guid documentId,Guid userId,CancellationToken ct)
    {var entity=await db.DocumentUserPermissions.FirstOrDefaultAsync(x=>x.DocumentId==documentId&&x.UserId==userId,ct);if(entity is null)return false;db.DocumentUserPermissions.Remove(entity);await db.SaveChangesAsync(ct);return true;}
}
