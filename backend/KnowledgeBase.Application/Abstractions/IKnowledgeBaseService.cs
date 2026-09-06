using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.KnowledgeBases;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 知识库应用服务契约。
/// </summary>
public interface IKnowledgeBaseService
{
    /// <summary>获取全部知识库，主要供内部管理场景使用。</summary>
    Task<IReadOnlyList<KnowledgeBaseDto>> GetListAsync(CancellationToken cancellationToken);
    /// <summary>按当前用户资源权限分页查询可访问知识库。</summary>
    Task<PageResult<KnowledgeBaseDto>> GetPageAsync(Guid userId, bool isSuperAdmin, PageQuery query, CancellationToken cancellationToken);
    /// <summary>获取单个知识库。</summary>
    Task<KnowledgeBaseDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>创建知识库。</summary>
    Task<KnowledgeBaseDto> CreateAsync(SaveKnowledgeBaseRequest request, CancellationToken cancellationToken);
    /// <summary>更新知识库。</summary>
    Task<KnowledgeBaseDto?> UpdateAsync(Guid id, SaveKnowledgeBaseRequest request, CancellationToken cancellationToken);
    /// <summary>删除知识库。</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
