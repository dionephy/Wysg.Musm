using Wysg.Musm.GhostApi.Orchestration;
using Wysg.Musm.GhostApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(o => o.AddPolicy("any", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddSingleton<IRuleEngine, RuleEngineService>(); // <-- renamed
builder.Services.AddSingleton<LlmClient>();
builder.Services.AddSingleton<Ranker>();
builder.Services.AddSingleton<SuggestionOrchestrator>();

var app = builder.Build();

app.UseCors("any");
app.MapControllers();

app.Run();

//dotnet run --project apps/Wysg.Musm.GhostApi/Wysg.Musm.GhostApi.csproj
