using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.BW;
using SistemaBancaEnLinea.BW.Interfaces.BW;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// Configuración de Servicios
// =========================================================

// Configurar DbContext con SQL Server
builder.Services.AddDbContext<BancaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar servicios de negocio (Inyección de Dependencias)
builder.Services.AddScoped<IUsuarioServicio, UsuarioServicio>();

// Configurar controladores
builder.Services.AddControllers();

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Sistema Banca en Línea API",
        Version = "v1",
        Description = "API REST para el Sistema de Banca en Línea - UIA"
    });
});

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// =========================================================
// Configuración del Pipeline HTTP
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Banca en Línea API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();