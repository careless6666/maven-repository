using Microsoft.EntityFrameworkCore;
using MavenRepositoryServer.Data;
using MavenRepositoryServer.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database
builder.Services.AddDbContext<RepositoryContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure repository service
builder.Services.AddScoped<RepositoryService>();

// Configure file upload limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "RegexRoute",
        pattern: "{path:regex(^([^/]+/){{3,}}.*\\..*$)}",
        defaults: new { controller = "CatchAll", action = "Index" });

    // Другие ваши маршруты:
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RepositoryContext>();
    context.Database.EnsureCreated();
}

app.Run();