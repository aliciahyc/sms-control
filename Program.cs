using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmsControl.Services;

var builder = WebApplication.CreateBuilder(args);

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin() 
                .AllowAnyMethod() 
                .AllowAnyHeader();
    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<SmsService>();
builder.Services.AddHostedService<PhoneNumberCleanupService>();
builder.WebHost.UseUrls("http://localhost:5000"); 

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseCors("AllowAllOrigins");

// Explicitly handle OPTIONS requests, which are sent for preflight checks
app.Use((context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200; // Respond with status 200 for OPTIONS requests
        return Task.CompletedTask;
    }
    return next();
});

app.MapControllers();

app.Run();
