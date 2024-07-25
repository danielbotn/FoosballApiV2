using System.Text;
using FoosballApi;
using FoosballApi.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

// Why was this added
// https does not work when this is added in development
var portVar = Environment.GetEnvironmentVariable("PORT");

if (!builder.Environment.IsDevelopment())
{
    if (portVar is {Length: >0} && int.TryParse(portVar, out int port))
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(5297);
            options.ListenAnyIP(7145);
            options.ListenAnyIP(52729);
            options.ListenAnyIP(port);
        });
    }
}

// Add services to the container.
var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWTSecret"));
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
            var name = context.Principal.FindFirst("name").Value;
            var userId = int.Parse(name);
            var user = userService.GetUserByIdSync(userId);
            if (user == null)
            {
                // return unauthorized if user no longer exists
                context.Fail("Unauthorized");
            }
            return Task.CompletedTask;
        }
    };
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        // ClockSkew = TimeSpan.Zero // for debugging only to remove 5 min default time
    };
});
builder.Services.AddControllers().AddNewtonsoftJson(s =>
            {
                s.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

string _connectionString = "";
#if DEBUG
    _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
#else
    _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
#endif

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(_connectionString)));


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


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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

app.UseCors(builder => builder
  .WithOrigins("http://localhost:5173")
  .WithOrigins("http://localhost:8000")
  .AllowAnyMethod()
  .AllowAnyHeader()
  .AllowCredentials());

app.Run();
