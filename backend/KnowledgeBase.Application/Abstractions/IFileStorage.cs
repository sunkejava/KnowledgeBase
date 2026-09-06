namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 文件存储抽象。业务服务只依赖该接口，不直接依赖本地磁盘路径。
/// 后续可以按配置切换 Local、MinIO、S3、OSS、COS 等存储实现。
/// </summary>
public interface IFileStorage
{
    /// <summary>返回当前存储提供方名称。</summary>
    string ProviderName { get; }

    /// <summary>创建用于写入指定对象的流。</summary>
    Task<Stream> CreateWriteAsync(string objectKey, CancellationToken ct);

    /// <summary>打开指定对象的只读流；对象不存在时返回 null。</summary>
    Task<Stream?> OpenReadAsync(string objectKey, CancellationToken ct);

    /// <summary>判断指定对象是否存在。</summary>
    Task<bool> ExistsAsync(string objectKey, CancellationToken ct);

    /// <summary>删除指定对象。对象不存在时不抛异常。</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct);
}
