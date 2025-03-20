using Bourse.Data;
using Bourse.Interfaces;
using Bourse.Mappings;
using Bourse.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

// Limiter le pool de threads pour contrôler la charge
System.Threading.ThreadPool.SetMinThreads(1, 1);
System.Threading.ThreadPool.SetMaxThreads(4, 4); // Par exemple, maximum 4 threads

var builder = WebApplication.CreateBuilder(args);

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
); ;

builder.Services.AddSingleton<MLContext>(sp => new MLContext());

builder.Services.AddSingleton<ScheduledTaskService>();
builder.Services.AddSingleton<IScheduledTaskService>(sp => sp.GetRequiredService<ScheduledTaskService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ScheduledTaskService>());

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Bourse}/{action=IndexDTO}/{id?}");

app.Run();
