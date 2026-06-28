using ChessCoach.Api.Configuration;
using ChessCoach.Api.Data;
using ChessCoach.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddDbContext<ChessCoachDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddOpenApi();

builder.Services.Configure<LlmSettings>(builder.Configuration.GetSection("LlmSettings"));

// Register engine pool manager as singleton
var stockfishPath = builder.Configuration.GetValue<string>("StockfishPath") ?? "stockfish";
builder.Services.AddSingleton<IEnginePoolManager>(sp => 
    new EnginePoolManager(stockfishPath, poolSize: 2));
    
builder.Services.AddTransient<DualStageEvaluator>();
builder.Services.AddHttpClient<ILlmClient, ConfigurableLlmClient>(client =>
{
    // Give Ollama plenty of time to load large models (like 12b) into memory on the first request
    client.Timeout = TimeSpan.FromMinutes(10);
});
builder.Services.AddTransient<ActiveCoachingPipeline>();

// Allow CORS for the local frontend
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Auto-create SQLite database if it doesn't exist (useful when sharing the project)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ChessCoachDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
