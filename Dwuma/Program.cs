using CareerAgent.Training;
using Dwuma.ML;
using Dwuma.Models.Data.DwumaContext;
using Dwuma.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Dwuma API",
        Version     = "v1",
        Description = "Career assistant API: profile management, skills-gap analysis, and CV tailoring.",
        Contact     = new OpenApiContact { Name = "Dwuma Team" }
    });

    // Include XML doc comments in Swagger UI (summaries on actions & models)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});


builder.Services.AddDbContext<DwumaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<SkillsGapService>();
builder.Services.AddHttpClient<ProfileService>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<ResumeTailorService>();
builder.Services.AddScoped<InteractionRatingService>();
builder.Services.AddScoped<JobRankingService>();
builder.Services.AddScoped<JobInteractionService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://localhost:7063",
                "http://localhost:5063",
                "http://localhost:3000",
                "http://127.0.0.1:5500",
                "http://localhost:5500"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

string jobRankerPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "MLModels",
    "JobRanker.zip");

if (!File.Exists(jobRankerPath))
{
    throw new FileNotFoundException(
        $"Job-ranking model was not found at: {jobRankerPath}");
}

builder.Services
    .AddPredictionEnginePool<JobInteractionTrainingRow, JobPrediction>()
    .FromFile(
        modelName: "JobRanker",
        filePath: jobRankerPath,
        watchForChanges: true);

builder.Services.AddScoped<JobRankingService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dwuma API");
    options.RoutePrefix = "swagger"; // UI at /swagger
    options.DocumentTitle = "Dwuma API";
    options.DefaultModelsExpandDepth(2);
    options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
    options.DisplayRequestDuration();
    options.EnableFilter();
});

app.UseCors("FrontendPolicy");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger");
        return;
    }
    await next();
});


app.Run();
