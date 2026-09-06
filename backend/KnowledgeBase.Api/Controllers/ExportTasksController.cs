using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>服务端导出任务中心接口。</summary>
[ApiController]
[Authorize]
[Route("api/export-tasks")]
public sealed class ExportTasksController(IExportTaskService service,IAccessControlService accessControl):ControllerBase
{
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>创建知识库异步导出任务。</summary>
    [HttpPost]
    public async Task<ActionResult<ExportTaskDto>> Create(CreateExportTaskRequest request,CancellationToken ct)
    {
        if(!User.IsInRole("SUPER_ADMIN")&&!await accessControl.CanViewKnowledgeBaseAsync(request.KnowledgeBaseId,UserId,ct))return Forbid();
        return Ok(await service.CreateAsync(UserId,request.KnowledgeBaseId,request.Format,ct));
    }

    /// <summary>分页获取当前用户的导出任务。</summary>
    [HttpGet]
    public Task<PageResult<ExportTaskDto>> Page([FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)
        =>service.GetPageAsync(UserId,page,pageSize,ct);

    /// <summary>下载已完成的导出文件。</summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id,CancellationToken ct)
    {
        var file=await service.GetFileAsync(id,UserId,User.IsInRole("SUPER_ADMIN"),ct);
        if(file is null||!System.IO.File.Exists(file.Value.Path))return NotFound();
        return PhysicalFile(file.Value.Path,"application/zip",file.Value.FileName);
    }
}
