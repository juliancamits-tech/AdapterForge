using AdapterForge.Http.Generated;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


builder.Services.AddMcpServer()
  .WithHttpTransport()
  .WithToolsFromAssembly();

builder.Services.AddOpenApi();
builder.Services.AddAdapterForgeEndpoints();

var app = builder.Build();
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapScalarApiReference();

app.MapOpenApi();
app.MapControllers();


app.MapMcp("/mcp");
app.MapAdapterForgeEndpoints();

app.Run();
