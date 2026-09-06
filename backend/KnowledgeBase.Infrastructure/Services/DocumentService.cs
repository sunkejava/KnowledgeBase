using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Documents;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>文档读写服务。目录列表与正文分开查询，避免加载全部 Markdown。</summary>
public sealed class DocumentService(KnowledgeDbContext dbContext) : IDocumentService
{
    public async Task<IReadOnlyList<DocumentListItemDto>> GetListAsync(Guid knowledgeBaseId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Documents.AsNoTracking()
            .Where(x => x.KnowledgeBaseId == knowledgeBaseId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new { x.Id, x.ParentId, x.Title, x.Status, x.UpdatedAt })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new DocumentListItemDto(x.Id, x.ParentId, x.Title, x.Status.ToString(), x.UpdatedAt)).ToList();
    }

    public async Task<DocumentDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var doc = await dbContext.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (doc is null) return null;
        var markdown = await dbContext.DocumentContents.AsNoTracking().Where(x => x.DocumentId == id).Select(x => x.Markdown).FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        return Map(doc, markdown);
    }

    public async Task<DocumentDetailDto> CreateAsync(CreateDocumentRequest request, CancellationToken cancellationToken)
    {
        var doc = new Document(request.KnowledgeBaseId, request.Title, request.Slug, request.ParentId);
        dbContext.Documents.Add(doc);
        dbContext.DocumentContents.Add(new DocumentContent(doc.Id, request.Markdown));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(doc, request.Markdown);
    }

    public async Task<DocumentDetailDto?> UpdateAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken)
    {
        var doc = await dbContext.Documents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (doc is null) return null;
        doc.UpdateMetadata(request.Title, request.Slug, request.ParentId);
        var content = await dbContext.DocumentContents.FirstOrDefaultAsync(x => x.DocumentId == id, cancellationToken);
        if (content is null) dbContext.DocumentContents.Add(new DocumentContent(id, request.Markdown)); else content.Update(request.Markdown);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(doc, request.Markdown);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var doc = await dbContext.Documents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (doc is null) return false;
        dbContext.Documents.Remove(doc);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static DocumentDetailDto Map(Document x, string markdown)
        => new(x.Id, x.KnowledgeBaseId, x.ParentId, x.Title, x.Slug, x.Status.ToString(), markdown, x.UpdatedAt);
}
