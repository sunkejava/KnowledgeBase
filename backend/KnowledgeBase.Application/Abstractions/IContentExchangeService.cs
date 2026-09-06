using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>Markdown、HTML、DOCX、ZIP 导入导出与版本差异服务。</summary>
public interface IContentExchangeService
{
    /// <summary>导出单篇 Markdown 文档。</summary>
    Task<ExportDocumentDto?> ExportMarkdownAsync(Guid documentId, CancellationToken ct);

    /// <summary>导入单篇 Markdown 文档。</summary>
    Task<ImportMarkdownResultDto> ImportMarkdownAsync(Guid knowledgeBaseId, Guid? parentId, string fileName, string markdown, CancellationToken ct);

    /// <summary>导入单篇 HTML 文档并转换为 Markdown。</summary>
    Task<ImportMarkdownResultDto> ImportHtmlAsync(Guid knowledgeBaseId, Guid? parentId, string fileName, string html, CancellationToken ct);

    /// <summary>导入 DOCX 文档，并提取段落、标题层级后转换为 Markdown。</summary>
    Task<ImportMarkdownResultDto> ImportDocxAsync(Guid knowledgeBaseId, Guid? parentId, string fileName, Stream docxStream, CancellationToken ct);

    /// <summary>
    /// 导入 Markdown ZIP。ZIP 目录会按文档路径恢复父子关系；进度回调可用于异步导入任务。
    /// </summary>
    Task<ImportZipResultDto> ImportMarkdownZipAsync(
        Guid knowledgeBaseId,
        Stream zipStream,
        Func<int, int, int, int, CancellationToken, Task>? progress,
        CancellationToken ct);

    /// <summary>对比历史版本与当前正文。</summary>
    Task<DocumentDiffDto?> DiffVersionToCurrentAsync(Guid versionId, CancellationToken ct);
}
