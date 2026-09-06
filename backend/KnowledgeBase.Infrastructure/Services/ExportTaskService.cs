using System.IO.Compression;
using System.Text;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 知识库服务端导出任务实现。导出文件统一写入 storage/exports。
/// </summary>
public sealed class ExportTaskService(KnowledgeDbContext db) : IExportTaskService
{
    private static readonly string Root=Path.Combine(AppContext.BaseDirectory,"storage","exports");

    public async Task<ExportTaskDto> CreateAsync(Guid userId,Guid knowledgeBaseId,string format,CancellationToken ct)
    {
        var name=await db.KnowledgeBases.AsNoTracking().Where(x=>x.Id==knowledgeBaseId).Select(x=>x.Name).FirstAsync(ct);
        var normalized=string.IsNullOrWhiteSpace(format)?"zip":format.Trim().ToLowerInvariant();
        if(normalized!="zip")throw new ArgumentOutOfRangeException(nameof(format),"当前仅支持 ZIP 导出。") ;
        var entity=new ExportTask(userId,knowledgeBaseId,$"{name} - Markdown ZIP",normalized);
        db.Set<ExportTask>().Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<PageResult<ExportTaskDto>> GetPageAsync(Guid userId,int page,int pageSize,CancellationToken ct)
    {
        page=Math.Max(page,1);pageSize=Math.Clamp(pageSize,1,100);
        var query=db.Set<ExportTask>().AsNoTracking().Where(x=>x.UserId==userId);
        var total=await query.LongCountAsync(ct);
        var items=await query.OrderByDescending(x=>x.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new ExportTaskDto(x.Id,x.KnowledgeBaseId,x.Name,x.Format,x.Status,x.FileName,x.ErrorMessage,x.CreatedAt,x.StartedAt,x.CompletedAt)).ToListAsync(ct);
        return new(items,total,page,pageSize);
    }

    public async Task<(string Path,string FileName)?> GetFileAsync(Guid taskId,Guid userId,bool isSuperAdmin,CancellationToken ct)
    {
        var row=await db.Set<ExportTask>().AsNoTracking().FirstOrDefaultAsync(x=>x.Id==taskId&&(isSuperAdmin||x.UserId==userId),ct);
        if(row is null||row.Status!="Completed"||string.IsNullOrWhiteSpace(row.RelativePath)||string.IsNullOrWhiteSpace(row.FileName))return null;
        return(Path.Combine(AppContext.BaseDirectory,row.RelativePath),row.FileName);
    }

    public async Task ProcessNextPendingAsync(CancellationToken ct)
    {
        var task=await db.Set<ExportTask>().OrderBy(x=>x.CreatedAt).FirstOrDefaultAsync(x=>x.Status=="Pending",ct);
        if(task is null)return;
        task.Start();
        await db.SaveChangesAsync(ct);
        try
        {
            Directory.CreateDirectory(Root);
            var kbName=await db.KnowledgeBases.AsNoTracking().Where(x=>x.Id==task.KnowledgeBaseId).Select(x=>x.Name).FirstOrDefaultAsync(ct)??"knowledge-base";
            var rows=await(from d in db.Documents.AsNoTracking() join c in db.DocumentContents.AsNoTracking() on d.Id equals c.DocumentId where d.KnowledgeBaseId==task.KnowledgeBaseId orderby d.Title select new{d.Id,d.Title,c.Markdown}).ToListAsync(ct);
            var fileName=$"{Sanitize(kbName)}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip";
            var stored=$"{task.Id:N}.zip";
            var full=Path.Combine(Root,stored);
            await using(var stream=File.Create(full))
            using(var archive=new ZipArchive(stream,ZipArchiveMode.Create,false,Encoding.UTF8))
            {
                foreach(var row in rows)
                {
                    var entry=archive.CreateEntry($"{Sanitize(row.Title)}-{row.Id:N}.md",CompressionLevel.Fastest);
                    await using var entryStream=entry.Open();
                    await using var writer=new StreamWriter(entryStream,new UTF8Encoding(false));
                    await writer.WriteAsync(row.Markdown.AsMemory(),ct);
                }
            }
            task.Complete(fileName,Path.Combine("storage","exports",stored));
        }
        catch(Exception ex)
        {
            task.Fail(ex.Message);
        }
        await db.SaveChangesAsync(ct);
    }

    private static ExportTaskDto Map(ExportTask x)=>new(x.Id,x.KnowledgeBaseId,x.Name,x.Format,x.Status,x.FileName,x.ErrorMessage,x.CreatedAt,x.StartedAt,x.CompletedAt);
    private static string Sanitize(string name){foreach(var c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');return string.IsNullOrWhiteSpace(name)?"knowledge-base":name;}
}
