using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.DA.Acciones;
using SistemaBancaEnLinea.BW;
using SistemaBancaEnLinea.BW.CU;
using SistemaBancaEnLinea.BW.Interfaces.BW;

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIGURACIÓN DE SERVICIOS ==========

// 1. Configurar DbContext
builder.Services.AddDbContext<BancaContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
    )
);

// 2. Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 3. Configurar JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Registrar Acciones (DA Layer)
builder.Services.AddScoped<UsuarioAcciones>();
builder.Services.AddScoped<ClienteAcciones>();
builder.Services.AddScoped<CuentaAcciones>();
builder.Services.AddScoped<BeneficiarioAcciones>();
builder.Services.AddScoped<TransaccionAcciones>();
builder.Services.AddScoped<ProgramacionAcciones>();
builder.Services.AddScoped<ProveedorServicioAcciones>();
builder.Services.AddScoped<AuditoriaAcciones>();

// 5. Registrar Servicios (BW Layer)
builder.Services.AddScoped<IUsuarioServicio, UsuarioServicio>();
builder.Services.AddScoped<IClienteServicio, ClienteServicio>();
builder.Services.AddScoped<ICuentaServicio, CuentaServicio>();
builder.Services.AddScoped<IBeneficiarioServicio, BeneficiarioServicio>();
builder.Services.AddScoped<ITransferenciasServicio, TransferenciasServicio>();
builder.Services.AddScoped<IPagosServiciosServicio, PagosServiciosServicio>();
builder.Services.AddScoped<IProgramacionServicio, ProgramacionServicio>();
builder.Services.AddScoped<IProveedorServicioServicio, ProveedorServicioServicio>();
builder.Services.AddScoped<IAuditoriaServicio, AuditoriaServicio>();

// 6. Registrar Casos de Uso
builder.Services.AddScoped<GestionCuentasCU>();
builder.Services.AddScoped<GestionUsuariosCU>();
builder.Services.AddScoped<TransferenciasCU>();

// 7. Configurar Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// 8. Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema Banca en Línea API",
        Version = "v1",
        Description = "API REST para el sistema de banca en línea - Proyecto SOF-18"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ========== CONFIGURACIÓN DEL PIPELINE ==========

// SIEMPRE habilitar Swagger (Development y Production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Banca API v1");
    c.RoutePrefix = "swagger"; // Swagger en la raíz
});

// Aplicar migraciones y seed data automáticamente
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BancaContext>();

        Console.WriteLine("========================================");
        Console.WriteLine("Aplicando migraciones pendientes...");
        await context.Database.MigrateAsync();
        Console.WriteLine(" Migraciones aplicadas exitosamente.");

        Console.WriteLine("Iniciando seed de datos...");
        await SeedData.InitializeAsync(context);
        await SeedDataProveedores.InitializeProveedoresAsync(context);
        Console.WriteLine(" Seed de datos completado.");
        Console.WriteLine("========================================");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, " Error durante la migración o seed de datos");
        throw;
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("========================================");
Console.WriteLine(" API Sistema Banca en Línea INICIADA");
Console.WriteLine($" Swagger UI: https://localhost:7500");
Console.WriteLine($" Swagger UI: http://localhost:5500");
Console.WriteLine("========================================");

app.Run();