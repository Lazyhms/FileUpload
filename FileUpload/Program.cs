using FileUpload;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
    options.Limits.MaxConcurrentConnections = 5000;
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
    options.Limits.MaxResponseBufferSize = long.MaxValue;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("corsPolicy",
       builder => builder.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
});

builder.Services.AddHealthChecks();
builder.Services.AddControllers();

builder.Services.AddDbContext<FileDbContext>(options =>
{
    options.UseSqlite("Data Source=file.db");
});

var app = builder.Build();

app.UseCors("corsPolicy");

app.UseAuthorization();

app.MapHealthChecks("ping");

app.MapControllers();

app.Run();