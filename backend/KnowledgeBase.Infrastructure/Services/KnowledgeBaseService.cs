using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.KnowledgeBases;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>知识库应用服务实现。</summary>
public sealed class KnowledgeBaseService(KnowledgeDbContext dbContext) : IKnowledgeBaseService
{
    public async Task<IReadOnlyList<KnowledgeBaseDto>> GetListAsync(CancellationToken cancellationToken)
        => await dbContext.KnowledgeBases.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new KnowledgeBaseDto(x.Id, x.Name, x.Description, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<KnowledgeBaseDto?> GetAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.KnowledgeBases.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new KnowledgeBaseDto(x.Id, x.Name, x.Description, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<KnowledgeBaseDto> CreateAsync(SaveKnowledgeBaseRequest request, CancellationToken cancellationToken)
    {
        var entity = new KnowledgeBaseSpace(request.Name, request.Description);
        dbContext.KnowledgeBases.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<KnowledgeBaseDto?> UpdateAsync(Guid id, SaveKnowledgeBaseRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.KnowledgeBases.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        entity.Update(request.Name, request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.KnowledgeBases.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;
        dbContext.KnowledgeBases.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static KnowledgeBaseDto Map(KnowledgeBaseSpace x) => new(x.Id, x.Name, x.Description, x.CreatedAt, x.UpdatedAt);
}
