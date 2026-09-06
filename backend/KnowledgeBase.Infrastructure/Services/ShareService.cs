using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>文档分享链接服务。</summary>
public sealed class ShareService(KnowledgeDbContext db) : IShareService
{
    public async Task<ShareLinkDto?> CreateAsync(Guid documentId,Guid? userId,DateTimeOffset? expiresAt,CancellationToken ct)
    {
        if(!await db.Documents.AnyAsync(x=>x.Id==documentId,ct)) return null;
        var entity=new DocumentShareLink(documentId,userId,expiresAt);
        db.DocumentShareLinks.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<IReadOnlyList<ShareLinkDto>> GetDocumentLinksAsync(Guid documentId,CancellationToken ct)
        => await db.DocumentShareLinks.AsNoTracking().Where(x=>x.DocumentId==documentId).OrderByDescending(x=>x.CreatedAt)
            .Select(x=>new ShareLinkDto(x.Id,x.DocumentId,x.Token,x.ExpiresAt,x.Enabled,x.CreatedAt)).ToListAsync(ct);

    public async Task<bool> DisableAsync(Guid id,CancellationToken ct)
    {
        var entity=await db.DocumentShareLinks.FirstOrDefaultAsync(x=>x.Id==id,ct);
        if(entity is null)return false;
        entity.Disable();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SharedDocumentDto?> GetSharedDocumentAsync(string token,CancellationToken ct)
    {
        var link=await db.DocumentShareLinks.AsNoTracking().FirstOrDefaultAsync(x=>x.Token==token&&x.Enabled,ct);
        if(link is null||(link.ExpiresAt.HasValue&&link.ExpiresAt.Value<=DateTimeOffset.UtcNow))return null;
        return await(from d in db.Documents.AsNoTracking() join c in db.DocumentContents.AsNoTracking() on d.Id equals c.DocumentId where d.Id==link.DocumentId select new SharedDocumentDto(d.Id,d.Title,c.Markdown,link.ExpiresAt)).FirstOrDefaultAsync(ct);
    }

    private static ShareLinkDto Map(DocumentShareLink x)=>new(x.Id,x.DocumentId,x.Token,x.ExpiresAt,x.Enabled,x.CreatedAt);
}
