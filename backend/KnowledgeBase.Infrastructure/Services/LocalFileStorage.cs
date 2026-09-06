using KnowledgeBase.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 本地文件系统存储实现。所有对象均存放在配置的根目录下，并通过对象 Key 进行访问。
/// </summary>
public sealed class LocalFileStorage(IConfiguration configuration) : IFileStorage
{
    private readonly string _root = ResolveRoot(configuration);

    public string ProviderName => "local";

    /// <inheritdoc />
    public Task<Stream> CreateWriteAsync(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var fullPath = ResolvePath(objectKey);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        Stream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public Task<Stream?> OpenReadAsync(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var fullPath = ResolvePath(objectKey);
        if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(ResolvePath(objectKey)));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var fullPath = ResolvePath(objectKey);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private string ResolvePath(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey)) throw new ArgumentException("对象 Key 不能为空。", nameof(objectKey));
        var normalized = objectKey.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains("../", StringComparison.Ordinal) || normalized.Contains("..\\", StringComparison.Ordinal))
            throw new InvalidOperationException("对象 Key 不允许包含上级目录跳转。 ");

        var fullPath = Path.GetFullPath(Path.Combine(_root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("对象路径超出存储根目录。 ");
        return fullPath;
    }

    private static string ResolveRoot(IConfiguration configuration)
    {
        var configured = configuration["Storage:Local:Root"];
        var value = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "storage")
            : Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured);
        var root = Path.GetFullPath(value);
        Directory.CreateDirectory(root);
        return root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
    }
}
