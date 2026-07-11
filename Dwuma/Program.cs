using Dwuma.Models.Data.DwumaContext;
using Dwuma.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dwuma API",
        Version = "v1",
        Description =
            "Career assistant API for profile management, " +
            "skills-gap analysis, CV tailoring, interview preparation, " +
            "and job discovery.",
        Contact = new OpenApiContact
        {
            Name = "Dwuma Team"
        }
    });

    string xmlFile =
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    string xmlPath =
        Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<DwumaContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddHttpClient<JobSearchService>();
builder.Services.AddHttpClient<ProfileService>();

builder.Services.AddScoped<SkillsGapService>();
builder.Services.AddScoped<ResumeTailorService>();
builder.Services.AddScoped<InteractionRatingService>();
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
                "http://localhost:5500")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Dwuma API");

    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Dwuma API";
    options.DefaultModelsExpandDepth(2);
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