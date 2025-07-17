using System;
using System.IO;
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

builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args);

var externalPath = Path.Combine(AppContext.BaseDirectory, "external_settings.json");
if (File.Exists(externalPath))
{
    builder.Configuration.AddJsonFile(externalPath, optional: false, reloadOnChange: true);
}
else
{
    Console.WriteLine("File external_settings.json not found");
}

// Парсеры
builder.Services.AddSingleton<IResultParser, TrxParser>();
builder.Services.AddSingleton<IResultParser, JUnitParser>();

// Сервисы
builder.Services.AddSingleton<ResultProcessor>();
builder.Services.AddSingleton<TestResultsService>();
builder.Services.AddSingleton<ScopesService>();
builder.Services.AddSingleton<ResultConsumerProvider>();

builder.Services.AddSingleton<ITaskQueue, TaskQueue>()
            .AddHostedService(provider => (TaskQueue)provider.GetRequiredService<ITaskQueue>());

// клиенты
builder.Services.AddNotifications();
builder.Services.AddSingleton<IScopesDbClient, ScopesDbClient>();
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Опции
builder.Services.AddOptions<AgentOptions>().BindConfiguration(nameof(AgentOptions));
builder.Services.AddOptions<DefaultOptions>().BindConfiguration(nameof(DefaultOptions));

// Конфигурация
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));
app.UseSwagger();
app.UseSwaggerUI();

app.Run();



// Для интеграционных тестов 
public partial class Program { }
