using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;

namespace KnowledgeBase.Api.Middleware;

/// <summary>
/// 对修改型 HTTP 请求统一记录审计日志。业务服务仍可补充更细粒度的领域审计。
/// </summary>
public sealed class AuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, KnowledgeDbContext dbContext)
    {
        var method = context.Request.Method;
        var shouldAudit = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
        if (!shouldAudit)
        {
            await next(context);
            return;
        }

        var success = true;
        string? message = null;
        try
        {
            await next(context);
            success = context.Response.StatusCode < 400;
            if (!success) message = $"HTTP {context.Response.StatusCode}";
        }
        catch (Exception ex)
        {
            success = false;
            message = ex.Message.Length > 900 ? ex.Message[..900] : ex.Message;
            throw;
        }
        finally
        {
            // 审计写入失败不能影响主业务响应。
            try
            {
                var userName = context.User.Identity?.IsAuthenticated == true
                    ? context.User.Identity.Name ?? "authenticated"
                    : "anonymous";
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                dbContext.AuditLogs.Add(new AuditLog(
                    context.Request.Path.StartsWithSegments("/api/auth") ? "Login" : "Operation",
                    method,
                    userName,
                    context.Request.Path,
                    ip,
                    success,
                    message));
                await dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch
            {
                // 后续接入 Serilog 时在这里记录审计持久化异常。
            }
        }
    }
}
