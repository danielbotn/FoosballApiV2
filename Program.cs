using System.Text;
using AutoMapper;
using FoosballApi;
using FoosballApi.Hub;
using FoosballApi.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables.
DotNetEnv.Env.Load();

// Configure Kestrel for non-development environments
var portVar = Environment.GetEnvironmentVariable("PORT");
if (!builder.Environment.IsDevelopment())
{
    if (int.TryParse(portVar, out var port))
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port);
            Console.WriteLine($"Listening on port {port}");
        });
    }
}

// Add services to the container
var jwtSecret = Environment.GetEnvironmentVariable("JwtSecret");

if (string.IsNullOrEmpty(jwtSecret))
{
    throw new ArgumentNullException("JWTSecret", "JwtSecret is not configured.");
}

var key = Encoding.ASCII.GetBytes(jwtSecret);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
    };

    // Handle SignalR token retrieval from query string
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            // Check if the request is for SignalR Hubs and has a token in the query string
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/messageHub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },

        // Validate token upon receipt
        OnTokenValidated = context =>
        {
            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
            var name = context.Principal.FindFirst("name")?.Value;
            if (name == null || !int.TryParse(name, out int userId))
            {
                context.Fail("Unauthorized");
                return Task.CompletedTask;
            }

            var user = userService.GetUserByIdSync(userId);
            if (user == null)
            {
                context.Fail("Unauthorized");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllers().AddNewtonsoftJson(s =>
{
    s.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
});

string connectionString = builder.Environment.IsDevelopment()
    ? Environment.GetEnvironmentVariable("FoosballDbDev")
    : Environment.GetEnvironmentVariable("FoosballDbProd");

if (string.IsNullOrEmpty(connectionString))
{
    throw new ArgumentNullException("ConnectionString", "Connection string is not configured.");
}

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(connectionString));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICmsService, CmsService>();
builder.Services.AddScoped<IDoubleLeagueGoalService, DoubleLeagueGoalService>();
builder.Services.AddScoped<IDoubleLeaugeMatchService, DoubleLeaugeMatchService>();
builder.Services.AddScoped<IDoubleLeaguePlayerService, DoubleLeaguePlayerService>();
builder.Services.AddScoped<IDoubleLeagueTeamService, DoubleLeagueTeamService>();
builder.Services.AddScoped<IFreehandDoubleGoalService, FreehandDoubleGoalService>();
builder.Services.AddScoped<IFreehandDoubleMatchService, FreehandDoubleMatchService>();
builder.Services.AddScoped<IFreehandMatchService, FreehandMatchService>();
builder.Services.AddScoped<IFreehandGoalService, FreehandGoalService>();
builder.Services.AddScoped<ILeagueService, LeagueService>();
builder.Services.AddScoped<ISingleLeagueMatchService, SingleLeagueMatchService>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();
builder.Services.AddScoped<ISingleLeagueGoalService, SingleLeagueGoalService>();
builder.Services.AddScoped<ISingleLeaguePlayersService, SingleLeaguePlayersService>();
builder.Services.AddScoped<IPremiumService, PremiumService>();
builder.Services.AddScoped<IMicrosoftTeamsService, MicrosoftTeamsService>();
builder.Services.AddScoped<IMatchService, MatchService>();

// Register IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Register MatchesRealtimeService as Singleton
builder.Services.AddSingleton<IMatchesRealtimeService>(provider =>
{
    var hubContext = provider.GetRequiredService<IHubContext<MessageHub>>();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var mapper = provider.GetRequiredService<IMapper>();
    return new MatchesRealtimeService(hubContext, connectionString, httpContextAccessor, mapper);
});

// Register the Background Service
builder.Services.AddHostedService<ScoreNotificationBackgroundService>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Foosball Api", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddScoped<ISlackService, SlackService>();
builder.Services.AddScoped<IDiscordService, DiscordService>();

// Add SignalR support
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireServer();
app.UseHangfireDashboard();

app.MapControllers();

app.MapHub<MessageHub>("/messageHub").RequireAuthorization();

app.UseCors(builder => builder
  .WithOrigins("http://localhost:5173", "http://localhost:8000")
  .AllowAnyMethod()
  .AllowAnyHeader()
  .AllowCredentials());

app.Run();
