using Invoyz.Api.Application.Customers;
using Invoyz.Api.Application.InvoiceLines;
using Invoyz.Api.Application.Invoices;
using Invoyz.Api.Application.PdfJobs;
using Invoyz.Api.Application.Products;
using Invoyz.Api.Domain.Common;
using Invoyz.Api.Domain.Customers;
using Invoyz.Api.Domain.InvoiceLines;
using Invoyz.Api.Domain.Invoices;
using Invoyz.Api.Domain.Products;
using Invoyz.Api.Infrastructure.Customers;
using Invoyz.Api.Infrastructure.InvoiceLines;
using Invoyz.Api.Infrastructure.Invoices;
using Invoyz.Api.Infrastructure.PdfJobs;
using Invoyz.Api.Infrastructure.Products;
using Invoyz.Api.Infrastructure.Seed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

const string ClientCorsPolicy = "ClientCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddNewtonsoftJson();

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Invoyz API",
        Version = "v1"
    });
});

builder.Services.AddScoped<IValidator<Customer>, CustomerValidator>();
builder.Services.AddScoped<IValidator<Product>, ProductValidator>();
builder.Services.AddScoped<IValidator<Invoice>, InvoiceValidator>();
builder.Services.AddScoped<IValidator<InvoiceLine>, InvoiceLineValidator>();

builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<InvoiceLineService>();

builder.Services.AddMemoryCache();

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceLineRepository, InvoiceLineRepository>();
builder.Services.AddScoped<IPdfJobRepository, PdfJobRepository>();

// PdfGenerationQueue is a singleton so InvoicePdfService (per-request, via the
// IPdfGenerationQueue port) and PdfGenerationWorker (app-lifetime background
// service, via the concrete type) share the exact same in-memory queue.
builder.Services.AddSingleton<PdfGenerationQueue>();
builder.Services.AddSingleton<IPdfGenerationQueue>(sp => sp.GetRequiredService<PdfGenerationQueue>());
builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();
builder.Services.AddHostedService<PdfGenerationWorker>();

var app = builder.Build();

using (var seedScope = app.Services.CreateScope())
{
    await MockDataSeeder.SeedAsync(seedScope.ServiceProvider);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(ClientCorsPolicy);

app.MapGet("/health", async (HealthCheckService healthCheckService) =>
{
    var report = await healthCheckService.CheckHealthAsync();
    return report.Status == HealthStatus.Healthy
        ? Results.Ok(report.Status.ToString())
        : Results.Json(report.Status.ToString(), statusCode: StatusCodes.Status503ServiceUnavailable);
})
.WithTags("Health");

app.MapControllers();

app.Run();

public partial class Program { }
