using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<KnowledgeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=knowledgebase.db"));
builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
    policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

var app = builder.Build();

app.UseCors("frontend");
app.MapOpenApi();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "KnowledgeBase.Api",
    time = DateTimeOffset.UtcNow
}));

app.Run();
