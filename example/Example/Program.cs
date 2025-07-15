using Example.BO;
using Example.DA.DbClients;
using Example.DA.HttpClients;
using Example.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IEntityPostgresDbClient, EntityPostgresDbClient>();
builder.Services.AddSingleton<IAnotherServiceHttpClient, AnotherServiceHttpClient>();
builder.Services.AddSingleton<ExampleService>();
builder.Services.AddOptions<PostgresOptions>().BindConfiguration(nameof(PostgresOptions));

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MapGet("/ping", () =>
{
    return "pong";
});

app.Run();

// Делаем класс Program доступным для интеграционных тестов
public partial class Program { }
