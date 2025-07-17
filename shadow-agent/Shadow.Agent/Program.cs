using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shadow.Agent.DA.Postgres;
using Shadow.Agent.Extensions.cs;
using Shadow.Agent.Options;
using Shadow.Agent.Parsers;
using Shadow.Agent.Processing;
using Shadow.Agent.Services;
using Shadow.Agent.TaskQueue;

var builder = WebApplication.CreateBuilder(args);

// Парсеры
builder.Services.AddSingleton<IResultParser, TrxParser>();
builder.Services.AddSingleton<IResultParser, JUnitParser>();

// Сервисы
builder.Services.AddSingleton<ResultProcessor>();
builder.Services.AddSingleton<TestResultsService>();

builder.Services.AddSingleton<ITaskQueue, TaskQueue>()
            .AddHostedService(provider => (TaskQueue)provider.GetRequiredService<ITaskQueue>());

// клиенты
builder.Services.AddNotifications();
builder.Services.AddSingleton<ScopesDbClient>();

// Опции
builder.Services.AddOptions<AgentOptions>().BindConfiguration(nameof(AgentOptions));
builder.Services.AddOptions<DefaultOptions>().BindConfiguration(nameof(DefaultOptions));

// Конфигурация
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();


// Для интеграционных тестов 
public partial class Program { }
