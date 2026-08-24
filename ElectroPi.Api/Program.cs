using AutoMapper;
using ElectroPi.Api.Extentions;
using ElectroPi.Api.Middlewares;
using ElectroPi.Application.MappingProfiles;
using ElectroPi.Domain.Entities;
using ElectroPi.Domain.Options;
using ElectroPi.Infrastructure.Persistance;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

JwtOptions jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new Exception("Error in JWT Settings");

builder.Services.AddSingleton<JwtOptions>(jwtOptions);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier,
        ValidateLifetime = true,

    };
});

builder.Services.AddApplicationServices();

builder.Services.AddAutoMapper(cfg =>
{
    var assembly = typeof(AuthProfile).Assembly;
    var profileTypes = assembly.GetTypes()
        .Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract);

    foreach (var profileType in profileTypes)
    {
        cfg.AddProfile(profileType);
    }
});

Log.Logger = new LoggerConfiguration()
.MinimumLevel.Information()
.MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
.MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
.WriteTo.Console()
.WriteTo.File("logs/log-.txt",
    rollingInterval: RollingInterval.Day,
    fileSizeLimitBytes: 20_000_000,
    retainedFileCountLimit: 7,
    rollOnFileSizeLimit: true)
.CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddCors(options =>
{
    options.AddPolicy("prod", policy =>
    {
        policy.WithOrigins(
            "https://abdofathy.cloud"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });

    options.AddPolicy("dev", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});


builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("fixed", httpContext =>

        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }
        )
    );
});

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(
        JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("dev");
}
else
{
    app.UseCors("prod");
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await AuthSeeder.SeedAsync(services);
    await DbSeeder.SeedAsync(services);
}

app.Run();
