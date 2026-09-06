using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>Markdown 导入导出与版本 Diff 服务。</summary>
public sealed class ContentExchangeService(KnowledgeDbContext db, IDocumentService documents) : IContentExchangeService
{
    public async Task<ExportDocumentDto?> ExportMarkdownAsync(Guid documentId,CancellationToken ct)
    {
        var row=await (from d in db.Documents.AsNoTracking() join c in db.DocumentContents.AsNoTracking() on d.Id equals c.DocumentId where d.Id==documentId select new{d.Id,d.Title,d.Slug,c.Markdown}).FirstOrDefaultAsync(ct);
        return row is null?null:new(row.Id,row.Title,row.Slug,row.Markdown,$"{Sanitize(row.Title)}.md");
    }

    public async Task<ImportMarkdownResultDto> ImportMarkdownAsync(Guid knowledgeBaseId,Guid? parentId,string fileName,string markdown,CancellationToken ct)
    {
        var title=Path.GetFileNameWithoutExtension(fileName).Trim();
        if(string.IsNullOrWhiteSpace(title)) title="导入文档";
        var slug=$"import-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..50];
        var created=await documents.CreateAsync(new(knowledgeBaseId,parentId,title,slug,markdown),ct);
        return new(created.Id,created.Title,created.Slug);
    }

    public async Task<DocumentDiffDto?> DiffVersionToCurrentAsync(Guid versionId,CancellationToken ct)
    {
        var version=await db.DocumentVersions.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==versionId,ct);
        if(version is null) return null;
        var current=await db.DocumentContents.AsNoTracking().Where(x=>x.DocumentId==version.DocumentId).Select(x=>x.Markdown).FirstOrDefaultAsync(ct)??string.Empty;
        return new(version.DocumentId,version.VersionNumber,BuildDiff(version.Markdown,current));
    }

    private static IReadOnlyList<DiffLineDto> BuildDiff(string oldText,string newText)
    {
        var a=oldText.Replace("\r","").Split('\n').Take(2000).ToArray();
        var b=newText.Replace("\r","").Split('\n').Take(2000).ToArray();
        var n=a.Length;var m=b.Length;var dp=new int[n+1,m+1];
        for(var i=n-1;i>=0;i--)for(var j=m-1;j>=0;j--)dp[i,j]=a[i]==b[j]?dp[i+1,j+1]+1:Math.Max(dp[i+1,j],dp[i,j+1]);
        var result=new List<DiffLineDto>();var x=0;var y=0;var oldLine=1;var newLine=1;
        while(x<n||y<m)
        {
            if(x<n&&y<m&&a[x]==b[y]){result.Add(new("equal",oldLine++,newLine++,a[x]));x++;y++;}
            else if(y<m&&(x==n||dp[x,y+1]>=dp[x+1,y])){result.Add(new("add",0,newLine++,b[y++]));}
            else if(x<n){result.Add(new("delete",oldLine++,0,a[x++]));}
        }
        return result;
    }

    private static string Sanitize(string name)
    {
        foreach(var c in Path.GetInvalidFileNameChars()) name=name.Replace(c,'_');
        return string.IsNullOrWhiteSpace(name)?"document":name;
    }
}
