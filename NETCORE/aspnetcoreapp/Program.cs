// This method gets called by the runtime. Use this method to add services to the container.
var builder = WebApplication.CreateBuilder(args);

// The following line enables Application Insights telemetry collection.
builder.Services.AddApplicationInsightsTelemetry();

// This code adds other services for your application.
builder.Services.AddMvc();

// Add services to the container.
builder.Services.AddRazorPages();

var key = "APPLICATIONINSIGHTS_LOG_DIAGNOSTICS";
var value = @"C:\home\LogFiles\SelfDiagnostics";
Environment.SetEnvironmentVariable(key, value);

var app = builder.Build();

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

app.UseAuthorization();

app.MapRazorPages();

app.Run();
