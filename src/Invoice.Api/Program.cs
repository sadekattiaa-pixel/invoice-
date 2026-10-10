using System.Text;
using Invoice.Api.Services;
using Invoice.Infrastructure.Persistence;
using Invoice.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ============ DbContext ============
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============ JWT Settings ============
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// ============ Application Services ============
builder.Services.AddScoped<ReportDataService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

// ============ Authentication (JWT Bearer) ============
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing in appsettings.json.");
var jwtIssuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing in appsettings.json.");
var jwtAudience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing in appsettings.json.");

if (jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ============ Controllers ============
builder.Services.AddControllers();

// ============ Swagger ============
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Invoice API",
        Version = "v1",
        Description = "CostProfitApp Invoice API with JWT authentication."
    });

    // تعريف مخطط Bearer حتى يستطيع Swagger UI إرسال التوكن
    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "أدخل: Bearer {your JWT access token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(bearerScheme.Reference.Id, bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { bearerScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

// ============ Pipeline ============
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ترتيب حرج: Authentication قبل Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();