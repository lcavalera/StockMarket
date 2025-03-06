using Bourse.Data;
using Bourse.Interfaces;
using Bourse.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton<IScheduledTaskService, ScheduledTaskService>();
builder.Services.AddHostedService<ScheduledTaskService>();

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
    pattern: "{controller=Bourse}/{action=Index}/{id?}");

app.Run();
