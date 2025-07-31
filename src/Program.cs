using Microsoft.EntityFrameworkCore;
using MyAzureWebApp.Data;
using MyAzureWebApp.Middleware;
using MyAzureWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add Entity Framework with SQL Server or In-Memory database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    // Use In-Memory database for development/testing
    builder.Services.AddDbContext<RequestTrackingContext>(options =>
        options.UseInMemoryDatabase("RequestTrackingDb"));
}
else
{
    // Use SQL Server for production
    builder.Services.AddDbContext<RequestTrackingContext>(options =>
        options.UseSqlServer(connectionString));
}

// Register custom services
builder.Services.AddScoped<IUserAgentClassifier, UserAgentClassifier>();

// Add logging configuration
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Ensure database is created (for In-Memory database)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RequestTrackingContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add request tracking middleware early in the pipeline
app.UseRequestTracking();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
