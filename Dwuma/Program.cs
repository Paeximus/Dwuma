using Dwuma.Models.Data.DwumaContext;
using Dwuma.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Dwuma.Models.Data.DwumaContext;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Dwuma API",
            Version = "v1"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Enter the JWT returned by the login endpoint."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<DwumaContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHttpClient<GeminiService>(
    client =>
    {
        client.Timeout =
            TimeSpan.FromMinutes(3);
    }); builder.Services.AddHttpClient<JobSearchService>();
builder.Services.AddHttpClient<ProfileService>();

builder.Services.AddScoped<SkillsGapService>();
builder.Services.AddScoped<ResumeTailorService>();
builder.Services.AddScoped<InteractionRatingService>();
builder.Services.AddScoped<JobInteractionService>();
builder.Services.AddScoped<InterviewCoachService>();
builder.Services.AddScoped<JobMatchService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();

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

builder.Services.Configure<
    Microsoft.AspNetCore.Http.Features.FormOptions>(
    options =>
    {
        options.MultipartBodyLengthLimit =
            10 * 1024 * 1024;
    });

string jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key is not configured.");

string jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? "DwumaApi";

string jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? "DwumaFrontend";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtKey)),

                ClockSkew =
                    TimeSpan.FromMinutes(1)
            };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (IServiceScope scope =
       app.Services.CreateScope())
{
    DwumaContext context =
        scope.ServiceProvider
            .GetRequiredService<DwumaContext>();

    Console.WriteLine(
        $"ACTIVE DATABASE: {context.Database.GetDbConnection().Database}");

    Console.WriteLine(
        $"ACTIVE SERVER: {context.Database.GetDbConnection().DataSource}");
}

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

app.UseAuthentication();
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