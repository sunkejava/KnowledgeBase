using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>Markdown、ZIP 导入导出与版本差异服务。</summary>
public interface IContentExchangeService
{
    Task<ExportDocumentDto?> ExportMarkdownAsync(Guid documentId,CancellationToken ct);
    Task<ImportMarkdownResultDto> ImportMarkdownAsync(Guid knowledgeBaseId,Guid? parentId,string fileName,string markdown,CancellationToken ct);
    Task<ImportZipResultDto> ImportMarkdownZipAsync(Guid knowledgeBaseId,Stream zipStream,CancellationToken ct);
    Task<DocumentDiffDto?> DiffVersionToCurrentAsync(Guid versionId,CancellationToken ct);
}
