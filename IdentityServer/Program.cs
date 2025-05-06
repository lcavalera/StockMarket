using IdentityServer.Data;
using IdentityServer.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Ajoute les services nécessaires pour Entity Framework et Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
}, ServiceLifetime.Scoped);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Ajouter JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "MyAllowSpecificOrigins", policy => policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000").AllowAnyHeader().AllowAnyMethod());
    options.AddPolicy(name: "MyAllowSpecificOrigins1", policy => policy.WithOrigins("http://localhost:3001", "http://127.0.0.1:3001").AllowAnyHeader().AllowAnyMethod());
});

// en haut du builder de services
builder.Services.AddControllers();

// si tu veux Swagger dans IdentityServer (recommandé pour tester) :
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurer Kestrel pour écouter HTTP et HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5235); // HTTP
    options.ListenLocalhost(7248, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});


var app = builder.Build();

// Configure la pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // ← Active Swagger et Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IdentityServer API V1");
        c.RoutePrefix = "swagger"; // si tu veux swagger à la racine (https://localhost:7248/)
    });
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("MyAllowSpecificOrigins");
app.UseCors("MyAllowSpecificOrigins1");

app.UseAuthentication();

app.UseAuthorization();

// Mappe les contrôleurs
app.MapControllers();

app.Run();