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
builder.Services.AddSingleton<IEnginePoolManager>(sp => 
    new EnginePoolManager(@"F:\myInstalls\Stockfish17\stockfish-windows-x86-64-avx2.exe", poolSize: 2));
    
builder.Services.AddTransient<DualStageEvaluator>();
builder.Services.AddHttpClient<ILlmClient, ConfigurableLlmClient>();
builder.Services.AddTransient<ActiveCoachingPipeline>();

// Allow CORS for the local frontend
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
