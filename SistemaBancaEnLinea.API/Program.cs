using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaBancaEnLinea.BW;
using SistemaBancaEnLinea.BW.CU;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.DA.Acciones;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIGURACIÓN DE SERVICIOS ==========

// 1. Configurar DbContext
builder.Services.AddDbContext<BancaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configurar CORS para permitir el frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 3. Configurar Autenticación JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TuClaveSecretaMuyLargaYSegura1234567890!@#$%";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SistemaBancaEnLinea";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SistemaBancaEnLinea";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Registrar Acciones (Capa de Datos)
builder.Services.AddScoped<UsuarioAcciones>();
builder.Services.AddScoped<ClienteAcciones>();
builder.Services.AddScoped<CuentaAcciones>();
builder.Services.AddScoped<BeneficiarioAcciones>();
builder.Services.AddScoped<TransaccionAcciones>();
builder.Services.AddScoped<ProgramacionAcciones>();
builder.Services.AddScoped<ProveedorServicioAcciones>();
builder.Services.AddScoped<AuditoriaAcciones>();

// 5. Registrar Servicios (Capa de Negocio)
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
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// 8. Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema Banca en Línea API",
        Version = "v1",
        Description = "API para el sistema de banca en línea"
    });

    // Configurar autenticación en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Bearer {token}\"",
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

// 9. Configurar Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// ========== CONFIGURACIÓN DEL PIPELINE ==========

// Configurar el pipeline de solicitudes HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Banca en Línea API v1");
        c.RoutePrefix = "swagger";
    });
}

// Habilitar CORS
app.UseCors("AllowAll");

// Habilitar HTTPS redirection (opcional en desarrollo)
// app.UseHttpsRedirection();

// Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

// Mapear controladores
app.MapControllers();

// Crear base de datos si no existe (solo en desarrollo)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BancaContext>();
    try
    {
        context.Database.EnsureCreated();
        // O usar migraciones: context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al crear/migrar la base de datos");
    }
}
// ========== INICIALIZAR BASE DE DATOS Y DATOS DE PRUEBA ==========
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<BancaContext>();

        // Crear la base de datos si no existe
        logger.LogInformation("Verificando base de datos...");

        if (context.Database.EnsureCreated())
        {
            logger.LogInformation("Base de datos creada exitosamente.");
        }
        else
        {
            logger.LogInformation("Base de datos ya existe.");
        }

        // Ejecutar seed de datos
        logger.LogInformation("Ejecutando seed de datos...");
        await SeedData.InitializeAsync(context);
        logger.LogInformation("Seed de datos completado.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al inicializar la base de datos");
        throw; // Re-lanzar para ver el error completo
    }
}

app.Run();
