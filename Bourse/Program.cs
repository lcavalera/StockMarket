using Bourse.Data;
using Bourse.Interfaces;
using Bourse.Mappings;
using Bourse.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using IdentityServer.Models;
using IdentityServer.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text; // Pour ApplicationUser

// Limiter le pool de threads pour contrôler la charge
System.Threading.ThreadPool.SetMinThreads(1, 1);
System.Threading.ThreadPool.SetMaxThreads(4, 4); // Par exemple, maximum 4 threads

var builder = WebApplication.CreateBuilder(args);

// Ajout des services nécessaires pour Swagger
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StockRank",
        Version = "v1",
        Description = "API for Forecasts of Stock Market",
        License = new OpenApiLicense
        {
            Name = "Apache 2.0",
            Url = new Uri("http://www.apache.org")
        },
        Contact = new OpenApiContact
        {
            Name = "Luca Cavalera",
            Email = "lcavalera75@gmail.com",
            Url = new Uri("https://google.com/")
        }
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Ajouter AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile)); // Ajoutez le profil

builder.Services.AddHttpClient();

// Active Razor Pages
//builder.Services.AddRazorPages();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped(typeof(IIndiceService), typeof(IndiceService));

//builder.Services.AddDbContext<BourseContext>(options => {
//    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
//});

builder.Services.AddDbContext<BourseContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    //.UseQueryTrackingBehavior((QueryTrackingBehavior)QuerySplittingBehavior.SplitQuery);
}, ServiceLifetime.Scoped
);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
}, ServiceLifetime.Scoped);


builder.Services.AddSingleton<MLContext>(sp => new MLContext());

builder.Services.AddSingleton<ScheduledTaskService>();
builder.Services.AddSingleton<IScheduledTaskService>(sp => sp.GetRequiredService<ScheduledTaskService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ScheduledTaskService>());

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "MyAllowSpecificOrigins", policy => policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000").AllowAnyHeader().AllowAnyMethod());
    options.AddPolicy(name: "MyAllowSpecificOrigins1", policy => policy.WithOrigins("http://localhost:3001", "http://127.0.0.1:3001").AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//    var secretKey = jwtSettings["SecretKey"];
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = jwtSettings["Issuer"],
//        ValidAudience = jwtSettings["Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
//    };
//});

// Configurer Kestrel pour écouter HTTP et HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5184); // HTTP
    options.ListenLocalhost(7157, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    // ← Active Swagger et Swagger UI

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bourse API V1");
        c.RoutePrefix = "swagger"; // si tu veux swagger à la racine (https://localhost:7157/)
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("MyAllowSpecificOrigins");
app.UseCors("MyAllowSpecificOrigins1");

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=BourseView}/{action=IndexDTO}/{id?}");

// Mappe Razor Pages
//app.MapRazorPages();

// Mappe les API
app.MapControllers();

//app.MapGet("/", context =>
//{
//    context.Response.Redirect("/Bourse/IndexDTO");
//    return Task.CompletedTask;
//});

app.Run();
