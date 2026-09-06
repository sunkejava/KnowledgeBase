using KnowledgeBase.Contracts.Knowledge;
namespace KnowledgeBase.Application.Abstractions;
public interface IContentExchangeService
{
    Task<ExportDocumentDto?> ExportMarkdownAsync(Guid documentId,CancellationToken ct);
    Task<ImportMarkdownResultDto> ImportMarkdownAsync(Guid knowledgeBaseId,Guid? parentId,string fileName,string markdown,CancellationToken ct);
    Task<DocumentDiffDto?> DiffVersionToCurrentAsync(Guid versionId,CancellationToken ct);
}
