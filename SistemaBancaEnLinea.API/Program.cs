using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.DA.Acciones;
using SistemaBancaEnLinea.BW;
using SistemaBancaEnLinea.BW.CU;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIGURACIÓN DEL DbContext ==========
builder.Services.AddDbContext<BancaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ========== CONFIGURACIÓN DE CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ========== CONFIGURACIÓN DE JWT ==========
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TuClaveSecretaMuyLargaYSegura1234567890!@#$%";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SistemaBancaEnLinea";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SistemaBancaEnLinea";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

// ========== REGISTRO DE ACCIONES (DA) ==========
builder.Services.AddScoped<UsuarioAcciones>();
builder.Services.AddScoped<ClienteAcciones>();
builder.Services.AddScoped<CuentaAcciones>();
builder.Services.AddScoped<BeneficiarioAcciones>();
builder.Services.AddScoped<TransaccionAcciones>();
builder.Services.AddScoped<ProgramacionAcciones>();
builder.Services.AddScoped<ProveedorServicioAcciones>();
builder.Services.AddScoped<AuditoriaAcciones>();

// ========== REGISTRO DE SERVICIOS (BW) ==========
builder.Services.AddScoped<IUsuarioServicio, UsuarioServicio>();
builder.Services.AddScoped<IClienteServicio, ClienteServicio>();
builder.Services.AddScoped<ICuentaServicio, CuentaServicio>();
builder.Services.AddScoped<IBeneficiarioServicio, BeneficiarioServicio>();
builder.Services.AddScoped<ITransferenciasServicio, TransferenciasServicio>();
builder.Services.AddScoped<IPagosServiciosServicio, PagosServiciosServicio>();
builder.Services.AddScoped<IProgramacionServicio, ProgramacionServicio>();
builder.Services.AddScoped<IProveedorServicioServicio, ProveedorServicioServicio>();
builder.Services.AddScoped<IAuditoriaServicio, AuditoriaServicio>();

// ========== REGISTRO DE CASOS DE USO ==========
builder.Services.AddScoped<GestionCuentasCU>();
builder.Services.AddScoped<GestionUsuariosCU>();
builder.Services.AddScoped<TransferenciasCU>();

// ========== CONFIGURACIÓN DE CONTROLLERS ==========
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// ========== CONFIGURACIÓN DE SWAGGER ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema Banca en Línea API",
        Version = "v1",
        Description = "API para el sistema de banca en línea"
    });

    // Configuración de seguridad JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer' seguido de un espacio y el token JWT"
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

// ========== PIPELINE DE MIDDLEWARE ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Banca en Línea API v1");
    });
}
// Habilitar CORS
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ========== INICIALIZAR BASE DE DATOS Y DATOS DE PRUEBA ==========
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<BancaContext>();

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
        throw;
    }
}

app.Run();