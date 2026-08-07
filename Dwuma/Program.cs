using Dwuma.Infrastructure;
using Dwuma.Models.Data.DwumaContext;
using Dwuma.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Resend;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

string? port =
    Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls(
        $"http://0.0.0.0:{port}");
}

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

    string xmlFile =
    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    string xmlPath =
        Path.Combine(
            AppContext.BaseDirectory,
            xmlFile);

    options.IncludeXmlComments(
        xmlPath,
        includeControllerXmlComments: true);

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
{
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(
            new Version(8, 0, 36)),
        mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});

var connectionInfo =
    new MySqlConnector.MySqlConnectionStringBuilder(
        connectionString);

Console.WriteLine(
    $"MySQL server: {connectionInfo.Server}");

Console.WriteLine(
    $"MySQL port: {connectionInfo.Port}");

Console.WriteLine(
    $"MySQL database: {connectionInfo.Database}");

Console.WriteLine(
    $"MySQL username: {connectionInfo.UserID}");

Console.WriteLine(
    $"SSL mode: {connectionInfo.SslMode}");

Console.WriteLine(
    $"Password configured: " +
    $"{!string.IsNullOrWhiteSpace(connectionInfo.Password)}");


builder.Services.AddHttpClient<GeminiService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddHttpClient<JobSearchService>(
    client =>
    {
        client.Timeout =
            TimeSpan.FromSeconds(30);

        client.DefaultRequestHeaders
            .UserAgent
            .ParseAdd("Dwuma/1.0");
    });

builder.Services.AddHttpClient<ProfileService>();
builder.Services.AddScoped<SkillsGapService>();
builder.Services.AddScoped<ResumeTailorService>();
builder.Services.AddScoped<InteractionRatingService>();
builder.Services.AddScoped<JobInteractionService>();
builder.Services.AddScoped<InterviewCoachService>();
builder.Services.AddScoped<JobMatchService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

string[] allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "FrontendPolicy",
        policy =>
        {
            if (allowedOrigins.Length == 0)
            {
                policy
                    .WithOrigins(
                        "http://localhost:5173",
                        "http://127.0.0.1:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();

                return;
            }

            policy
                .WithOrigins(allowedOrigins)
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

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter(
        "ai-policy",
        limiterOptions =>
        {
            limiterOptions.PermitLimit = 10;
            limiterOptions.Window =
                TimeSpan.FromMinutes(1);

            limiterOptions.QueueLimit = 0;

            limiterOptions.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;

            limiterOptions.AutoReplenishment = true;
        });

    options.AddFixedWindowLimiter(
        "auth-policy",
        limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window =
                TimeSpan.FromMinutes(1);

            limiterOptions.QueueLimit = 0;

            limiterOptions.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;

            limiterOptions.AutoReplenishment = true;
        });
});


builder.Services.AddHttpClient<JoobleJobService>(
    client =>
    {
        client.Timeout =
            TimeSpan.FromSeconds(30);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Dwuma/1.0");
    });

builder.Services.AddHttpClient<CareerjetJobService>(
    client =>
    {
        client.Timeout =
            TimeSpan.FromSeconds(30);
    });

builder.Services.AddOptions();

builder.Services.AddHttpClient<ResendClient>();

builder.Services.Configure<ResendClientOptions>(
    options =>
    {
        options.ApiToken =
            builder.Configuration["Resend:ApiKey"]
            ?? throw new InvalidOperationException(
                "Resend API key is not configured.");
    });

builder.Services.AddTransient<
    IResend,
    ResendClient>();

builder.Services.Configure<ForwardedHeadersOptions>(
    options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto;

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

var app = builder.Build();

app.UseForwardedHeaders();

app.UseSwagger();
bool enableSwagger =
    app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>(
        "Swagger:Enabled");

if (enableSwagger)
{
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
}

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "Healthy",
        service = "Dwuma API",
        time = DateTime.UtcNow
    }))
    .AllowAnonymous();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("FrontendPolicy");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.MapGet(
    "/",
    () => Results.Redirect("/swagger"))
    .AllowAnonymous();

app.Run();