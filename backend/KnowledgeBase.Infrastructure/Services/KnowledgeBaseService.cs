using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.KnowledgeBases;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Common;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 知识库应用服务实现。
/// </summary>
public sealed class KnowledgeBaseService(KnowledgeDbContext dbContext) : IKnowledgeBaseService
{
    /// <summary>
    /// 获取全部知识库，主要供内部管理和兼容场景使用。
    /// SQLite 不支持直接按 DateTimeOffset 排序，因此先投影再使用统一安全排序扩展。
    /// </summary>
    public Task<IReadOnlyList<KnowledgeBaseDto>> GetListAsync(CancellationToken cancellationToken)
        => dbContext.KnowledgeBases.AsNoTracking()
            .Select(x => new KnowledgeBaseDto(x.Id, x.Name, x.Description, x.CreatedAt, x.UpdatedAt))
            .ToSqliteSafeDateTimeOffsetListAsync(x => x.UpdatedAt, descending: true, cancellationToken);

    /// <summary>
    /// 根据当前用户成员关系分页查询可访问知识库；超级管理员可以访问全部知识库。
    /// </summary>
    public Task<PageResult<KnowledgeBaseDto>> GetPageAsync(
        Guid userId,
        bool isSuperAdmin,
        PageQuery query,
        CancellationToken cancellationToken)
    {
        var source = dbContext.KnowledgeBases.AsNoTracking().AsQueryable();

        if (!isSuperAdmin)
        {
            source = source.Where(space => dbContext.KnowledgeBaseMembers.Any(member =>
                member.KnowledgeBaseId == space.Id && member.UserId == userId));
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.Name.Contains(keyword) || x.Description.Contains(keyword));
        }

        return source
            .Select(x => new KnowledgeBaseDto(x.Id, x.Name, x.Description, x.CreatedAt, x.UpdatedAt))
            .ToSqliteSafeDateTimeOffsetPageAsync(
                x => x.UpdatedAt,
                descending: true,
                query.NormalizedPage,
                query.NormalizedPageSize,
                cancellationToken);
    }

    /// <summary>获取单个知识库。</summary>
    public async Task<KnowledgeBaseDto?> GetAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.KnowledgeBases.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new KnowledgeBaseDto(x.Id, x.Name, x.Description, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>创建知识库。</summary>
    public async Task<KnowledgeBaseDto> CreateAsync(SaveKnowledgeBaseRequest request, CancellationToken cancellationToken)
    {
        var entity = new KnowledgeBaseSpace(request.Name, request.Description);
        dbContext.KnowledgeBases.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    /// <summary>更新知识库。</summary>
    public async Task<KnowledgeBaseDto?> UpdateAsync(Guid id, SaveKnowledgeBaseRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.KnowledgeBases.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;

        entity.Update(request.Name, request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    /// <summary>删除知识库。</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.KnowledgeBases.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        dbContext.KnowledgeBases.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static KnowledgeBaseDto Map(KnowledgeBaseSpace x)
        => new(x.Id, x.Name, x.Description, x.CreatedAt, x.UpdatedAt);
}
