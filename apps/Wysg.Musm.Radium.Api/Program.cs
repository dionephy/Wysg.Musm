using Wysg.Musm.Radium.Api.Authentication;
using Wysg.Musm.Radium.Api.Repositories;
using Wysg.Musm.Radium.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure OpenAPI (built-in .NET 10)
builder.Services.AddOpenApi();

// Add Firebase Authentication
builder.Services.AddFirebaseAuthentication(builder.Configuration);

// Register database connection factory
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

// Register repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IHotkeyRepository, HotkeyRepository>();
builder.Services.AddScoped<ISnippetRepository, SnippetRepository>();
builder.Services.AddScoped<IPhraseRepository, PhraseRepository>();
builder.Services.AddScoped<ISnomedRepository, SnomedRepository>();
builder.Services.AddScoped<IUserSettingRepository, UserSettingRepository>();
builder.Services.AddScoped<IExportedReportRepository, ExportedReportRepository>();

// Register services
builder.Services.AddScoped<IHotkeyService, HotkeyService>();
builder.Services.AddScoped<ISnippetService, SnippetService>();
builder.Services.AddScoped<IUserSettingService, UserSettingService>();
builder.Services.AddScoped<IExportedReportService, ExportedReportService>();

// Add CORS (configure as needed for production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowRadiumApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowRadiumApp");

// Add Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
