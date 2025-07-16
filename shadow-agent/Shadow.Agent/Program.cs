using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shadow.Agent.Options;
using Shadow.Agent.Parsers;
using Shadow.Agent.Processing;
using Shadow.Agent.Services;
using Shadow.Agent.TaskQueue;

var builder = WebApplication.CreateBuilder(args);

// Регистрируем парсеры
builder.Services.AddSingleton<IResultParser, TrxParser>();
builder.Services.AddSingleton<IResultParser, JUnitParser>();
// Регистрируем процессор
builder.Services.AddSingleton<ResultProcessor>();

builder.Services.AddSingleton<ITaskQueue, TaskQueue>()
            .AddHostedService(provider => (TaskQueue)provider.GetRequiredService<ITaskQueue>());

builder.Services.AddSingleton<TestResultsService>();

builder.Services.AddOptions<AgentOptions>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();

// Для интеграционных тестов
public partial class Program { }
