using KnowledgeBase.Contracts.KnowledgeBases;

namespace KnowledgeBase.Application.Abstractions;

public interface IKnowledgeBaseService
{
    Task<IReadOnlyList<KnowledgeBaseDto>> GetListAsync(CancellationToken cancellationToken);
    Task<KnowledgeBaseDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<KnowledgeBaseDto> CreateAsync(SaveKnowledgeBaseRequest request, CancellationToken cancellationToken);
    Task<KnowledgeBaseDto?> UpdateAsync(Guid id, SaveKnowledgeBaseRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
