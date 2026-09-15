using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SystemDesign.CloudStorage.Api.Handlers;
using SystemDesign.CloudStorage.Infrastructure;
using SystemDesign.CloudStorage.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddDbContext<CloudStorageDbContext>(
    o => o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddCloudStorageInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "SystemDesign.CloudStorage", Version = "v1" });

    o.AddSecurityDefinition("Bearer",
        new() { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });

    o.AddSecurityRequirement(
        new() { { new() { Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();