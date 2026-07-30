using StockGuard.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<FirebaseService>();
builder.Services.AddSingleton<QrCodeService>();

// ── Session ───────────────────────────────────────────────────
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// ── Routes ────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ✅ Explicit short routes for sidebar navigation
app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard",
    defaults: new
    {
        controller = "Dashboard",
        action = "Index"
    });

app.MapControllerRoute(
    name: "equipment",
    pattern: "Equipment",
    defaults: new
    {
        controller = "Equipment",
        action = "Index"
    });

app.MapControllerRoute(
    name: "tool",
    pattern: "Tool",
    defaults: new
    {
        controller = "Tool",
        action = "Index"
    });

app.MapControllerRoute(
    name: "worker",
    pattern: "Worker",
    defaults: new
    {
        controller = "Worker",
        action = "Index"
    });

app.MapControllerRoute(
    name: "damage",
    pattern: "Damage",
    defaults: new
    {
        controller = "Damage",
        action = "Index"
    });

app.MapControllerRoute(
    name: "transaction",
    pattern: "Transaction",
    defaults: new
    {
        controller = "Transaction",
        action = "Index"
    });

app.MapControllerRoute(
    name: "project",
    pattern: "Project",
    defaults: new
    {
        controller = "Project",
        action = "Index"
    });

app.MapControllerRoute(
    name: "analytics",
    pattern: "Analytics",
    defaults: new
    {
        controller = "Analytics",
        action = "Index"
    });

app.MapControllerRoute(
    name: "toolQrCode",
    pattern: "Tool/QrCode",
    defaults: new
    {
        controller = "Tool",
        action = "QrCode"
    });

app.MapControllerRoute(
    name: "toolPrintAll",
    pattern: "Tool/PrintAll",
    defaults: new
    {
        controller = "Tool",
        action = "PrintAll"
    });

app.MapControllerRoute(
    name: "toolDownloadQr",
    pattern: "Tool/DownloadQr",
    defaults: new
    {
        controller = "Tool",
        action = "DownloadQr"
    });
app.MapControllerRoute(
    name: "pause",
    pattern: "Pause",
    defaults: new
    {
        controller = "Pause",
        action = "Index"
    });

app.Run();