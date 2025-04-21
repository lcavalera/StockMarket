using Bourse.Data;
using Bourse.Interfaces;
using Bourse.Mappings;
using Bourse.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.OpenApi.Models;
using System.Reflection;

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

builder.Services.AddSingleton<MLContext>(sp => new MLContext());

builder.Services.AddSingleton<ScheduledTaskService>();
builder.Services.AddSingleton<IScheduledTaskService>(sp => sp.GetRequiredService<ScheduledTaskService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ScheduledTaskService>());

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "MyAllowSpecificOrigins", policy => policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000").AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseCors("MyAllowSpecificOrigins");

app.MapControllers();

app.Run();
