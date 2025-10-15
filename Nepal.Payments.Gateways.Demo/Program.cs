using Microsoft.EntityFrameworkCore;
using Nepal.Payments.Gateways.Demo.Data;
using Nepal.Payments.Gateways.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// Add services to the container.
builder.Services.AddControllersWithViews();

// Add logging
builder.Services.AddLogging();

// Add SignalR
builder.Services.AddSignalR();

// Add Nepal Payment Gateways with WebSocket support
builder.Services.AddNepalPaymentGateways();

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map SignalR hub
app.MapHub<Nepal.Payments.Gateways.Demo.Hubs.PaymentHub>("/paymentHub");

app.Run();
