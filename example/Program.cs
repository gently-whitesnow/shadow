var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapControllers();

app.MapGet("/ping", () =>
{
    return "pong";
});

app.Run();
