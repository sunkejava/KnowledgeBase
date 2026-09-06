using KnowledgeBase.Contracts.Documents;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>文档应用服务契约。</summary>
public interface IDocumentService
{
    Task<IReadOnlyList<DocumentListItemDto>> GetListAsync(Guid? knowledgeBaseId, CancellationToken cancellationToken);
    Task<DocumentDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<DocumentDetailDto> CreateAsync(CreateDocumentRequest request, CancellationToken cancellationToken);
}
